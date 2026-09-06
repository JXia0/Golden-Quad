using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using LetGo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// Runs only the real ending in a saved scene. No screenshots, asset changes or quality overrides.
[InitializeOnLoad]
public static class FlashbackPlaybackChecks
{
    private const string ActiveKey = "LetGo.FlashbackPacing.Active";
    private const string BatchKey = "LetGo.FlashbackPacing.Batch";
    private const string SetupKey = "LetGo.FlashbackPacing.Setup";
    private const string ResultKey = "LetGo.FlashbackPacing.Result";
    private const string HostKey = "LetGo.FlashbackPacing.HostOverrides";
    private const string BackgroundKey = "LetGo.FlashbackPacing.Background";
    private const string PausedKey = "LetGo.FlashbackPacing.Paused";
    private const string ViewIdKey = "LetGo.FlashbackPacing.ViewId";
    private const string ViewSizeKey = "LetGo.FlashbackPacing.ViewSize";
    private static Report report;
    private static readonly List<double> appIntervals = new List<double>(8192);
    private static readonly List<double> readyIntervals = new List<double>(1024);
    private static readonly List<Sample> samples = new List<Sample>(8192);
    private static double deadline;
    private static double firstWall = -1d, lastWall, firstVideoTime, lastVideoTime, previousReadyWall = -1d;
    private static long previousFrame = -1, previousReadyFrame = -1;
    private static bool completed;

    static FlashbackPlaybackChecks()
    {
        EditorApplication.playModeStateChanged += OnMode;
        EditorApplication.update += Watchdog;
    }

    [MenuItem("Tools/Let Go/Measure Flashback Playback Pacing")]
    public static void Run() => Begin(false);
    public static void RunBatch() => Begin(true);

