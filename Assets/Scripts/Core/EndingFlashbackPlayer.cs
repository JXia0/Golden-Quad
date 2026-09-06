using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace LetGo
{
    // Optional film between FinalWalk's black fade and the delivered ending artwork.
    public sealed class EndingFlashbackPlayer : MonoBehaviour
    {
        public const string ResourcePath = "Ending/flashback";
        public const string NarrationObjectName = "Flashback Narration";
        private const double NarrationStartSeconds = 0.5d;
        private const float NarrationMusicVolume = 0.3f;
        private const double PrepareTimeout = 8d;
        private const double FrameTimeout = 5d;
        private const double MaximumPlaybackSeconds = 120d;

        private GameObject presentation;
        private VideoPlayer video;
        private AudioSource filmAudio;
        private AudioSource narrationAudio;
        private RawImage picture;
        private RenderTexture texture;
        private SceneAudio mutedSceneAudio;
        private bool reachedEnd;
        private bool receivedFrame;
        private bool narrationStarted;
        private bool previousRunInBackground;
        private bool restoreRunInBackground;
        private string failure;

        public bool Playing { get; private set; }
        public bool NarrationPlaying => narrationAudio != null && narrationAudio.isPlaying;

        public IEnumerator PlayIfAvailable()
        {
            if (Playing || !isActiveAndEnabled) yield break;
            var clip = Resources.Load<VideoClip>(ResourcePath);
            // Do not reserve an empty thirty seconds while the film is still in production.
            if (clip == null) yield break;

            Playing = true;
            reachedEnd = false;
            receivedFrame = false;
            narrationStarted = false;
            failure = null;
            try
            {
                // Keep decoder progress and wall-clock timeouts aligned while this film is active.
                previousRunInBackground = Application.runInBackground;
                restoreRunInBackground = true;
                Application.runInBackground = true;
                if (!TryPrepare(clip)) yield break;
                var deadline = Time.realtimeSinceStartupAsDouble + PrepareTimeout;
                while (Playing && video != null && !video.isPrepared && failure == null &&
                    Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                if (!Playing || video == null) yield break;
                if (failure != null || !video.isPrepared)
                {
                    Warn(failure ?? "Preparation timed out.");
                    yield break;
                }

                if (!TryStartPlayback()) yield break;
                var startedAt = Time.realtimeSinceStartupAsDouble;
                var length = video.length;
                var playbackLimit = double.IsNaN(length) || double.IsInfinity(length) || length <= 0d
                    ? MaximumPlaybackSeconds : Math.Min(MaximumPlaybackSeconds, Math.Max(10d, length + 8d));
                var lastProgressAt = startedAt;
                long lastFrame = -1;
                while (Playing && video != null && !reachedEnd && failure == null)
                {
                    var now = Time.realtimeSinceStartupAsDouble;
                    var frame = video.frame;
                    if (frame >= 0 && frame != lastFrame)
                    {
                        lastFrame = frame;
                        lastProgressAt = now;
                        receivedFrame = true;
                    }
                    // Never show an uninitialized render target while the decoder starts.
                    if (receivedFrame && !picture.enabled) picture.enabled = true;
                    UpdateNarration();
                    if (now - lastProgressAt >= FrameTimeout)
                    {
                        failure = receivedFrame ? "Playback stopped producing frames." : "First frame timed out.";
                        break;
                    }
                    if (now - startedAt >= playbackLimit)
                    {
                        failure = "Playback exceeded its time limit.";
                        break;
                    }
                    yield return null;
                }
                if (failure != null) Warn(failure);
            }
            finally
            {
                Cleanup();
            }
        }

        private bool TryPrepare(VideoClip clip)
        {
            try
            {
                presentation = new GameObject("Ending Flashback", typeof(RectTransform), typeof(Canvas));
                presentation.transform.SetParent(transform, false);
                var canvas = presentation.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32759;

                var backing = new GameObject("Flashback Black", typeof(RectTransform), typeof(Image));
                backing.transform.SetParent(presentation.transform, false);
                var backingImage = backing.GetComponent<Image>();
                Stretch(backingImage.rectTransform);
                backingImage.color = Color.black;
                backingImage.raycastTarget = false;

                var frame = new GameObject("Flashback Picture", typeof(RectTransform), typeof(RawImage),
                    typeof(AspectRatioFitter));
                frame.transform.SetParent(presentation.transform, false);
                picture = frame.GetComponent<RawImage>();
                picture.enabled = false;
                picture.raycastTarget = false;
                var pixelAspect = clip.pixelAspectRatioDenominator == 0 ? 1f :
                    (float)clip.pixelAspectRatioNumerator / clip.pixelAspectRatioDenominator;
                var width = Mathf.Max(1f, clip.width);
                var height = Mathf.Max(1f, clip.height);
                var fitter = frame.GetComponent<AspectRatioFitter>();
                fitter.aspectRatio = width / height * Mathf.Max(0.001f, pixelAspect);
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

                var maximumDimension = Mathf.Min(2048, SystemInfo.maxTextureSize);
                var scale = Mathf.Min(1f, maximumDimension / Mathf.Max(width, height));
                texture = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(width * scale)),
                    Mathf.Max(1, Mathf.RoundToInt(height * scale)), 0, RenderTextureFormat.ARGB32)
                {
                    name = "Ending Flashback Render Texture",
                    useMipMap = false,
                    autoGenerateMips = false
                };
                if (!texture.Create()) throw new InvalidOperationException("Could not create the video render target.");
                var previousTarget = RenderTexture.active;
                try
                {
                    RenderTexture.active = texture;
                    GL.Clear(true, true, Color.black);
                }
                finally
                {
                    RenderTexture.active = previousTarget;
                }
                picture.texture = texture;

                filmAudio = presentation.AddComponent<AudioSource>();
                filmAudio.playOnAwake = false;
                filmAudio.loop = false;
                filmAudio.spatialBlend = 0f;
                filmAudio.volume = 1f;
                PrepareNarration();

                video = presentation.AddComponent<VideoPlayer>();
                video.playOnAwake = false;
                video.isLooping = false;
                video.source = VideoSource.VideoClip;
                video.clip = clip;
                video.renderMode = VideoRenderMode.RenderTexture;
                video.targetTexture = texture;
                // RawImage's aspect fitter applies pixel aspect correction exactly once.
                video.aspectRatio = VideoAspectRatio.Stretch;
                video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
                video.audioOutputMode = clip.audioTrackCount > 0
                    ? VideoAudioOutputMode.AudioSource : VideoAudioOutputMode.None;
                video.controlledAudioTrackCount = (ushort)(clip.audioTrackCount > 0 ? 1 : 0);
                if (clip.audioTrackCount > 0)
                {
                    // The delivered film has music; its separate user-recorded narration is mixed at runtime.
                    video.EnableAudioTrack(0, true);
                    video.SetTargetAudioSource(0, filmAudio);
                }
                video.errorReceived += OnVideoError;
                video.loopPointReached += OnVideoEnded;
                video.frameReady += OnFrameReady;
                video.sendFrameReadyEvents = true;
                mutedSceneAudio = SceneAudio.Instance;
                mutedSceneAudio?.SetFilmPlaybackMuted(true);
                video.Prepare();
                return true;
            }
            catch (Exception exception)
            {
                Warn(exception.Message);
                return false;
            }
        }

        private bool TryStartPlayback()
        {
            try
            {
                video.Play();
                return true;
            }
            catch (Exception exception)
            {
                Warn(exception.Message);
                return false;
            }
        }

        private void PrepareNarration()
        {
            var voice = Resources.Load<SceneAudioLibrary>("SceneAudioLibrary")?.flashbackVoice;
            if (voice == null) return;
            // The library reference keeps the recording at its original Assets/Audio/Voice path.
            if (voice.loadState != AudioDataLoadState.Loaded && !voice.LoadAudioData())
            {
                Debug.LogWarning("Ending flashback narration could not load; continuing with the film audio.", this);
                return;
            }
            var narrator = new GameObject(NarrationObjectName, typeof(AudioSource));
            narrator.transform.SetParent(presentation.transform, false);
            narrationAudio = narrator.GetComponent<AudioSource>();
            narrationAudio.playOnAwake = false;
            narrationAudio.loop = false;
            narrationAudio.spatialBlend = 0f;
            narrationAudio.volume = 1f;
            narrationAudio.clip = voice;
        }

        private void UpdateNarration()
        {
            // Use the decoded video timeline so a slow Prepare/start never consumes the voice lead-in.
            if (!narrationStarted && narrationAudio != null && receivedFrame &&
                video.time >= NarrationStartSeconds)
            {
                narrationStarted = true;
                narrationAudio.Play();
            }
            var targetVolume = NarrationPlaying ? NarrationMusicVolume : 1f;
            var fadeSeconds = NarrationPlaying ? 0.4f : 0.8f;
            filmAudio.volume = Mathf.MoveTowards(filmAudio.volume, targetVolume,
                (1f - NarrationMusicVolume) * Time.unscaledDeltaTime / fadeSeconds);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnVideoError(VideoPlayer source, string message) => failure = message;
        private void OnVideoEnded(VideoPlayer source) => reachedEnd = true;
        private void OnFrameReady(VideoPlayer source, long frame) => receivedFrame = true;
        private void Warn(string message) => Debug.LogWarning("Ending flashback skipped: " + message, this);

        private void Cleanup()
        {
            Playing = false;
            if (restoreRunInBackground)
            {
                restoreRunInBackground = false;
                Application.runInBackground = previousRunInBackground;
            }
            // Hide first, so no released texture or last film frame can cover the closing picture.
            if (presentation != null) presentation.SetActive(false);
            if (video != null)
            {
                video.errorReceived -= OnVideoError;
                video.loopPointReached -= OnVideoEnded;
                video.frameReady -= OnFrameReady;
                video.Stop();
                video.targetTexture = null;
                video.clip = null;
            }
            if (filmAudio != null) filmAudio.Stop();
            if (narrationAudio != null)
            {
                narrationAudio.Stop();
                narrationAudio.clip = null;
            }
            if (picture != null) picture.texture = null;
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
            if (presentation != null) Destroy(presentation);
            mutedSceneAudio?.SetFilmPlaybackMuted(false);
            mutedSceneAudio = null;
            presentation = null;
            video = null;
            filmAudio = null;
            narrationAudio = null;
            narrationStarted = false;
            picture = null;
            texture = null;
        }

        private void OnDisable() => Cleanup();
        private void OnDestroy() => Cleanup();
    }
}
