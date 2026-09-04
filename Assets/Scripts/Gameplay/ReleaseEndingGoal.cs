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
        private const string ReleasePrompt = "松开 E，让他自己走进去";

        public void Configure(HandConnection handConnection, HoldTarget target, float seconds)
        {
            connection = handConnection;
            recipient = target;
            transferSeconds = seconds;
        }

        private void Update()
        {
            if (finished || connection == null || recipient == null) return;
            if (connection.CurrentTarget == recipient)
            {
                if (recipient.HeldDuration >= transferSeconds) ready = true;
                if (ready) StorySceneDirector.Instance?.ShowPrompt(ReleasePrompt);
                return;
            }
            if (!ready || Vector2.Distance(connection.transform.position, recipient.transform.position) > 3f) return;
            finished = true;
            StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
            StorySceneDirector.Instance?.ShowOpenEnding();
        }
    }
}
