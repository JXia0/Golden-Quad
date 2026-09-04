using UnityEngine;

namespace LetGo
{
    public sealed class ArtSlot : MonoBehaviour
    {
        [SerializeField] private string slotId;
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Animator targetAnimator;

        public string SlotId => slotId;
        public SpriteRenderer TargetRenderer => targetRenderer;
        public Animator TargetAnimator => targetAnimator;

        public void Configure(string id, SpriteRenderer renderer, Animator animator = null)
        {
            slotId = id;
            targetRenderer = renderer;
            targetAnimator = animator;
        }
    }
}
