using UnityEngine;

namespace LetGo
{
    public sealed class DeliveryStation : ProximityInteractable
    {
        [SerializeField] private string requiredItemId;
        [TextArea] [SerializeField] private string completionLine;
        [TextArea] [SerializeField] private string missingItemLine;
        [SerializeField] private string choiceCategory;
        [SerializeField] private string choiceValue;

        public void Configure(string itemId, string promptText, string completedLine, string missingLine,
            string category = "", string value = "")
        {
            requiredItemId = itemId;
            prompt = promptText;
            completionLine = completedLine;
            missingItemLine = missingLine;
            choiceCategory = category;
            choiceValue = value;
        }

        protected override void Interact()
        {
            var inventory = player.GetComponent<CarryInventory>();
            if (inventory == null || !inventory.TryDeliver(requiredItemId))
            {
                StorySceneDirector.Instance?.Say(missingItemLine, 1.8f);
                return;
            }
            completed = true;
            JourneyChoices.Record(choiceCategory, choiceValue, true);
            StorySceneDirector.Instance?.ClearPrompt(prompt);
            StorySceneDirector.Instance?.CompleteObjective(completionLine);
            player.GetComponent<CourageSystem>()?.RestoreFull();
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(1f, 0.72f, 0.25f);
        }
    }
}
