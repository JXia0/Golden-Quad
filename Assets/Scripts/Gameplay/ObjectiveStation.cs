using UnityEngine;

namespace LetGo
{
    public sealed class ObjectiveStation : ProximityInteractable
    {
        [TextArea] [SerializeField] private string completionLine;
        [SerializeField] private Color completedColor = new(1f, 0.75f, 0.25f, 1f);
        [SerializeField] private int prerequisiteCount;
        [SerializeField] private string choiceCategory;
        [SerializeField] private string choiceValue;
        [SerializeField] private bool recordOnlyIfEmpty;

        public void Configure(string promptText, string line, int prerequisite)
        {
            prompt = promptText;
            completionLine = line;
            prerequisiteCount = prerequisite;
        }

        public void RecordChoice(string category, string value, bool onlyIfEmpty = true)
        {
            choiceCategory = category;
            choiceValue = value;
            recordOnlyIfEmpty = onlyIfEmpty;
        }

        protected override void Interact()
        {
            var director = StorySceneDirector.Instance;
            if (director == null || director.CompletedObjectives < prerequisiteCount)
            {
                director?.Say("还缺少前面的那一步。", 1.5f);
                return;
            }
            completed = true;
            JourneyChoices.Record(choiceCategory, choiceValue, recordOnlyIfEmpty);
            director.ClearPrompt(prompt);
            director.CompleteObjective(completionLine);
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = completedColor;
            player?.GetComponent<CourageSystem>()?.RestoreFull();
        }
    }
}
