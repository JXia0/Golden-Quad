using UnityEngine;

namespace LetGo
{
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
        private Transform door;
        private LineRenderer breath;
        private LineRenderer onward;
        public bool IsDeparting => departing;
        public bool HasEnteredDoor => gone;
        public bool HideRecipientOnEntry { get; set; } = true;
        public System.Action RecipientDeparted;
        public System.Action RecipientEnteredDoor;
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
                if (!gone && Mathf.Abs(recipient.transform.position.x - door.position.x) < 0.1f)
                {
                    gone = true;
                    goneAt = Time.time;
                    if (HideRecipientOnEntry)
                        foreach (var renderer in recipient.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
                    RecipientEnteredDoor?.Invoke();
                    connection.GetComponent<PlayerController2D>().ControlsEnabled = true;
                    SceneAudio.Instance?.PlayDoor();
                }
                if (!gone) return;
                if (onward != null)
                {
                    onward.positionCount = 3;
                    var p = new Vector3(door.position.x + 1.4f, connection.transform.position.y, 0);
                    onward.SetPosition(0, p + new Vector3(-0.3f, -0.15f));
                    onward.SetPosition(1, p);
                    onward.SetPosition(2, p + new Vector3(-0.3f, 0.15f));
                }
                if (Time.time - goneAt > 4f) StorySceneDirector.Instance?.ShowPrompt(ContinuePrompt);
                if (connection.transform.position.x < door.position.x + 1.2f) return;
                finished = true;
                StorySceneDirector.Instance?.ClearPrompt(ContinuePrompt);
                StorySceneDirector.Instance?.ShowOpenEnding();
                return;
            }
            if (connection.CurrentTarget == recipient)
            {
                ready = recipient.HeldDuration >= transferSeconds;
                if (breath != null)
                {
                    breath.enabled = true;
                    JourneyVisuals.Ring(breath, recipient.transform.position + Vector3.up * 0.3f,
                        Mathf.Lerp(0.15f, 0.65f, Mathf.Clamp01(recipient.HeldDuration / transferSeconds)));
                }
                if (ready) StorySceneDirector.Instance?.ShowPrompt(ReleasePrompt);
                return;
            }
            if (breath != null) breath.enabled = false;
            if (!ready || connection.LastReleasedTarget != recipient || Time.time - connection.LastReleaseTime > 0.25f)
            {
                ready = false;
                StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
                return;
            }
            departing = true;
            StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
            connection.GetComponent<PlayerController2D>().ControlsEnabled = false;
            recipient.WalkAway(door.position);
            RecipientDeparted?.Invoke();
        }
    }
}
