#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using LetGo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed partial class JourneyPlaythroughDriver
{
    private IEnumerator ReviewEndingPainting()
    {
        yield return ReviewDeliveredFlashback();
        if (finished) yield break;
        phase = "the delivered final painting settles after the film's actual EOF";
        var director = StorySceneDirector.Instance;
        for (var i = 0; i < 30 && !director.EndingVisible; i++) yield return Keys(0.05f);
        var painting = GameObject.Find("Delivered Ending Art")?.GetComponent<Image>();
        if (!Check(GameObject.Find("Ending Flashback") == null && !AudioListener.pause,
            "the optional flashback leaves no covering canvas or global audio pause before the ending picture")) yield break;
        if (!Check(director.EndingVisible && !director.ReplayAvailable && !Player.ControlsEnabled &&
            painting != null && painting.sprite != null && painting.sprite.name.Contains("结束界面") &&
            painting.preserveAspect && painting.color.a < 0.2f,
            "the delivered painting begins a gradual reveal only after the farewell and the last player step")) yield break;
        yield return Keys(0.25f, Key.Enter);
        yield return Keys(0.2f);
        if (!Check(SceneManager.GetActiveScene().name == "06_FinalWalk" && !director.ReplayAvailable,
            "an early Enter cannot skip the final image")) yield break;
        for (var i = 0; i < 80 && !director.ReplayAvailable; i++) yield return Keys(0.1f);
        var hint = GameObject.Find("Closing Replay")?.GetComponent<Text>();
        if (!Check(painting.color.a > 0.99f && hint != null && hint.fontSize <= 16 &&
            director.ReplayAvailable && GameObject.Find("Closing Title") == null && GameObject.Find("Closing Thanks") == null,
            "the final image remains unobscured by title cards and settles before a quiet replay hint appears")) yield break;
        Capture("06-delivered-ending-painting");
        yield return Keys(1.4f, Key.Enter);
        var menu = FindAnyObjectByType<MainMenuController>();
        if (!Check(SceneManager.GetActiveScene().name == "00_Prologue" && menu != null && menu.Visible &&
            !menu.HasStarted && !JourneyChoices.HasStageRhythm,
            "replay returns to the delivered start screen and holding Enter cannot skip that screen")) yield break;
        yield return Keys(0.15f);
    }

    private IEnumerator ReviewDeliveredFlashback()
    {
        phase = "FinalWalk plays the delivered film and the user's separate narration before its ending art";
        var director = StorySceneDirector.Instance;
        var clip = Resources.Load<VideoClip>(EndingFlashbackPlayer.ResourcePath);
        var voiceClip = Resources.Load<SceneAudioLibrary>("SceneAudioLibrary")?.flashbackVoice;
        if (!Check(clip != null && clip.length >= 29.9d && clip.length <= 30.2d &&
            clip.width == 1280 && clip.height == 720 && clip.audioTrackCount == 1 &&
            UnityEditor.AssetDatabase.GetAssetPath(clip) == "Assets/Resources/Ending/flashback.mp4" &&
            voiceClip != null && voiceClip.length > 27f && voiceClip.length < 28f &&
            UnityEditor.AssetDatabase.GetAssetPath(voiceClip) == "Assets/Audio/Voice/vo_flashback.wav",
            "the actual thirty-second MP4 and original twenty-seven-second narration import through their runtime references")) yield break;

        var sceneSources = new AudioSource[4];
        var originalMute = new bool[4];
        var sceneCameras = new List<(Camera camera, int mask, CameraClearFlags clearFlags)>();
        foreach (var sceneCamera in FindObjectsByType<Camera>())
            if (sceneCamera.isActiveAndEnabled && sceneCamera.gameObject.scene == director.gameObject.scene)
                sceneCameras.Add((sceneCamera, sceneCamera.cullingMask, sceneCamera.clearFlags));
        var sceneAudio = SceneAudio.Instance;
        if (sceneAudio != null)
        {
            var serialized = new UnityEditor.SerializedObject(sceneAudio);
            var fields = new[] { "effectsSource", "ambienceSource", "musicSource", "tensionSource" };
            for (var i = 0; i < fields.Length; i++)
            {
                sceneSources[i] = serialized.FindProperty(fields[i]).objectReferenceValue as AudioSource;
                originalMute[i] = sceneSources[i] != null && sceneSources[i].mute;
            }
        }
        EndingFlashbackPlayer flashback = null;
        VideoPlayer video = null;
        var prepareDeadline = Time.realtimeSinceStartup + 18f;
        while (Time.realtimeSinceStartup < prepareDeadline)
        {
            flashback = director.GetComponent<EndingFlashbackPlayer>();
            video = GameObject.Find("Ending Flashback")?.GetComponent<VideoPlayer>();
            if (video != null && video.isPlaying && video.frame > 0) break;
            if (director.EndingVisible)
            {
                Check(false, "the delivered MP4 must produce frames before the final art appears");
                yield break;
            }
            yield return Keys(0.05f);
        }
        if (!Check(flashback != null && flashback.Playing && video != null && video.isPlaying && video.frame > 0 &&
            !director.EndingVisible && !director.ReplayAvailable && !Player.ControlsEnabled && !AudioListener.pause,
            "the final step starts real decoded video with controls locked and no premature art or replay")) yield break;
        var onlyFilmDraws = sceneCameras.Count > 0;
        foreach (var saved in sceneCameras)
            onlyFilmDraws &= saved.camera != null && saved.camera.isActiveAndEnabled &&
                saved.camera.cullingMask == 0 && saved.camera.clearFlags == saved.clearFlags;
        if (!Check(onlyFilmDraws,
            "the film suppresses scene-camera object drawing while retaining enabled cameras and their clear settings")) yield break;

        var picture = GameObject.Find("Flashback Picture")?.GetComponent<RawImage>();
        var music = video.GetTargetAudioSource(0);
        var voice = GameObject.Find(EndingFlashbackPlayer.NarrationObjectName)?.GetComponent<AudioSource>();
        var renderTarget = video.targetTexture;
        var muted = true;
        foreach (var source in sceneSources) muted &= source == null || source.mute;
        if (!Check(picture != null && picture.enabled && picture.texture == renderTarget && renderTarget != null &&
            picture.GetComponent<AspectRatioFitter>().aspectMode == AspectRatioFitter.AspectMode.FitInParent &&
            video.audioOutputMode == VideoAudioOutputMode.AudioSource && video.IsAudioTrackEnabled(0) &&
            music != null && voice != null && voice != music && voice.clip == voiceClip &&
            voice.spatialBlend == 0f && !voice.loop && Mathf.Approximately(voice.volume, 1f) && muted,
            "the aspect-fitted film routes its embedded music separately from the user's full-volume voice and mutes scene beds")) yield break;

        var naturalEnd = false;
        video.loopPointReached += source => naturalEnd = true;
        var beforeEnter = video.time;
        yield return Keys(0.25f, Key.Enter);
        yield return Keys(0.2f);
        if (!Check(flashback.Playing && video.isPlaying && video.time > beforeEnter &&
            SceneManager.GetActiveScene().name == "06_FinalWalk" && !director.EndingVisible && !director.ReplayAvailable,
            "pressing Enter during the film cannot skip playback or expose the ending picture")) yield break;

        var movieDeadline = Time.realtimeSinceStartup + 42f;
        var output = new float[512];
        var heardMusicSamples = false;
        var lastFrame = video.frame;
        var lastVoiceSample = voice.timeSamples;
        foreach (var second in new[] { 5f, 15f, 27f })
        {
            phase = "delivered flashback at " + second + " seconds";
            while (video != null && video.time < second && Time.realtimeSinceStartup < movieDeadline)
            {
                if (director.EndingVisible || GameObject.Find("Delivered Ending Art") != null)
                {
                    Check(false, "ending artwork stays absent for the entire running film");
                    yield break;
                }
                heardMusicSamples |= EndingAudioHasSamples(music, output);
                yield return Keys(0.05f);
            }
            if (!Check(video != null && video.isPlaying && video.time >= second && video.frame > lastFrame &&
                flashback != null && flashback.NarrationPlaying && voice != null && voice.timeSamples > lastVoiceSample &&
                Mathf.Abs(voice.timeSamples / (float)voiceClip.frequency - ((float)video.time - 0.5f)) < 0.8f &&
                music != null && Mathf.Abs(music.volume - 0.3f) < 0.02f && !music.mute &&
                !director.EndingVisible && !director.ReplayAvailable,
                "video frames and narration samples advance in sync, with the film music ducked at " + second + "s")) yield break;
            var narratorCount = 0;
            foreach (var source in FindObjectsByType<AudioSource>())
                if (source.clip == voiceClip && source.isPlaying) narratorCount++;
            if (!Check(narratorCount == 1, "exactly one source plays the recorded flashback narration at " + second + "s")) yield break;
            if (!Check(second < 18f ? Mathf.Approximately(picture.rectTransform.localScale.x, 1f) :
                picture.rectTransform.localScale.x > 1.001f && picture.rectTransform.localScale.x < 1.0151f,
                "the film preserves moving shots and gives the held final shot its restrained camera push at " + second + "s")) yield break;
            lastFrame = video.frame;
            lastVoiceSample = voice.timeSamples;
            yield return SaveMemoryReviewCapture("06-flashback-" + second.ToString("00") + "s");
            if (finished) yield break;
        }
        while (video != null && video.time < 29.2d && Time.realtimeSinceStartup < movieDeadline)
        {
            heardMusicSamples |= EndingAudioHasSamples(music, output);
            yield return Keys(0.05f);
        }
        if (!Check(video != null && video.isPlaying && !flashback.NarrationPlaying &&
            !voice.isPlaying && music.volume > 0.99f && heardMusicSamples && !director.EndingVisible,
            "the narration finishes before the film, actual music samples were emitted, and its full level returns for the tail")) yield break;
        while (!director.EndingVisible && Time.realtimeSinceStartup < movieDeadline) yield return Keys(0.05f);
        // Destroy is deferred until end of frame; inspect after that cleanup boundary.
        yield return Keys(0.05f);
        var restored = true;
        for (var i = 0; i < sceneSources.Length; i++)
            restored &= sceneSources[i] == null || sceneSources[i].mute == originalMute[i];
        var cameraMasksRestored = true;
        foreach (var saved in sceneCameras)
            cameraMasksRestored &= saved.camera != null && saved.camera.isActiveAndEnabled &&
                saved.camera.cullingMask == saved.mask && saved.camera.clearFlags == saved.clearFlags;
        if (!Check(cameraMasksRestored,
            "actual film EOF restores every captured scene camera's original culling mask without disabling it or changing its clear settings")) yield break;
        if (!Check(naturalEnd && director.EndingVisible && !director.ReplayAvailable &&
            GameObject.Find("Ending Flashback") == null && GameObject.Find(EndingFlashbackPlayer.NarrationObjectName) == null &&
            video == null && voice == null && music == null && renderTarget == null && restored && !AudioListener.pause,
            "actual video EOF precedes the painting, destroys both audio sources and its render target, and restores scene audio")) yield break;
    }

    private static bool EndingAudioHasSamples(AudioSource source, float[] samples)
    {
        if (source == null) return false;
        source.GetOutputData(samples, 0);
        foreach (var sample in samples)
            if (Mathf.Abs(sample) > 0.00001f) return true;
        return false;
    }
}
#endif
