using UnityEngine;

namespace LetGo
{
    /// <summary>Turns the stage rhythm events into light on the floor instead of debug rings.</summary>
    [DefaultExecutionOrder(110)]
    public sealed class StageFeedbackPresentation : MonoBehaviour
    {
        private StagePerformance performance;
        private SpriteRenderer voiceLight;
        private readonly SpriteRenderer[] audienceLights = new SpriteRenderer[2];
        private float voiceUntil;
        private float voiceDuration;
        private readonly float[] audienceUntil = new float[2];
        private AudioSource source;
        private AudioClip shortNote, longNote, answerLeft, answerRight;
        private float pendingAudienceAt = -1f;
        private int pendingAudience = -1;
        private static Sprite softPool;
        public int PlayedNotes { get; private set; }
        public int PlayedAudienceAnswers { get; private set; }
        public bool LastNoteLong { get; private set; }
        public bool IsPlayingNote => source != null && source.isPlaying;

        private void Start()
        {
            performance = FindAnyObjectByType<StagePerformance>();
            if (performance == null) { enabled = false; return; }
            EnsureSprite();
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            shortNote = Tone("Short stage note", 523.25f, 0.24f, 0.14f);
            longNote = Tone("Long stage note", 392f, 0.62f, 0.16f);
            answerLeft = Tone("Left audience answer", 659.25f, 0.32f, 0.1f);
            answerRight = Tone("Right audience answer", 783.99f, 0.32f, 0.1f);
            voiceLight = CreatePool("Voice light", JourneyVisuals.Warm, 14);
            audienceLights[0] = CreatePool("Left audience answer", JourneyVisuals.Cool, 12);
            audienceLights[1] = CreatePool("Right audience answer", JourneyVisuals.Cool, 12);
            audienceLights[0].transform.localScale = new Vector3(0.22f, 0.32f, 1f);
            audienceLights[1].transform.localScale = new Vector3(0.22f, 0.32f, 1f);
            performance.NoteReleased += OnNoteReleased;
            performance.AudienceAnswered += OnAudienceAnswered;
        }

        private void OnNoteReleased(int beat, bool isLong)
        {
            PlayedNotes++;
            LastNoteLong = isLong;
            if (source != null) source.PlayOneShot(isLong ? longNote : shortNote);
            voiceDuration = isLong ? 0.9f : 0.45f;
            voiceUntil = Time.time + voiceDuration;
            var player = FindAnyObjectByType<PlayerController2D>();
            if (player != null)
                voiceLight.transform.position = player.transform.position + new Vector3(0f, -0.45f, 0f);
            voiceLight.transform.localScale = new Vector3(isLong ? 1.45f : 0.72f, 0.2f, 1f);
        }

        private void OnAudienceAnswered(int listener, Vector3 position)
        {
            if (listener < 0 || listener >= audienceLights.Length) return;
            audienceUntil[listener] = Time.time + 0.7f;
            pendingAudience = listener;
            pendingAudienceAt = Time.time + 0.14f;
            // The response belongs to the audience bank, not to a floating target above it.
            audienceLights[listener].transform.position = position;
        }

        private void LateUpdate()
        {
            if (pendingAudience >= 0 && Time.time >= pendingAudienceAt)
            {
                if (source != null) source.PlayOneShot(pendingAudience == 0 ? answerLeft : answerRight);
                PlayedAudienceAnswers++;
                pendingAudience = -1;
                pendingAudienceAt = -1f;
            }
            if (voiceLight != null)
                SetPulse(voiceLight, voiceUntil, voiceDuration, 0.28f);
            for (var i = 0; i < audienceLights.Length; i++)
                if (audienceLights[i] != null) SetPulse(audienceLights[i], audienceUntil[i], 0.7f, 0.22f);
        }

        private static void SetPulse(SpriteRenderer renderer, float until, float duration, float maximumAlpha)
        {
            var remaining = until - Time.time;
            renderer.enabled = remaining > 0f;
            if (!renderer.enabled) return;
            var progress = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.01f, duration));
            var color = renderer.color;
            color.a = Mathf.Sin(progress * Mathf.PI) * maximumAlpha;
            renderer.color = color;
        }

        private static SpriteRenderer CreatePool(string name, Color color, int order)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = softPool;
            renderer.color = new Color(color.r, color.g, color.b, 0f);
            renderer.sortingOrder = order;
            renderer.transform.localScale = new Vector3(1.1f, 0.18f, 1f);
            renderer.enabled = false;
            return renderer;
        }

        private static void EnsureSprite()
        {
            if (softPool != null) return;
            const int width = 96;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = "Stage soft floor light", wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var dx = ((x + 0.5f) / width - 0.5f) * 2f;
                var dy = ((y + 0.5f) / height - 0.5f) * 2f;
                var alpha = Mathf.Pow(Mathf.Clamp01(1f - dx * dx - dy * dy), 2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            softPool = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f, 32f,
                0, SpriteMeshType.FullRect);
        }

        private static AudioClip Tone(string name, float frequency, float seconds, float volume)
        {
            const int rate = 44100;
            var samples = Mathf.CeilToInt(rate * seconds);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)rate;
                var fadeIn = Mathf.Clamp01(t / 0.025f);
                var fadeOut = Mathf.Clamp01((seconds - t) / 0.1f);
                var envelope = fadeIn * fadeOut;
                data[i] = (Mathf.Sin(2f * Mathf.PI * frequency * t) +
                    0.24f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * t)) * envelope * volume;
            }
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (performance == null) return;
            performance.NoteReleased -= OnNoteReleased;
            performance.AudienceAnswered -= OnAudienceAnswered;
            if (shortNote != null) Destroy(shortNote);
            if (longNote != null) Destroy(longNote);
            if (answerLeft != null) Destroy(answerLeft);
            if (answerRight != null) Destroy(answerRight);
        }
    }
}
