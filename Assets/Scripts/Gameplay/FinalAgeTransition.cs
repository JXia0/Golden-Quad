using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FinalAgeTransition : MonoBehaviour
    {
        [SerializeField] private Vector3 playerScale = Vector3.one;
        [SerializeField] private Color playerColor = Color.white;
        [SerializeField] private string artSlotId;
        [SerializeField] private Sprite transitionSprite;
        [SerializeField] private RuntimeAnimatorController transitionController;
        [TextArea] [SerializeField] private string line;
        private bool triggered;

        public string ArtSlotId => artSlotId;

        public void Configure(Vector3 scale, Color color, string narration, string slotId)
        {
            playerScale = scale;
            playerColor = color;
            line = narration;
            artSlotId = slotId;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        public void SetTransitionSprite(Sprite value) => transitionSprite = value;
        public void SetTransitionController(RuntimeAnimatorController value) => transitionController = value;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered || !other.TryGetComponent<PlayerController2D>(out var player)) return;
            triggered = true;
            var renderer = player.CharacterRenderer;
            if (renderer != null)
            {
                renderer.color = playerColor;
                if (transitionSprite != null)
                {
                    renderer.sprite = transitionSprite;
                    player.FitCharacterVisualToCollider();
                }
            }
            var animator = other.GetComponent<Animator>();
            if (animator != null && transitionController != null) animator.runtimeAnimatorController = transitionController;
            StorySceneDirector.Instance?.Say(line, 4f);
        }
    }
}
