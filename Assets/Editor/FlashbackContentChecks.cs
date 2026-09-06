using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LetGo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Object = UnityEngine.Object;

// Separate content audit. GPU readbacks deliberately make this unsuitable as a pacing benchmark.
[InitializeOnLoad]
public static class FlashbackContentChecks
{
    private const string Key = "LetGo.FlashbackContent.";
    private const int Width = 160, Height = 90;
    private const double MeanThreshold = 0.25d, TileThreshold = 0.75d;
    private static Report report;
    private static VideoPlayer video;
    private static RenderTexture target, previousFull, small;
    private static Texture2D smallCpu, fullCpu;
    private static SHA256 sha;
    private static byte[] previousLuma;
    private static long lastFrame = -1, cachedFullFrame = -1;
    private static string cachedFullHash;
    private static bool finished;
    private static double deadline;
    private static readonly List<Frame> frames = new List<Frame>(720);

    static FlashbackContentChecks()
    {
        EditorApplication.playModeStateChanged += OnMode;
        EditorApplication.update += UpdateHost;
    }

    [MenuItem("Tools/Let Go/Audit Flashback Repeated Frames")]
    public static void Run() => Begin(false);
    public static void RunBatch() => Begin(true);

    private static void Begin(bool batch)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!batch)
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                { Debug.LogWarning("Save scene edits before the content audit."); return; }
        SessionState.SetString(Key + "Setup", JsonUtility.ToJson(new Setup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        SessionState.SetBool(Key + "Background", Application.runInBackground);
        SessionState.SetBool(Key + "Paused", EditorApplication.isPaused);
        SessionState.SetBool(Key + "Batch", batch);
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetInt(Key + "Exit", 1);
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void OnMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            ConfigureView();
            frames.Clear(); previousLuma = null; finished = false;
            lastFrame = cachedFullFrame = -1;
            cachedFullHash = null;
            report = new Report();
            deadline = EditorApplication.timeSinceStartup + 75d;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/FlashbackContentProgress.txt", "Starting continuous content audit: 0 / 720 frames.");
            var host = new GameObject("Flashback Content Audit Host", typeof(Camera), typeof(EndingFlashbackPlayer));
            host.GetComponent<Camera>().cullingMask = 0;
            host.GetComponent<EndingFlashbackPlayer>().StartCoroutine(Audit(host));
        }
        if (state != PlayModeStateChange.EnteredEditMode) return;
        SessionState.SetBool(Key + "Active", false);
        Application.runInBackground = SessionState.GetBool(Key + "Background", false);
        EditorApplication.isPaused = SessionState.GetBool(Key + "Paused", false);
        RestoreView();
        var setup = JsonUtility.FromJson<Setup>(SessionState.GetString(Key + "Setup", ""));
        if (setup?.scenes != null && setup.scenes.Length > 0 && !string.IsNullOrEmpty(setup.scenes[0].path))
            EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);
        if (File.Exists("Logs/FlashbackContentChecks.json"))
        {
            var saved = JsonUtility.FromJson<Report>(File.ReadAllText("Logs/FlashbackContentChecks.json"));
            saved.hostSettingsRestored = Application.runInBackground == SessionState.GetBool(Key + "Background", false) &&
                EditorApplication.isPaused == SessionState.GetBool(Key + "Paused", false);
            File.WriteAllText("Logs/FlashbackContentChecks.json", JsonUtility.ToJson(saved, true));
        }
        if (SessionState.GetBool(Key + "Batch", false)) EditorApplication.Exit(SessionState.GetInt(Key + "Exit", 1));
    }

    private static IEnumerator Audit(GameObject host)
    {
        yield return null;
        var clip = Resources.Load<VideoClip>(EndingFlashbackPlayer.ResourcePath);
        if (clip == null) { Finish("Missing actual Ending/flashback clip."); yield break; }
        report.path = AssetDatabase.GetAssetPath(clip);
        report.frameRate = clip.frameRate;
        report.expectedFrames = (long)clip.frameCount;
        if (report.expectedFrames != 720) { Finish("This audit expects the delivered 720-frame movie."); yield break; }
        sha = SHA256.Create();
        target = MakeTarget((int)clip.width, (int)clip.height);
        previousFull = MakeTarget(target.width, target.height);
        small = MakeTarget(Width, Height);
        smallCpu = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        fullCpu = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        video = host.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.source = VideoSource.VideoClip;
        video.clip = clip;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.targetTexture = target;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        video.playbackSpeed = 1f;
        video.isLooping = false;
        video.sendFrameReadyEvents = true;
        video.frameReady += FrameReady;
        video.errorReceived += VideoError;
        video.Prepare();
        var prepareDeadline = EditorApplication.timeSinceStartup + 10d;
        while (!video.isPrepared && report.error == null && EditorApplication.timeSinceStartup < prepareDeadline) yield return null;
        if (!video.isPrepared || report.error != null) { Finish(report.error ?? "Prepare timed out."); yield break; }
        report.canStep = video.canStep;
        if (video.canSetSkipOnDrop) video.skipOnDrop = false;
        report.skipOnDrop = video.skipOnDrop;
        video.Play();
        while (!finished && frames.Count < report.expectedFrames)
        {
            if (report.error != null) { Finish(report.error); yield break; }
            yield return null;
        }
        if (finished) yield break;
        Finish(null);
    }

    private static void FrameReady(VideoPlayer source, long frame)
    {
        if (finished || report.error != null) return;
        report.frameReadyEvents++;
        if (frame != lastFrame + 1)
        { report.error = "Noncontiguous decoded frame: expected " + (lastFrame + 1) + ", got " + frame; return; }
        try
        {
            // Read this frame inside its callback before the decoder can replace the target.
            SampleFrame(frame);
            lastFrame = frame;
            if (frames.Count % 60 == 0)
            {
                var progress = "Audited " + frames.Count + " / 720 frames; source time " +
                    (frame / report.frameRate).ToString("F2", CultureInfo.InvariantCulture) + "s.";
                File.WriteAllText("Logs/FlashbackContentProgress.txt", progress);
                Debug.Log("[Flashback content] " + progress);
            }
        }
        catch (Exception exception) { report.error = "Frame readback audit failed: " + exception.Message; }
    }
    private static void VideoError(VideoPlayer source, string message) => report.error = message;

    private static void SampleFrame(long number)
    {
        Graphics.Blit(target, small);
        Read(small, smallCpu);
        var rgb = smallCpu.GetRawTextureData<byte>();
        var luma = new byte[Width * Height];
        var tiles = new double[32];
        var tileCounts = new int[32];
        var frame = new Frame { number = number, seconds = number / report.frameRate, mae = -1d };
        double total = 0d;
        for (var i = 0; i < luma.Length; i++)
        {
            luma[i] = (byte)((77 * rgb[i * 3] + 150 * rgb[i * 3 + 1] + 29 * rgb[i * 3 + 2]) >> 8);
            if (previousLuma == null) continue;
            var difference = Math.Abs(luma[i] - previousLuma[i]);
            total += difference;
            var tile = ((i / Width) * 4 / Height) * 8 + (i % Width) * 8 / Width;
            tiles[tile] += difference;
            tileCounts[tile]++;
        }
        frame.smallLumaHash = Convert.ToBase64String(sha.ComputeHash(luma));
        if (previousLuma != null)
        {
            frame.mae = total / luma.Length;
            for (var i = 0; i < tiles.Length; i++) frame.maximumTileMae = Math.Max(frame.maximumTileMae, tiles[i] / tileCounts[i]);
            frame.nearDuplicateCandidate = frame.mae <= MeanThreshold && frame.maximumTileMae <= TileThreshold;
            if (frame.nearDuplicateCandidate)
            {
                var previousHash = cachedFullFrame == number - 1 ? cachedFullHash : FullHash(previousFull);
                frame.fullHash = FullHash(target);
                frame.exactDuplicate = previousHash == frame.fullHash;
                cachedFullHash = frame.fullHash;
                cachedFullFrame = number;
            }
        }
        frames.Add(frame);
        previousLuma = luma;
        // Preserve the previous full-size pixels bit-for-bit for candidate hash comparison.
        Graphics.CopyTexture(target, previousFull);
    }

    private static string FullHash(RenderTexture source)
    {
        Read(source, fullCpu);
        report.fullResolutionReadbacks++;
        return Convert.ToBase64String(sha.ComputeHash(fullCpu.GetRawTextureData<byte>().ToArray()));
    }
    private static void Read(RenderTexture source, Texture2D cpu)
    {
        var previous = RenderTexture.active;
        try { RenderTexture.active = source; cpu.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false); }
        finally { RenderTexture.active = previous; }
    }
    private static RenderTexture MakeTarget(int width, int height)
    {
        var result = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        if (!result.Create()) throw new InvalidOperationException("Could not allocate audit render texture.");
        return result;
    }

    private static void Finish(string error)
    {
        if (finished) return;
        finished = true;
        report.error = error ?? report.error;
        report.observedFrames = frames.Count;
        report.passed = report.error == null && frames.Count == 720 && lastFrame == 719 && report.frameReadyEvents == 720;
        report.exactHolds = Holds(true);
        report.nearHolds = Holds(false);
        foreach (var hold in report.exactHolds) report.longestExactHoldSeconds = Math.Max(report.longestExactHoldSeconds, hold.durationSeconds);
        foreach (var hold in report.nearHolds) report.longestNearHoldSeconds = Math.Max(report.longestNearHoldSeconds, hold.durationSeconds);
        report.note = "Separate content audit, not playback performance. Continuous playback with skipOnDrop=false " +
            "reads each frameReady callback and validates all frames 0-719; no Pause or StepForward calls. " +
            "Near-duplicate candidates use 160x90 8-bit luma MAE<=0.25 and maximum 8x4-tile MAE<=0.75. " +
            "Exact duplicates require equal full-resolution RGB SHA256. Static shots/black frames may be intentional.";
        Cleanup();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/FlashbackContentChecks.json", JsonUtility.ToJson(report, true));
        var csv = new StringBuilder("frame,seconds,luma_mae,max_tile_mae,near_candidate,exact_duplicate,small_luma_sha256,full_rgb_sha256\n");
        foreach (var frame in frames)
            csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1:F6},{2:F6},{3:F6},{4},{5},{6},{7}\n", frame.number,
                frame.seconds, frame.mae, frame.maximumTileMae, frame.nearDuplicateCandidate, frame.exactDuplicate,
                frame.smallLumaHash, frame.fullHash);
        File.WriteAllText("Logs/FlashbackContentFrames.csv", csv.ToString());
        SessionState.SetInt(Key + "Exit", report.passed ? 0 : 1);
        Debug.Log("[Flashback content] " + JsonUtility.ToJson(report));
        EditorApplication.isPlaying = false;
    }

    private static Hold[] Holds(bool exact)
    {
        var result = new List<Hold>();
        long first = -1, last = -1;
        for (var i = 1; i <= frames.Count; i++)
        {
            var same = i < frames.Count && (exact ? frames[i].exactDuplicate : frames[i].nearDuplicateCandidate);
            if (same) { if (first < 0) first = i - 1; last = i; }
            else if (first >= 0)
            {
                result.Add(new Hold { firstFrame = first, lastFrame = last, startSeconds = first / report.frameRate,
                    endSeconds = (last + 1) / report.frameRate, durationSeconds = (last - first + 1) / report.frameRate });
                first = -1;
            }
        }
        return result.ToArray();
    }
    private static void Cleanup()
    {
        if (video != null) { video.frameReady -= FrameReady; video.errorReceived -= VideoError; video.Stop(); video.targetTexture = null; }
        foreach (var rt in new[] { target, previousFull, small }) if (rt != null) { rt.Release(); Object.Destroy(rt); }
        if (smallCpu != null) Object.Destroy(smallCpu);
        if (fullCpu != null) Object.Destroy(fullCpu);
        sha?.Dispose(); sha = null;
    }
    private static void UpdateHost()
    {
        if (!SessionState.GetBool(Key + "Active", false) || !EditorApplication.isPlaying || finished) return;
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        EditorApplication.QueuePlayerLoopUpdate();
        if (deadline > 0d && EditorApplication.timeSinceStartup > deadline) Finish("Content audit exceeded its 75-second watchdog.");
    }
    private static void ConfigureView()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var views = Resources.FindObjectsOfTypeAll(type);
        var view = views.Length == 0 ? null : views[0] as EditorWindow;
        SessionState.SetString(Key + "View", view == null ? "" : view.GetEntityId().ToString());
        SessionState.SetInt(Key + "Size", view == null ? 0 : (int)type.GetProperty("selectedSizeIndex", flags).GetValue(view));
        typeof(JourneyPlaythroughChecks).GetMethod("ConfigureBatchView", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
    private static void RestoreView()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var id = SessionState.GetString(Key + "View", "");
        foreach (var item in Resources.FindObjectsOfTypeAll(type))
        {
            var view = item as EditorWindow;
            if (view == null) continue;
            if (id == "") view.Close();
            else if (view.GetEntityId().ToString() == id) type.GetProperty("selectedSizeIndex", flags).SetValue(view, SessionState.GetInt(Key + "Size", 0));
        }
    }

    [Serializable] private sealed class Setup { public SceneSetup[] scenes; }
    private sealed class Frame { public long number; public double seconds, mae, maximumTileMae; public bool nearDuplicateCandidate, exactDuplicate; public string smallLumaHash, fullHash; }
    [Serializable] private sealed class Hold { public long firstFrame, lastFrame; public double startSeconds, endSeconds, durationSeconds; }
    [Serializable] private sealed class Report
    {
        public bool passed, canStep, skipOnDrop, hostSettingsRestored;
        public string path, error, note;
        public long expectedFrames, observedFrames, frameReadyEvents, fullResolutionReadbacks;
        public double frameRate, longestExactHoldSeconds, longestNearHoldSeconds;
        public Hold[] exactHolds, nearHolds;
    }
}
