using UnityEngine;

namespace LetGo
{
    /// <summary>Three phrases: breathe, choose a place to speak, finish despite the empty seat.</summary>
    public sealed class StagePerformance : MonoBehaviour
    {
        private StorySceneDirector director;
        private HandConnection hand;
        private Transform first, steady, forward, final;
        private Transform armedAt;
        private float armedTime;
        private float consumedRelease = -100f;
        private float waveStarted = -100f;
        private Vector3 waveOrigin;
        private LineRenderer cue, wave;
        private readonly LineRenderer[] phrases = new LineRenderer[3];
        public int SpokenPhrases => director == null ? 0 : director.CompletedObjectives;
        public System.Action<int, Transform> PhraseSpoken;
        private const string BreathPrompt = "按住 E · 吸气";
        private const string ReleasePrompt = "松开 E · 把声音送出去";

        public void Initialize(StorySceneDirector sceneDirector, JourneyVisuals visuals)
        {
            director = sceneDirector;
            hand = director.Player.GetComponent<HandConnection>();
            first = EmotionalJourney.Named("First Breath Cue");
            steady = EmotionalJourney.Named("Steady Route");
            forward = EmotionalJourney.Named("Forward Route");
            final = EmotionalJourney.Named("Final Release Cue");
            if (first == null || steady == null || forward == null || final == null)
            { Debug.LogError("[LetGo] Stage is missing a performance mark."); enabled = false; return; }
            foreach (var old in FindObjectsByType<SelfAnchorCue>()) old.enabled = false;
            foreach (var old in FindObjectsByType<FinalReleaseCue>()) old.enabled = false;
            foreach (var old in FindObjectsByType<StageRouteChoiceTrigger>()) old.enabled = false;
            var platform = EmotionalJourney.Named("Forward Platform");
            if (platform != null)
            {
                // A high speaking position needs somewhere to stand while breathing.
                var collider = platform.GetComponent<BoxCollider2D>();
                if (collider == null) collider = platform.gameObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = false;
                collider.size = new Vector2(2.8f / Mathf.Abs(platform.lossyScale.x), 0.3f / Mathf.Abs(platform.lossyScale.y));
                var effector = platform.GetComponent<PlatformEffector2D>();
                if (effector == null) effector = platform.gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                collider.usedByEffector = true;
                director.Player.GetComponent<PlayerController2D>().SetJumpForce(8.8f);
            }
            cue = visuals.Line("A breath becomes a phrase", JourneyVisuals.Warm);
            wave = visuals.Line("Voice travelling into the room", JourneyVisuals.Warm, 0.075f);
            for (var i = 0; i < phrases.Length; i++) phrases[i] = visuals.Line("Spoken phrase " + i, JourneyVisuals.Warm, 0.09f);
        }

        private Transform NearestMark()
        {
            var step = director.CompletedObjectives;
            if (step == 0) return Vector2.Distance(hand.transform.position, first.position) <= 1.7f ? first : null;
            if (step == 2) return Vector2.Distance(hand.transform.position, final.position) <= 1.7f ? final : null;
            if (step != 1) return null;
            // Explicitly distinguish the low and high marks; crossing one never commits a choice.
            var a = Vector2.Distance(hand.transform.position, steady.position);
            var b = Vector2.Distance(hand.transform.position, forward.position);
            return Mathf.Min(a, b) > 1.5f ? null : a <= b ? steady : forward;
        }

        private void Update()
        {
            if (hand == null) return;
            var mark = NearestMark();
            cue.enabled = mark != null;
            if (mark == null || hand.CurrentTarget != null)
            {
                armedAt = null;
                director.ClearPrompt(BreathPrompt);
                director.ClearPrompt(ReleasePrompt);
            }
            else
            {
                JourneyVisuals.Ring(cue, mark.position, Mathf.Lerp(0.28f, 0.95f, hand.SelfChargeNormalized));
                if (hand.IsSelfAnchoring && hand.SelfChargeNormalized >= 0.98f)
                {
                    armedAt = mark;
                    armedTime = Time.time;
                }
                director.ShowPrompt(armedAt == mark ? ReleasePrompt : BreathPrompt);
                if (armedAt == mark && hand.LastFullSelfReleaseTime > consumedRelease &&
                    hand.LastFullSelfReleaseTime >= armedTime && Time.time - hand.LastFullSelfReleaseTime < 0.25f)
                    Speak(mark);
            }
            var elapsed = Time.time - waveStarted;
            wave.enabled = elapsed < 1.6f;
            if (wave.enabled)
            {
                JourneyVisuals.Ring(wave, waveOrigin + Vector3.up * elapsed * 0.45f, 0.3f + elapsed * 2f);
                var color = JourneyVisuals.Warm;
                color.a = 1f - elapsed / 1.6f;
                wave.startColor = wave.endColor = color;
            }
        }

        private void Speak(Transform mark)
        {
            var step = director.CompletedObjectives;
            consumedRelease = hand.LastFullSelfReleaseTime;
            armedAt = null;
            if (step == 1)
                JourneyChoices.Record("stage", mark == steady ? "我选择了离幕布更近的位置。" : "我选择向观众再靠近一步。", false);
            var phrase = phrases[step];
            phrase.positionCount = 21;
            for (var i = 0; i < 21; i++)
            {
                var t = i / 20f;
                phrase.SetPosition(i, mark.position + new Vector3((t - 0.5f) * 1.8f, 0.15f + Mathf.Sin(t * Mathf.PI * 4) * 0.12f, 0));
            }
            waveOrigin = hand.transform.position + Vector3.up * 0.5f;
            waveStarted = Time.time;
            director.ClearPrompt(ReleasePrompt);
            director.CompleteObjective();
            PhraseSpoken?.Invoke(step + 1, mark);
            var environment = FindAnyObjectByType<EmotionalEnvironment>();
            if (environment != null) environment.Resolution01 = director.CompletedObjectives / 3f;
            if (step == 2)
            {
                SceneAudio.Instance?.PlayApplause();
            }
        }
    }
}
