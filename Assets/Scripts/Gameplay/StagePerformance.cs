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
        private bool revising;
        private float audienceTime;
        private int audienceBeat;
        public bool AudienceLeading { get; private set; }
        public bool ReadyForCurtain { get; private set; }
        public int Revisions { get; private set; }
        private string activePrompt;
        public int SpokenPhrases => director == null ? 0 : director.CompletedObjectives;
        public int ComposedBeats => composingBeat;
        public int ReprisedBeats => repriseBeat;
        public bool FirstBeatLong => rhythm[0];
        public bool SecondBeatLong => rhythm[1];
        public int RepriseRetries { get; private set; }
        public float VoiceAim => aim;
        private int ActiveStep => revising ? 1 : director.CompletedObjectives;
        public int ExpectedListener => ActiveStep == 2 && repriseBeat < 2 ? addressedListeners[repriseBeat] : -1;
        public int AddressedListener(int beat) => addressedListeners[Mathf.Clamp(beat, 0, 1)];
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
            if (director.ObjectivesComplete) return null;
            Transform nearest = null;
            var distance = 1.7f;
            foreach (var mark in new[] { first, steady, forward, final })
            {
                var d = Vector2.Distance(hand.transform.position, mark.position);
                if (d >= distance) continue;
                distance = d; nearest = mark;
            }
            return nearest;
        }

        private void Update()
        {
            if (hand == null) return;
            var mark = NearestMark();
            if (director.CompletedObjectives == 2 && mark != null)
            {
                if (GameInput.ReconsiderPressed)
                {
                    revising = true; composingBeat = repriseBeat = 0;
                    ReadyForCurtain = AudienceLeading = false;
                    Revisions++;
                }
                if (!revising && GameInput.UsePressed && !hand.IsSelfAnchoring)
                {
                    if (ReadyForCurtain) { Speak(mark); return; }
                    if (!AudienceLeading) { AudienceLeading = true; audienceTime = Time.time; audienceBeat = 0; }
                }
            }
            if (AudienceLeading)
            {
                if (GameInput.InteractPressed) { AudienceLeading = false; repriseBeat = 0; }
                else
                {
                    var elapsedReply = Time.time - audienceTime;
                    var secondAt = (rhythm[0] ? 0.85f : 0.45f) + 0.6f;
                    if (audienceBeat == 0 || audienceBeat == 1 && elapsedReply >= secondAt)
                    {
                        var index = audienceBeat++;
                        AudienceAnswered?.Invoke(addressedListeners[index], ListenerPosition(mark != null ? mark : final, addressedListeners[index]));
                        NoteReleased?.Invoke(index, rhythm[index]);
                    }
                    if (elapsedReply >= secondAt + (rhythm[1] ? 0.85f : 0.45f) + 0.6f)
                    { AudienceLeading = false; ReadyForCurtain = true; }
                }
            }
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
            if (AudienceLeading) return "他们正在接过你的节奏 · 可以等一会儿，也可以按 E 加入";
            if (ReadyForCurtain) return "F · 谢幕　Q · 改写节奏　也可以换一个位置继续唱";
            if (ActiveStep == 0)
                return hand.SelfChargeNormalized >= 0.98f ? ReleasePrompt : BreathPrompt;
            if (ActiveStep == 1)
                return composingBeat == 0 ? "按住 E · A / D 朝向一侧听众 · 松开发声" :
                    "再发一拍 · 可以回应同一侧，也可以用 A / D 转向另一侧";
            var score = ScoreNote(0) + "  " + ScoreNote(1);
            return (Time.time < retryUntil ? "再来一次 · " : "你的节奏 · ") + score +
                (repriseBeat == 0 ? "  |  第一拍" : "  |  第二拍") + "  E 演唱 / F 请观众接唱 / Q 改写";
        }

        private void ReleaseNote(Transform mark)
        {
            consumedRelease = hand.LastSelfReleaseTime;
            armedAt = null;
            var duration = hand.LastSelfReleaseDuration;
            var step = ActiveStep;
            if (duration >= 0.12f) director.Player.GetComponent<CharacterAnimationDriver>()?.PlayStageGesture();
            if (step == 0)
            {
                if (duration >= 1.15f) Speak(mark);
                return;
            }
            // Ignore accidental taps. Long and short are broad gestures, not a metronome test.
            if (duration < 0.12f) return;
            if (repriseBeat >= 2) repriseBeat = 0;
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
                if (revising)
                {
                    revising = false; repriseBeat = 0;
                    JourneyChoices.Record("stage", mark == forward ? "我选择向观众再靠近一步。" : "我选择了离幕布更近的位置。", false);
                }
                else Speak(mark);
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
                if (repriseBeat == 2) ReadyForCurtain = true;
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
                JourneyChoices.Record("stage", mark == forward ? "我选择向观众再靠近一步。" : "我选择了离幕布更近的位置。", false);
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