    private static void Begin(bool batch)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!batch)
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                {
                    Debug.LogWarning("Save the open scenes before measuring flashback playback.");
                    return;
                }
        SessionState.SetString(SetupKey, JsonUtility.ToJson(new SavedSetup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(BatchKey, batch);
        SessionState.SetInt(ResultKey, 1);
        SessionState.SetBool(BackgroundKey, Application.runInBackground);
        SessionState.SetBool(PausedKey, EditorApplication.isPaused);
        SessionState.SetBool(HostKey, batch);
        if (batch)
        {
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
        }
        EditorSceneManager.OpenScene("Assets/Scenes/06_FinalWalk.unity");
        EditorApplication.isPlaying = true;
    }

    private static void OnMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            if (SessionState.GetBool(HostKey, false))
            {
                Application.runInBackground = true;
                EditorApplication.isPaused = false;
                ConfigureHostView();
            }
            deadline = EditorApplication.timeSinceStartup + 75d;
            completed = false;
            report = new Report
            {
                unityVersion = Application.unityVersion,
                gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                batchMode = Application.isBatchMode,
                initialRunInBackground = SessionState.GetBool(BackgroundKey, Application.runInBackground),
                hostOverridesApplied = SessionState.GetBool(HostKey, false)
            };
            appIntervals.Clear();
            readyIntervals.Clear();
            samples.Clear();
            firstWall = previousReadyWall = -1d;
            previousFrame = previousReadyFrame = -1;
            var director = UnityEngine.Object.FindAnyObjectByType<StorySceneDirector>();
            if (director == null) Finish("FinalWalk has no StorySceneDirector.");
            else director.StartCoroutine(Measure(director));
        }
        if (state != PlayModeStateChange.EnteredEditMode) return;
        SessionState.SetBool(ActiveKey, false);
        RestoreHost();
        var setup = JsonUtility.FromJson<SavedSetup>(SessionState.GetString(SetupKey, ""));
        if (setup?.scenes != null && setup.scenes.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);
        if (SessionState.GetBool(BatchKey, false))
        {
            SessionState.SetBool(BatchKey, false);
            EditorApplication.Exit(SessionState.GetInt(ResultKey, 1));
        }
    }

    private static IEnumerator Measure(StorySceneDirector director)
    {
        // Let ordinary FinalWalk Start methods and its entry fade finish before requesting its ending.
        yield return new WaitForSecondsRealtime(1.3f);
        report.screenWidth = Screen.width;
        report.screenHeight = Screen.height;
        var clip = Resources.Load<VideoClip>(EndingFlashbackPlayer.ResourcePath);
        if (clip == null) { Finish("The real Ending/flashback VideoClip is missing."); yield break; }
        report.clipPath = AssetDatabase.GetAssetPath(clip);
        report.clipSeconds = clip.length;
        report.clipFrameRate = clip.frameRate;
        report.clipFrameCount = clip.frameCount;
        report.clipWidth = clip.width;
        report.clipHeight = clip.height;
        report.audioTracks = clip.audioTrackCount;
        var requestedAt = Time.realtimeSinceStartupAsDouble;
        director.ShowOpenEnding();
        VideoPlayer video = null;
        while (video == null && !director.EndingVisible && !completed)
        {
            video = GameObject.Find("Ending Flashback")?.GetComponent<VideoPlayer>();
            yield return null;
        }
        if (video == null) { Finish("The film was skipped before a VideoPlayer could be observed."); yield break; }
        report.configuredPlaybackSpeed = video.playbackSpeed;
        report.skipOnDrop = video.skipOnDrop;
        report.canSetSkipOnDrop = video.canSetSkipOnDrop;
        report.clockMode = video.timeUpdateMode.ToString();
        report.frameReadyEvents = video.sendFrameReadyEvents;
        report.renderWidth = video.targetTexture == null ? 0 : video.targetTexture.width;
        report.renderHeight = video.targetTexture == null ? 0 : video.targetTexture.height;
        video.frameReady += FrameReady;
        video.errorReceived += VideoError;
        video.loopPointReached += VideoEnded;
        var voice = GameObject.Find(EndingFlashbackPlayer.NarrationObjectName)?.GetComponent<AudioSource>();
        while (video != null && !report.naturalEnd && !completed)
        {
            if (video.isPlaying && video.frame >= 0)
            {
                var now = Time.realtimeSinceStartupAsDouble;
                var position = video.time;
                if (firstWall < 0d)
                {
                    firstWall = lastWall = now;
                    firstVideoTime = lastVideoTime = position;
                    report.secondsFromEndingRequestToFirstFrame = now - requestedAt;
                }
                else
                {
                    var frameMs = (now - lastWall) * 1000d;
                    appIntervals.Add(frameMs);
                    if (video.frame > previousFrame + 1) report.polledFrameGaps += video.frame - previousFrame - 1;
                    if (video.frame < previousFrame) report.polledFrameBacktracks++;
                    if (position < lastVideoTime - 0.001d) report.timelineBacktracks++;
                    report.maximumClockDriftMs = Math.Max(report.maximumClockDriftMs,
                        Math.Abs((position - firstVideoTime) - (now - firstWall)) * 1000d);
                    var voicePosition = voice != null && voice.isPlaying && voice.clip != null
                        ? voice.timeSamples / (double)voice.clip.frequency : -1d;
                    samples.Add(new Sample { wall = now - firstWall, appMs = frameMs, frame = video.frame,
                        videoSeconds = position, voiceSeconds = voicePosition });
                }
                previousFrame = video.frame;
                lastVideoTime = position;
                lastWall = now;
            }
            yield return null;
        }
        if (completed) yield break;
        if (video != null)
        {
            video.frameReady -= FrameReady;
            video.errorReceived -= VideoError;
            video.loopPointReached -= VideoEnded;
        }
        // Await the runtime player's cleanup, including its temporary background-running override.
        yield return null;
        report.runtimeRestoredHostBackground = Application.runInBackground ==
            (report.hostOverridesApplied || report.initialRunInBackground);
        Finish(report.naturalEnd && string.IsNullOrEmpty(report.error) ? null :
            (string.IsNullOrEmpty(report.error) ? "Playback stopped without a real EOF event." : report.error));
    }

    private static void FrameReady(VideoPlayer source, long frame)
    {
        var now = Time.realtimeSinceStartupAsDouble;
        if (previousReadyWall >= 0d)
        {
            readyIntervals.Add((now - previousReadyWall) * 1000d);
            if (frame > previousReadyFrame + 1) report.frameReadyGaps += frame - previousReadyFrame - 1;
            if (frame < previousReadyFrame) report.frameReadyBacktracks++;
        }
        previousReadyWall = now;
        previousReadyFrame = frame;
        report.frameReadyCount++;
    }

    private static void VideoError(VideoPlayer source, string message) => report.error = message;
    private static void VideoEnded(VideoPlayer source)
    {
        report.naturalEnd = true;
        report.secondsFromFirstFrameToEof = firstWall < 0d ? 0d : Time.realtimeSinceStartupAsDouble - firstWall;
    }

    private static void Watchdog()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || completed) return;
        if (SessionState.GetBool(HostKey, false)) EditorApplication.isPaused = false;
        EditorApplication.QueuePlayerLoopUpdate();
        if (deadline > 0d && EditorApplication.timeSinceStartup > deadline) Finish("Measurement exceeded its 75-second watchdog.");
    }

    private static void ConfigureHostView()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var existing = Resources.FindObjectsOfTypeAll(viewType);
        var view = existing.Length == 0 ? null : existing[0] as EditorWindow;
        SessionState.SetString(ViewIdKey, view == null ? string.Empty : view.GetEntityId().ToString());
        SessionState.SetInt(ViewSizeKey, view == null ? -1 : (int)viewType.GetProperty("selectedSizeIndex", flags).GetValue(view));
        // Reuse the same tested 1280x720 Game view setup as the full native gameplay QA.
        typeof(JourneyPlaythroughChecks).GetMethod("ConfigureBatchView", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, null);
    }

    private static void RestoreHost()
    {
        if (!SessionState.GetBool(HostKey, false)) return;
        SessionState.SetBool(HostKey, false);
        Application.runInBackground = SessionState.GetBool(BackgroundKey, false);
        EditorApplication.isPaused = SessionState.GetBool(PausedKey, false);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var views = Resources.FindObjectsOfTypeAll(viewType);
        var originalId = SessionState.GetString(ViewIdKey, string.Empty);
        foreach (var item in views)
        {
            var view = item as EditorWindow;
            if (view == null) continue;
            if (string.IsNullOrEmpty(originalId)) view.Close();
            else if (view.GetEntityId().ToString() == originalId)
                viewType.GetProperty("selectedSizeIndex", flags).SetValue(view, SessionState.GetInt(ViewSizeKey, 0));
        }
        const string path = "Logs/FlashbackPlaybackChecks.json";
        if (!File.Exists(path)) return;
        var finishedReport = JsonUtility.FromJson<Report>(File.ReadAllText(path));
        finishedReport.restoredRunInBackground = Application.runInBackground == SessionState.GetBool(BackgroundKey, false);
        finishedReport.hostOverridesRestored = finishedReport.restoredRunInBackground &&
            EditorApplication.isPaused == SessionState.GetBool(PausedKey, false);
        File.WriteAllText(path, JsonUtility.ToJson(finishedReport, true));
    }

    private static void Finish(string error)
    {
        if (completed) return;
        completed = true;
        if (report == null) report = new Report();
        if (error != null) report.error = error;
        report.appFrameMs = Summarize(appIntervals);
        report.frameReadyIntervalMs = Summarize(readyIntervals);
        report.measuredPlaybackSpeed = lastWall > firstWall && firstWall >= 0d
            ? (lastVideoTime - firstVideoTime) / (lastWall - firstWall) : 0d;
        report.passed = report.naturalEnd && string.IsNullOrEmpty(report.error) && appIntervals.Count > 0;
        report.note = "Pacing diagnostics, not a perceptual smoothness pass. Polled frame gaps may be app sampling gaps; " +
            "frameReady gaps are separately recorded. Editor/batch timing includes editor overhead. " +
            "Batch host temporarily enables background running, unpauses the editor and selects a 1280x720 Game view; " +
            "prior active host settings are restored after Play Mode. No screenshots or frame-rate overrides. " +
            "A zero-sample host failure is not evidence of a video playback defect.";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/FlashbackPlaybackChecks.json", JsonUtility.ToJson(report, true));
        var csv = new StringBuilder("wall_seconds,app_frame_ms,video_frame,video_seconds,narration_seconds\n");
        foreach (var sample in samples)
            csv.AppendFormat(CultureInfo.InvariantCulture, "{0:F6},{1:F3},{2},{3:F6},{4:F6}\n",
                sample.wall, sample.appMs, sample.frame, sample.videoSeconds, sample.voiceSeconds);
        File.WriteAllText("Logs/FlashbackPlaybackFrames.csv", csv.ToString());
        Debug.Log("[Flashback pacing] " + JsonUtility.ToJson(report));
        SessionState.SetInt(ResultKey, report.passed ? 0 : 1);
        EditorApplication.isPlaying = false;
    }

    private static Distribution Summarize(List<double> values)
    {
        if (values.Count == 0) return new Distribution();
        var sorted = values.ToArray();
        Array.Sort(sorted);
        var distribution = new Distribution { count = sorted.Length, p50 = sorted[(sorted.Length - 1) / 2],
            p95 = sorted[(int)Math.Ceiling((sorted.Length - 1) * 0.95d)], max = sorted[sorted.Length - 1] };
        foreach (var value in values) { if (value > 50d) distribution.over50Ms++; if (value > 100d) distribution.over100Ms++; }
        return distribution;
    }

    [Serializable] private sealed class SavedSetup { public SceneSetup[] scenes; }
    private struct Sample { public double wall, appMs, videoSeconds, voiceSeconds; public long frame; }
    [Serializable] private sealed class Distribution { public int count, over50Ms, over100Ms; public double p50, p95, max; }
    [Serializable] private sealed class Report
    {
        public bool passed, naturalEnd, batchMode, skipOnDrop, canSetSkipOnDrop, frameReadyEvents;
        public bool initialRunInBackground, restoredRunInBackground;
        public bool hostOverridesApplied, hostOverridesRestored, runtimeRestoredHostBackground;
        public string error, unityVersion, gpu, graphicsApi, quality, clipPath, clockMode, note;
        public int vSyncCount, targetFrameRate, screenWidth, screenHeight, renderWidth, renderHeight, audioTracks;
        public uint clipWidth, clipHeight;
        public ulong clipFrameCount;
        public long polledFrameGaps, polledFrameBacktracks, timelineBacktracks, frameReadyGaps, frameReadyBacktracks, frameReadyCount;
        public double clipSeconds, clipFrameRate, configuredPlaybackSpeed, measuredPlaybackSpeed, maximumClockDriftMs;
        public double secondsFromEndingRequestToFirstFrame, secondsFromFirstFrameToEof;
        public Distribution appFrameMs, frameReadyIntervalMs;
    }
}
