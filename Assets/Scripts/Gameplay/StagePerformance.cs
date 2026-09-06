using UnityEngine;

namespace LetGo
{
    /// <summary>Compose a phrase, then choose whether to own its ending or leave room for another voice.</summary>
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
        private readonly StageAudienceRelay relay = new StageAudienceRelay();
        private Transform relayMark;
        private Vector3 relayOrigin;
        private StageRelayPhase presentedRelayPhase;
        private StageEncorePhase presentedEncorePhase;
        private float firstComposedNoteAt;
        private float composedNoteSpacing = 1.3f;
        private bool tookBackLead;
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
        public StageAudienceRelay Relay => relay;
        public StageRelaySnapshot FinishedRelay { get; private set; }
        public int AudienceHandoffs { get; private set; }
        public int LeadTakebacks { get; private set; }
        public bool LastNoteWasAudience { get; private set; }
        public int LastSingingListener { get; private set; } = -1;
        public Vector3 RelayOrigin => relayOrigin;
        public bool AudienceContinuedWithoutPlayer => relay.AudienceContinuedWithoutPlayer;
        public int IndependentAudienceNotes => relay.EncoreNotesSung;
        private int ActiveStep => revising ? 1 : director.CompletedObjectives;
        public int ExpectedListener => AudienceLeading ? relay.Receiver :
            ActiveStep == 2 && repriseBeat < 2 ? addressedListeners[repriseBeat] : -1;
        public int AddressedListener(int beat) => addressedListeners[Mathf.Clamp(beat, 0, 1)];
        public System.Action<int, Transform> PhraseSpoken;
        public System.Action<int, bool> NoteReleased;
        public System.Action RepriseMistimed;
        public System.Action<int, Vector3> AudienceAnswered;
        public System.Action<int, bool, Vector3> AudiencePreparing;
        public System.Action<StageRelayOutcome, int, Vector3> RelayResponse;
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
            if (GameInput.IsPaused) return;
            if (hand == null) return;
            var mark = NearestMark();
            if (director.CompletedObjectives == 2 && (mark != null || AudienceLeading))
            {
                if (GameInput.ReconsiderPressed)
                {
                    revising = true; composingBeat = repriseBeat = 0;
                    ReadyForCurtain = AudienceLeading = false;
                    relay.Cancel();
                    FinishedRelay = default;
                    tookBackLead = false;
                    Revisions++;
                }
                if (!revising && GameInput.UsePressed && !hand.IsSelfAnchoring)
                {
                    if (ReadyForCurtain) { Speak(mark); return; }
                    if (AudienceLeading)
                    {
                        AudienceLeading = false;
                        relay.Cancel();
                        repriseBeat = 0;
                        tookBackLead = true;
                        LeadTakebacks++;
                    }
                    else BeginRelay(mark);
                }
            }
            // This remains alive after the bow: walking into the exit wing can reveal that
            // the room no longer needs the performer to carry its next phrase.
            if (GameInput.ReconsiderPressed) relay.CancelEncore();
            relay.TickEncore(Time.deltaTime, hand.transform.position.x - relayOrigin.x, hand.IsSelfAnchoring);
            PresentEncorePhase();
            if (AudienceLeading)
            {
                mark = relayMark;
                var responseReleasing = hand.LastSelfReleaseTime > consumedRelease &&
                    Time.time - hand.LastSelfReleaseTime < 0.25f;
                relay.Tick(Time.deltaTime, hand.transform.position.x - relayOrigin.x,
                    hand.IsSelfAnchoring || responseReleasing);
                PresentRelayPhase();
            }
            var addressing = mark != null && director.CompletedObjectives > 0 && director.CompletedObjectives < 3;
            aimLine.enabled = addressing && hand.IsSelfAnchoring;
            for (var i = 0; i < listeners.Length; i++)
            {
                listeners[i].enabled = addressing;
                if (addressing) JourneyVisuals.Ring(listeners[i], AudienceLeading ? RelayListenerPosition(i) : ListenerPosition(mark, i),
                    ExpectedListener == i ? 0.45f : 0.28f);
            }
            if (addressing && hand.IsSelfAnchoring)
            {
                aim = Mathf.Clamp(aim + GameInput.Horizontal * Time.deltaTime * 1.6f, -0.65f, 0.65f);
                aimLine.positionCount = 2;
                aimLine.SetPosition(0, hand.transform.position + Vector3.up * 0.4f);
                aimLine.SetPosition(1, (AudienceLeading ? relayOrigin : mark.position) + new Vector3(aim * 3f, 2f));
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
            if (AudienceLeading)
            {
                if (relay.Phase == StageRelayPhase.FirstVoice)
                    return "声音从观众席传回来。\n先听一听　F · 接回领唱";
                if (relay.Phase == StageRelayPhase.OfferedVoice)
                {
                    var direction = relay.Receiver == 0 ? "左边" : "右边";
                    if (hand.IsSelfAnchoring)
                        return hand.SelfChargeNormalized >= 0.58f ?
                            "松开 E · 陪他把这一拍唱长" : "朝" + direction + "松开 E · 给他起音";
                    return direction + "想接下一拍。\n走近 · 鼓励　E + A D · 起音或合唱　留在原地 · 听他唱完";
                }
                return relay.Outcome == StageRelayOutcome.Shared ? "这一拍，由你们一起唱完。" : "现在，听他的声音。";
            }
            if (ReadyForCurtain) return (FinishedRelay.Outcome == StageRelayOutcome.Accepted ? "最后一拍，成了他的声音。" :
                FinishedRelay.Outcome == StageRelayOutcome.Shared ? "你们一起把这句话唱完了。" :
                FinishedRelay.Outcome == StageRelayOutcome.Encouraged ? "你给了起点，他自己唱到了最后。" : "这一句唱完了。") +
                (relay.EncorePhase == StageEncorePhase.WaitingForSpace ? "\n可以退一步，听他们接下去。" : "") +
                "\nF · 谢幕　Q · 改写　按住 E · 再唱一次";
            if (ActiveStep == 0)
                return hand.SelfChargeNormalized >= 0.98f ? ReleasePrompt : BreathPrompt;
            if (ActiveStep == 1)
                return (composingBeat == 0 ? "第一拍，唱给谁听？" : "第二拍，留在这一侧，或转向另一侧。") +
                    "\n按住 E · 吸气　A D · 转向　松开 E · 唱出短音或长音";
            var score = ScoreNote(0) + "  " + ScoreNote(1);
            return (Time.time < retryUntil ? "再来一次：" : tookBackLead ? "你接回了领唱：" : "你的两拍：") + score +
                (repriseBeat == 0 ? "　从第一拍开始" : "　接上第二拍") +
                "\n按住 E · 重唱　A D · 转向　F · 请观众接唱　Q · 改写";
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
            if (AudienceLeading)
            {
                var aimedAt = Mathf.Abs(aim) >= 0.3f ? (aim < 0f ? 0 : 1) : -1;
                var offset = (hand.transform.position.x - relayOrigin.x) * (relay.Receiver == 0 ? -1f : 1f);
                if (relay.Respond(duration, aimedAt, offset >= 1.05f))
                {
                    PlayNote(1, duration >= 0.7f, false, -1);
                    PresentRelayPhase();
                }
                aim = 0f;
                return;
            }
            if (repriseBeat >= 2) repriseBeat = 0;
            var listener = aim < 0f ? 0 : 1;
            var received = Mathf.Abs(aim - (listener == 0 ? -0.65f : 0.65f)) <= 0.18f;
            aim = 0f;
            if (!received || ExpectedListener >= 0 && listener != ExpectedListener) return;
            AudienceAnswered?.Invoke(listener, ListenerPosition(mark, listener));
            var isLong = duration >= 0.7f;
            if (step == 1)
            {
                if (composingBeat == 0) firstComposedNoteAt = Time.time;
                else composedNoteSpacing = Time.time - firstComposedNoteAt;
                rhythm[composingBeat] = isLong;
                addressedListeners[composingBeat] = listener;
                PlayNote(composingBeat, isLong, false, -1);
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
                ReadyForCurtain = false;
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
                PlayNote(repriseBeat, rhythm[repriseBeat], false, -1);
                repriseBeat++;
                DrawNotes(phrases[2], mark.position, repriseBeat);
                if (repriseBeat == 2)
                {
                    ReadyForCurtain = true;
                    RememberFinishedPhrase(new StageRelaySnapshot(StageRelayOutcome.Solo, rhythm[0], rhythm[1],
                        addressedListeners[0], addressedListeners[1], 2));
                }
            }
        }

