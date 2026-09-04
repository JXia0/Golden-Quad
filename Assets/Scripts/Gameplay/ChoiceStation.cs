using UnityEngine;

namespace LetGo
{
    public sealed class ChoiceStation : ProximityInteractable
    {
        [SerializeField] private string groupId;
        [SerializeField] private string category;
        [SerializeField] private string choiceValue;
        [TextArea] [SerializeField] private string completionLine;
        [SerializeField] private int prerequisiteCount;

        public void Configure(string group, string categoryName, string value, string promptText, string line, int prerequisite)
        {
            groupId = group;
            category = categoryName;
            choiceValue = value;
            prompt = promptText;
            completionLine = line;
            prerequisiteCount = prerequisite;
        }

        protected override void Interact()
        {
            var director = StorySceneDirector.Instance;
            if (director == null || director.CompletedObjectives < prerequisiteCount)
            {
                director?.Say("先走进舞台中央。", 1.5f);
                return;
            }
            JourneyChoices.Record(category, choiceValue, false);
            director.CompleteObjective(completionLine);
            director.ClearPrompt(prompt);
            foreach (var station in FindObjectsByType<ChoiceStation>())
                if (station.groupId == groupId) station.Lock(station == this);
        }

        private void Lock(bool selected)
        {
            completed = true;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = selected
                ? new Color(1f, 0.72f, 0.25f)
                : new Color(0.2f, 0.2f, 0.25f, 0.35f);
        }
    }
}
