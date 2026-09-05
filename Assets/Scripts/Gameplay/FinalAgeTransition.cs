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
        private PlayerController2D player;
        private Animator animator;
        private float nextBoundary = float.PositiveInfinity;
        private bool firstRegion = true;
        private bool wasCurrent;

        public string ArtSlotId => artSlotId;
        public RuntimeAnimatorController TransitionController => transitionController;
        public bool IsCurrentAge { get; private set; }
        public int Applications { get; private set; }

        private void Awake() => FinalPassageLayout.EnsureInstalled(gameObject.scene);

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

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController2D>();
            animator = player == null ? null : player.GetComponent<Animator>();
            foreach (var region in FindObjectsByType<FinalAgeTransition>())
            {
                if (region.gameObject.scene != gameObject.scene || region == this) continue;
                var x = region.transform.position.x;
                if (x < transform.position.x) firstRegion = false;
                else if (x > transform.position.x) nextBoundary = Mathf.Min(nextBoundary, x);
            }
        }

        private void Update()
        {
            if (player == null) return;
            var x = player.transform.position.x;
            IsCurrentAge = (firstRegion || x >= transform.position.x) && x < nextBoundary;
            if (!IsCurrentAge) { wasCurrent = false; return; }
            if (wasCurrent) return;
            wasCurrent = true;
            Applications++;
            // Age belongs to a place in the remembered journey, so walking back restores it.
            // The presentation's full-black passage covers this midpoint in both directions.
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
            if (animator != null && transitionController != null) animator.runtimeAnimatorController = transitionController;
            StorySceneDirector.Instance?.Say(line, 4f);
        }
    }
}
