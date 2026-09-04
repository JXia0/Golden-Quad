using UnityEngine;

namespace LetGo
{
    public sealed class CarryItem : ProximityInteractable
    {
        [SerializeField] private string itemId;
        [SerializeField] private string pickedUpLine;

        public void Configure(string id, string promptText, string line)
        {
            itemId = id;
            prompt = promptText;
            pickedUpLine = line;
        }

        protected override void Interact()
        {
            var inventory = player.GetComponent<CarryInventory>();
            var renderer = GetComponent<SpriteRenderer>();
            if (inventory == null) return;
            if (!inventory.TryPickUp(itemId, renderer.sprite, renderer.color))
            {
                StorySceneDirector.Instance?.Say("手里已经拿着东西了。", 1.5f);
                return;
            }
            completed = true;
            StorySceneDirector.Instance?.ClearPrompt(prompt);
            StorySceneDirector.Instance?.Say(pickedUpLine, 2f);
            renderer.enabled = false;
            GetComponent<Collider2D>().enabled = false;
        }
    }
}
