using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FinalAgeTransition : MonoBehaviour
    {
        [SerializeField] private Vector3 playerScale = Vector3.one;
        [SerializeField] private Color playerColor = Color.white;
        [TextArea] [SerializeField] private string line;
        private bool triggered;

        public void Configure(Vector3 scale, Color color, string narration)
        {
            playerScale = scale;
            playerColor = color;
            line = narration;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered || !other.TryGetComponent<PlayerController2D>(out _)) return;
            triggered = true;
            other.transform.localScale = playerScale;
            var renderer = other.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = playerColor;
            StorySceneDirector.Instance?.Say(line, 4f);
        }
    }
}
