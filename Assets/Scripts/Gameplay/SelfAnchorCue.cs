using UnityEngine;

namespace LetGo
{
    public sealed class SelfAnchorCue : MonoBehaviour
    {
        [SerializeField] private HandConnection connection;
        [SerializeField] private float radius = 2f;
        [SerializeField] private int prerequisiteCount;
        [TextArea] [SerializeField] private string completionLine;
        private bool completed;
        private const string CuePrompt = "按住 E，把手放在胸口稳定呼吸";

        public void Configure(HandConnection handConnection, float interactionRadius, int prerequisite, string line)
        {
            connection = handConnection;
            radius = interactionRadius;
            prerequisiteCount = prerequisite;
            completionLine = line;
        }

        private void Update()
        {
            if (completed || connection == null) return;
            var near = Vector2.Distance(connection.transform.position, transform.position) <= radius;
            if (!near)
            {
                StorySceneDirector.Instance?.ClearPrompt(CuePrompt);
                return;
            }
            var director = StorySceneDirector.Instance;
            if (director != null && director.CompletedObjectives < prerequisiteCount) return;
            director?.ShowPrompt(CuePrompt);
            if (connection.IsSelfAnchoring && connection.SelfChargeNormalized >= 0.98f)
            {
                completed = true;
                director?.ClearPrompt(CuePrompt);
                director?.CompleteObjective(completionLine);
            }
        }
    }
}
