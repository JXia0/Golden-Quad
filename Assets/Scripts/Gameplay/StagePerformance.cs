using UnityEngine;

namespace LetGo
{
    /// <summary>Breathe, compose a two-note phrase, then speak it again in your own rhythm.</summary>
    [DefaultExecutionOrder(50)]
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
        private LineRenderer aimLine;
        private readonly LineRenderer[] listeners = new LineRenderer[2];
        private readonly LineRenderer[] phrases = new LineRenderer[3];
        private readonly bool[] rhythm = new bool[2];
        private readonly int[] addressedListeners = new int[2];
        private float aim;
        private int composingBeat;
        private int repriseBeat;
        private float retryUntil;
        private string activePrompt;
        public int SpokenPhrases => director == null ? 0 : director.CompletedObjectives;
        public int ComposedBeats => composingBeat;
        public int ReprisedBeats => repriseBeat;
        public bool FirstBeatLong => rhythm[0];
        public bool SecondBeatLong => rhythm[1];
        public int RepriseRetries { get; private set; }
        public float VoiceAim => aim;
        public int ExpectedListener => director.CompletedObjectives == 1 ? composingBeat == 0 ? -1 : 1 - addressedListeners[0] :
            director.CompletedObjectives == 2 ? addressedListeners[repriseBeat] : -1;
        public System.Action<int, Transform> PhraseSpoken;
        public System.Action<int, bool> NoteReleased;
        public System.Action RepriseMistimed;
        public System.Action<int, Vector3> AudienceAnswered;
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
            cue = visuals.GameplayLine("A breath becomes a phrase", JourneyVisuals.Warm);
            wave = visuals.Line("Voice travelling into the room", JourneyVisuals.Warm, 0.075f);
            aimLine = visuals.GameplayLine("Where the voice is going", JourneyVisuals.Warm);
            for (var i = 0; i < 2; i++) listeners[i] = visuals.GameplayLine("Listener response " + i, JourneyVisuals.Cool);
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
            var addressing = mark != null && director.CompletedObjectives > 0 && director.CompletedObjectives < 3;
            aimLine.enabled = addressing && hand.IsSelfAnchoring;
            for (var i = 0; i < listeners.Length; i++)
            {
                listeners[i].enabled = addressing;
                if (addressing) JourneyVisuals.Ring(listeners[i], ListenerPosition(mark, i), ExpectedListener == i ? 0.45f : 0.28f);
            }
            if (addressing && hand.IsSelfAnchoring)
            {
                aim = Mathf.Clamp(aim + GameInput.Horizontal * Time.deltaTime * 1.6f, -0.65f, 0.65f);
                aimLine.positionCount = 2;
                aimLine.SetPosition(0, hand.transform.position + Vector3.up * 0.4f);
                aimLine.SetPosition(1, mark.position + new Vector3(aim * 3f, 2f));
            }
            cue.enabled = mark != null;
            if (mark == null || hand.CurrentTarget != null)
            {
                armedAt = null;
                aim = 0f;
                ClearPrompt();
                if (director.CompletedObjectives == 1 && composingBeat < 2)
                {
                    composingBeat = 0;
                    phrases[1].positionCount = 0;
                }
            }
            else
            {
                JourneyVisuals.Ring(cue, mark.position, Mathf.Lerp(0.28f, 0.95f, hand.SelfChargeNormalized));
                if (hand.IsSelfAnchoring)
                {
                    if (armedAt != mark) armedTime = Time.time;
                    armedAt = mark;
                }
                SetPrompt(PromptForStep());
                if (armedAt == mark && hand.LastSelfReleaseTime > consumedRelease &&
                    hand.LastSelfReleaseTime >= armedTime && Time.time - hand.LastSelfReleaseTime < 0.25f)
                    ReleaseNote(mark);
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

        private string PromptForStep()
        {
            if (director.CompletedObjectives == 0)
                return hand.SelfChargeNormalized >= 0.98f ? ReleasePrompt : BreathPrompt;
            if (director.CompletedObjectives == 1)
                return composingBeat == 0 ? "按住 E · A / D 朝向一侧听众 · 松开发声" :
                    "另一侧也在听 · 按住 E，用 A / D 朝向另一侧，再松开";
            var score = ScoreNote(0) + "  " + ScoreNote(1);
            return (Time.time < retryUntil ? "再来一次 · " : "你的节奏 · ") + score +
                (repriseBeat == 0 ? "  |  第一拍" : "  |  第二拍") + "  A / D 朝向 · E 短 ● / 长 ━";
        }

        private void ReleaseNote(Transform mark)
        {
            consumedRelease = hand.LastSelfReleaseTime;
            armedAt = null;
            var duration = hand.LastSelfReleaseDuration;
            var step = director.CompletedObjectives;
            if (step == 0)
            {
                if (duration >= 1.15f) Speak(mark);
                return;
            }
            // Ignore accidental taps. Long and short are broad gestures, not a metronome test.
            if (duration < 0.12f) return;
            var listener = aim < 0f ? 0 : 1;
            var received = Mathf.Abs(aim - (listener == 0 ? -0.65f : 0.65f)) <= 0.18f;
            aim = 0f;
            if (!received || ExpectedListener >= 0 && listener != ExpectedListener) return;
            AudienceAnswered?.Invoke(listener, ListenerPosition(mark, listener));
            var isLong = duration >= 0.7f;
            if (step == 1)
            {
                rhythm[composingBeat] = isLong;
                addressedListeners[composingBeat] = listener;
                NoteReleased?.Invoke(composingBeat, isLong);
                composingBeat++;
                DrawNotes(phrases[1], mark.position, composingBeat);
                if (composingBeat < 2) return;
                JourneyChoices.RememberRhythm(rhythm[0], rhythm[1]);
                Speak(mark);
            }
            else if (step == 2)
            {
                // A generous overlap avoids classifying a borderline hold as a failure.
                var matches = rhythm[repriseBeat] ? duration >= 0.55f : duration <= 0.85f;
                if (!matches)
                {
                    repriseBeat = 0;
                    RepriseRetries++;
                    retryUntil = Time.time + 2f;
                    RepriseMistimed?.Invoke();
                    return;
                }
                NoteReleased?.Invoke(repriseBeat, rhythm[repriseBeat]);
                repriseBeat++;
                DrawNotes(phrases[2], mark.position, repriseBeat);
                if (repriseBeat == 2) Speak(mark);
            }
        }

        private string ScoreNote(int index) => (addressedListeners[index] == 0 ? "← " : "→ ") + (rhythm[index] ? "━" : "●");
        private static Vector3 ListenerPosition(Transform mark, int listener) => mark.position + new Vector3(listener == 0 ? -1.95f : 1.95f, 2f);

        private void DrawNotes(LineRenderer line, Vector3 origin, int count)
        {
            line.positionCount = count * 3;
            for (var i = 0; i < count; i++)
            {
                var center = origin + new Vector3(i * 0.85f - 0.45f, 0.6f);
                var width = rhythm[i] ? 0.55f : 0.16f;
                line.SetPosition(i * 3, center);
                line.SetPosition(i * 3 + 1, center + Vector3.right * width);
                line.SetPosition(i * 3 + 2, center);
            }
        }

        private void SetPrompt(string value)
        {
            ClearPrompt();
            activePrompt = value;
            director.ShowPrompt(value);
        }

        private void ClearPrompt()
        {
            director.ClearPrompt(activePrompt);
            activePrompt = null;
        }

        private void Speak(Transform mark)
        {
            var step = director.CompletedObjectives;
            armedAt = null;
            if (step == 1)
                JourneyChoices.Record("stage", mark == steady ? "我选择了离幕布更近的位置。" : "我选择向观众再靠近一步。", false);
            var phrase = phrases[step];
            if (step == 0)
            {
            phrase.positionCount = 21;
            for (var i = 0; i < 21; i++)
            {
                var t = i / 20f;
                phrase.SetPosition(i, mark.position + new Vector3((t - 0.5f) * 1.8f, 0.15f + Mathf.Sin(t * Mathf.PI * 4) * 0.12f, 0));
            }
            }
            waveOrigin = hand.transform.position + Vector3.up * 0.5f;
            waveStarted = Time.time;
            ClearPrompt();
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
