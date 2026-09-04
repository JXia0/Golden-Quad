using UnityEngine;

namespace LetGo
{
    public sealed class CarryInventory : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer carriedVisual;
        public string ItemId { get; private set; }
        public bool HasItem => !string.IsNullOrEmpty(ItemId);

        public void Configure(SpriteRenderer visual)
        {
            carriedVisual = visual;
            if (carriedVisual != null) carriedVisual.enabled = false;
        }

        public bool TryPickUp(string itemId, Sprite sprite, Color color)
        {
            if (HasItem) return false;
            ItemId = itemId;
            if (carriedVisual != null)
            {
                carriedVisual.sprite = sprite;
                carriedVisual.color = color;
                carriedVisual.enabled = true;
            }
            return true;
        }

        public bool TryDeliver(string itemId)
        {
            if (ItemId != itemId) return false;
            ItemId = string.Empty;
            if (carriedVisual != null) carriedVisual.enabled = false;
            return true;
        }
    }
}
