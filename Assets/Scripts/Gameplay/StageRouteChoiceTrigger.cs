using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class StageRouteChoiceTrigger : MonoBehaviour
    {
        [SerializeField] private string choiceValue;
        [TextArea] [SerializeField] private string completionLine;

        public void Configure(string value, string line)
        {
            choiceValue = value;
            completionLine = line;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<PlayerController2D>(out _)) return;
            var director = StorySceneDirector.Instance;
            if (director == null || director.CompletedObjectives != 1) return;
            JourneyChoices.Record("stage", choiceValue, false);
            director.CompleteObjective(completionLine);
        }
    }
}