        private void BeginRelay(Transform mark)
        {
            if (mark == null) return;
            relayMark = mark;
            relayOrigin = mark.position;
            relayOrigin.x = hand.transform.position.x;
            AudienceLeading = true;
            ReadyForCurtain = false;
            tookBackLead = false;
            repriseBeat = 0;
            aim = 0f;
            AudienceHandoffs++;
            relay.Begin(rhythm[0], rhythm[1], addressedListeners[0], composedNoteSpacing);
            presentedRelayPhase = StageRelayPhase.Idle;
            presentedEncorePhase = StageEncorePhase.None;
            PresentRelayPhase();
        }

        private void PresentEncorePhase()
        {
            if (presentedEncorePhase == relay.EncorePhase) return;
            presentedEncorePhase = relay.EncorePhase;
            if (relay.EncorePhase != StageEncorePhase.FirstVoice && relay.EncorePhase != StageEncorePhase.SecondVoice) return;
            var firstVoice = relay.EncorePhase == StageEncorePhase.FirstVoice;
            var listener = firstVoice ? relay.EncoreLeader : relay.FirstListener;
            AudienceAnswered?.Invoke(listener, RelayListenerPosition(listener));
            PlayNote(firstVoice ? 0 : 1, firstVoice ? relay.EncoreFirstLong : relay.EncoreSecondLong, true, listener);
        }

