using UnityEngine;

namespace LetGo
{
    public sealed class FinalReleaseCue : MonoBehaviour
    {
        [SerializeField] private HandConnection connection;
        [SerializeField] private float radius = 2.2f;
        [SerializeField] private int prerequisiteCount = 2;
        private bool chargedHere;
        private bool completed;
        private const string CuePrompt = "按住 E 吸气，光充满后松开";
        private const string PrerequisitePrompt = "先在前面的站位完成一次呼吸，并选择一条路线";

        public void Configure(HandConnection handConnection, float interactionRadius, int prerequisite)
        {
            connection = handConnection;
            radius = interactionRadius;
            prerequisiteCount = prerequisite;
        }

        private void Update()
        {
            if (completed || connection == null) return;
            var near = Vector2.Distance(connection.transform.position, transform.position) <= radius;
            if (!near)
            {
                chargedHere = false;
                StorySceneDirector.Instance?.ClearPrompt(CuePrompt);
                StorySceneDirector.Instance?.ClearPrompt(PrerequisitePrompt);
                return;
            }
            var director = StorySceneDirector.Instance;
            if (director == null || director.CompletedObjectives < prerequisiteCount)
            {
                director?.ShowPrompt(PrerequisitePrompt);
                return;
            }
            director.ClearPrompt(PrerequisitePrompt);
            director.ShowPrompt(CuePrompt);
            if (connection.IsSelfAnchoring && connection.SelfChargeNormalized >= 0.98f) chargedHere = true;
            if (!chargedHere || Time.time - connection.LastFullSelfReleaseTime > 0.3f) return;
            completed = true;
            director.ClearPrompt(CuePrompt);
            director.CompleteObjective("我准备好了。");
            SceneAudio.Instance?.PlayApplause();
        }
    }
}
