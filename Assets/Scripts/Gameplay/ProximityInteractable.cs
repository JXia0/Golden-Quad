using UnityEngine;

namespace LetGo
{
    public abstract class ProximityInteractable : MonoBehaviour
    {
        [SerializeField] protected string prompt = "按 E 互动";
        [SerializeField] protected float radius = 1.5f;
        protected Transform player;
        protected bool completed;

        protected virtual void Update()
        {
            if (completed) return;
            if (player == null)
            {
                var controller = FindFirstObjectByType<PlayerController2D>();
                if (controller != null) player = controller.transform;
                return;
            }
            var near = Vector2.Distance(player.position, transform.position) <= radius;
            if (near)
            {
                StorySceneDirector.Instance?.ShowPrompt(prompt);
                if (GameInput.InteractPressed) Interact();
            }
            else StorySceneDirector.Instance?.ClearPrompt(prompt);
        }

        protected abstract void Interact();

        protected virtual void OnDisable() => StorySceneDirector.Instance?.ClearPrompt(prompt);
    }
}