        private void PresentRelayPhase()
        {
            if (presentedRelayPhase == relay.Phase) return;
            presentedRelayPhase = relay.Phase;
            switch (relay.Phase)
            {
                case StageRelayPhase.FirstVoice:
                    AudienceAnswered?.Invoke(relay.FirstListener, RelayListenerPosition(relay.FirstListener));
                    PlayNote(0, relay.FirstLong, true, relay.FirstListener);
                    break;
                case StageRelayPhase.OfferedVoice:
                    AudiencePreparing?.Invoke(relay.Receiver, relay.ProposedLong, RelayListenerPosition(relay.Receiver));
                    break;
                case StageRelayPhase.Answering:
                    var position = RelayListenerPosition(relay.Receiver);
                    RelayResponse?.Invoke(relay.Outcome, relay.Receiver, position);
                    AudienceAnswered?.Invoke(relay.Receiver, position);
                    PlayNote(1, relay.SecondLong, true, relay.Receiver);
                    break;
                case StageRelayPhase.Complete:
                    AudienceLeading = false;
                    ReadyForCurtain = true;
                    RememberFinishedPhrase(relay.Snapshot);
                    break;
            }
        }

        private void PlayNote(int beat, bool isLong, bool audience, int listener)
        {
            LastNoteWasAudience = audience;
            LastSingingListener = listener;
            NoteReleased?.Invoke(beat, isLong);
        }

        private void RememberFinishedPhrase(StageRelaySnapshot snapshot)
        {
            FinishedRelay = snapshot;
            JourneyChoices.RememberStageRelay(snapshot);
            JourneyChoices.RememberRhythm(snapshot.FirstLong, snapshot.SecondLong);
        }

        private string ScoreNote(int index) => (addressedListeners[index] == 0 ? "← " : "→ ") + (rhythm[index] ? "━" : "●");
        private static Vector3 ListenerPosition(Transform mark, int listener) => mark.position + new Vector3(listener == 0 ? -1.95f : 1.95f, 2f);
        private Vector3 RelayListenerPosition(int listener) => relayOrigin + new Vector3(listener == 0 ? -1.95f : 1.95f, 2f);

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
