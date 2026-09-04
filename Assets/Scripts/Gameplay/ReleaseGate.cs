using UnityEngine;

namespace LetGo
{
    public sealed class ReleaseGate : MonoBehaviour
    {
        [SerializeField] private HandConnection connection;
        [SerializeField] private string targetId;
        [SerializeField] private float boundaryX;
        [TextArea] [SerializeField] private string completionLine;
        private bool sawConnection;
        private bool completed;
        private const string ReleasePrompt = "松开 E，自己跨过这扇门";

        public void Configure(HandConnection handConnection, string requiredTargetId, float x, string line)
        {
            connection = handConnection;
            targetId = requiredTargetId;
            boundaryX = x;
            completionLine = line;
        }

        private void Update()
        {
            if (completed || connection == null) return;
            if (connection.CurrentTarget != null && connection.CurrentTarget.TargetId == targetId)
            {
                sawConnection = true;
                if (connection.transform.position.x > boundaryX - 1.8f)
                    StorySceneDirector.Instance?.ShowPrompt(ReleasePrompt);
            }
            else StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);

            if (!sawConnection || connection.transform.position.x < boundaryX || connection.CurrentTarget != null) return;
            completed = true;
            StorySceneDirector.Instance?.ClearPrompt(ReleasePrompt);
            StorySceneDirector.Instance?.CompleteObjective(completionLine);
        }
    }
}
