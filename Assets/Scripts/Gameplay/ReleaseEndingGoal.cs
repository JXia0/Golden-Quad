using UnityEngine;

namespace LetGo
{
    [DefaultExecutionOrder(50)]
    public sealed class ReleaseEndingGoal : MonoBehaviour
    {
        [SerializeField] private HandConnection connection;
        [SerializeField] private HoldTarget recipient;
        [SerializeField] private float transferSeconds = 1.4f;
        private bool ready;
        private bool finished;
        private bool departing;
        private bool gone;
        private float goneAt;
        private float onwardX;
        private Transform door;
        private bool wasHeld;
        private float consumedRelease = -100f;
        private float readyAt;
        private int readinessBeat = -1;
        private LineRenderer breath;
        private LineRenderer onward;
        public bool IsDeparting => departing;
        public bool HasEnteredDoor => gone;
        public bool IsReadyToLeave => ready;
        public bool IsHeldBack { get; private set; }
        public int TimesReheld { get; private set; }
        public float OnwardPositionX => onwardX;
        public int ReadinessBeat => readinessBeat;
        public bool HideRecipientOnEntry { get; set; } = true;
        public System.Action RecipientDeparted;
        public System.Action RecipientEnteredDoor;
        public System.Action ReadyToLeave;
        public System.Action RecipientReheld;
        public System.Action<int, bool> RecipientBreath;
        private const string ReleasePrompt = "松开 E";
        private const string ContinuePrompt = "D / →";

        public void Configure(HandConnection handConnection, HoldTarget target, float seconds)
        {
            connection = handConnection;
            recipient = target;
            transferSeconds = seconds;
        }

        private void Update()
        {
            if (finished || connection == null || recipient == null) return;
            if (door == null)
            {
                door = EmotionalJourney.Named("Unknown Door");
                if (door == null) return;
                if (JourneyChoices.HasStageRhythm)
                    transferSeconds = BeatDuration(JourneyChoices.FirstBeatLong) + BeatDuration(JourneyChoices.SecondBeatLong) + 0.3f;
                var visuals = FindAnyObjectByType<JourneyVisuals>();
                if (visuals != null)
                {
                    breath = visuals.Line("The next person's breath", JourneyVisuals.Warm);
                    onward = visuals.Line("One more step", JourneyVisuals.Warm, 0.035f);
                }
            }
            if (departing)
            {
                if (breath != null) breath.enabled = false;
                var held = connection.CurrentTarget == recipient;
                IsHeldBack = held;
                if (held && !wasHeld)
                {
                    TimesReheld++;
                    RecipientReheld?.Invoke();
                }
                wasHeld = held;
                // Reaching for the departing person pauses them; releasing lets them continue.
                // There is no penalty, and progress is never reset by a second goodbye.
                if (!gone && !held) StepTowards(door.position.x);
                if (held) StorySceneDirector.Instance?.ShowPrompt(ReleasePrompt);
                else StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
                if (!gone && Mathf.Abs(recipient.transform.position.x - door.position.x) < 0.1f)
                {
                    gone = true;
                    goneAt = Time.time;
                    // Even a player who walked ahead must take a step after the hand is empty.
                    onwardX = Mathf.Max(door.position.x + 1.2f, connection.transform.position.x + 0.8f);
                    recipient.MarkPlaced(recipient.transform.position);
                    if (HideRecipientOnEntry)
                        foreach (var renderer in recipient.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
                    RecipientEnteredDoor?.Invoke();
                    SceneAudio.Instance?.PlayDoor();
                }
                if (!gone) return;
                if (onward != null)
                {
                    onward.positionCount = 3;
                    var p = new Vector3(onwardX + 0.2f, connection.transform.position.y, 0);
                    onward.SetPosition(0, p + new Vector3(-0.3f, -0.15f));
                    onward.SetPosition(1, p);
                    onward.SetPosition(2, p + new Vector3(-0.3f, 0.15f));
                }
                if (Time.time - goneAt > 4f) StorySceneDirector.Instance?.ShowPrompt(ContinuePrompt);
                if (connection.transform.position.x < onwardX) return;
                finished = true;
                StorySceneDirector.Instance?.ClearPrompt(ContinuePrompt);
                StorySceneDirector.Instance?.ShowOpenEnding();
                return;
            }
            if (connection.CurrentTarget == recipient)
            {
                if (!ready && JourneyChoices.HasStageRhythm)
                {
                    var beat = recipient.HeldDuration < BeatDuration(JourneyChoices.FirstBeatLong) + 0.3f ? 0 : 1;
                    if (beat != readinessBeat)
                    {
                        readinessBeat = beat;
                        RecipientBreath?.Invoke(beat, beat == 0 ? JourneyChoices.FirstBeatLong : JourneyChoices.SecondBeatLong);
                    }
                }
                if (!ready && recipient.HeldDuration >= transferSeconds)
                {
                    ready = true;
                    readyAt = Time.time;
                    ReadyToLeave?.Invoke();
                }
                if (breath != null)
                {
                    breath.enabled = true;
                    JourneyVisuals.Ring(breath, recipient.transform.position + Vector3.up * 0.3f,
                        Mathf.Lerp(0.15f, 0.65f, Mathf.Clamp01(recipient.HeldDuration / transferSeconds)));
                }
                if (ready)
                {
                    var limit = Mathf.Min(door.position.x - 0.7f,
                        connection.transform.position.x + recipient.MaxDistance * 0.85f);
                    StepTowards(Mathf.Max(recipient.transform.position.x, limit));
                    IsHeldBack = recipient.transform.position.x >= limit - 0.08f;
                    // First let the attempted step communicate the situation; offer a control hint later.
                    if (Time.time - readyAt > 2.5f) StorySceneDirector.Instance?.ShowPrompt(ReleasePrompt);
                }
                return;
            }
            if (breath != null) breath.enabled = false;
            IsHeldBack = false;
            if (!ready || connection.LastReleasedTarget != recipient || connection.LastReleaseTime <= consumedRelease ||
                Time.time - connection.LastReleaseTime > 0.25f)
            {
                StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
                return;
            }
            consumedRelease = connection.LastReleaseTime;
            departing = true;
            wasHeld = false;
            StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
            RecipientDeparted?.Invoke();
        }

        private void StepTowards(float x)
        {
            var position = recipient.transform.position;
            position.x = Mathf.MoveTowards(position.x, x, 1.25f * Time.deltaTime);
            recipient.transform.position = position;
        }

        private static float BeatDuration(bool isLong) => isLong ? 0.85f : 0.45f;
    }
}
