using UnityEngine;

namespace LetGo
{
    public sealed class ReleaseGate : MonoBehaviour
    {
        [SerializeField] private HandConnection connection;
        [SerializeField] private string targetId;
        [SerializeField] private float boundaryX;
        [TextArea] [SerializeField] private string completionLine;
        private bool reachedDoorTogether;
        private bool completed;
        private const string ReleasePrompt = "松开 E，自己跨过这扇门";
        private const string BringHandPrompt = "可以自己进去，也可以回头牵住父母的手";

        public void Configure(HandConnection handConnection, string requiredTargetId, float x, string line)
        {
            connection = handConnection;
            targetId = requiredTargetId;
            boundaryX = x;
            completionLine = line;
            if (connection != null)
                transform.position = new Vector3(boundaryX, connection.transform.position.y, 0f);
        }

        private void Update()
        {
            if (completed || connection == null) return;
            if (connection.CurrentTarget != null && connection.CurrentTarget.TargetId == targetId)
            {
                if (connection.transform.position.x > boundaryX - 1.8f)
                {
                    reachedDoorTogether = true;
                    StorySceneDirector.Instance?.ClearPrompt(BringHandPrompt);
                    StorySceneDirector.Instance?.ShowPrompt(ReleasePrompt);
                }
            }
            else
            {
                StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
                if (!reachedDoorTogether && connection.transform.position.x > boundaryX - 1.8f)
                    StorySceneDirector.Instance?.ShowPrompt(BringHandPrompt);
                else StorySceneDirector.Instance?.ClearPrompt(BringHandPrompt);
            }

            // Accept independent entry too: parental help is an offer, not a key for this door.
            if (connection.transform.position.x < boundaryX || connection.CurrentTarget != null) return;
            completed = true;
            StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
            StorySceneDirector.Instance?.ClearPrompt(BringHandPrompt);
            StorySceneDirector.Instance?.SetCheckpoint(transform);
            StorySceneDirector.Instance?.CompleteObjective(completionLine);
        }
    }
}
