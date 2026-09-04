using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ComfortZone : MonoBehaviour
    {
        private bool introduced;

        private void OnTriggerStay2D(Collider2D other)
        {
            var courage = other.GetComponent<CourageSystem>();
            if (courage == null || !courage.Draining) return;
            courage.RestoreFull();
            if (!introduced)
            {
                introduced = true;
                StorySceneDirector.Instance?.Say("我仍然可以回来。\n准备好以后，再向前走。", 3f);
            }
        }
    }
}
