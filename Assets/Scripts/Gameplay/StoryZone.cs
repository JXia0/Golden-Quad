using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class StoryZone : MonoBehaviour
    {
        [TextArea] [SerializeField] private string line;
        [SerializeField] private bool beginCourageDrain;
        [SerializeField] private bool stopCourageDrain;
        [SerializeField] private bool setCheckpoint;
        private bool triggered;

        public void Configure(string narration, bool beginDrain = false, bool stopDrain = false, bool checkpoint = false)
        {
            line = narration;
            beginCourageDrain = beginDrain;
            stopCourageDrain = stopDrain;
            setCheckpoint = checkpoint;
            var collider = GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered || !other.TryGetComponent<PlayerController2D>(out _)) return;
            triggered = true;
            if (!string.IsNullOrWhiteSpace(line)) StorySceneDirector.Instance?.Say(line, 4f);
            var courage = other.GetComponent<CourageSystem>();
            if (courage != null)
            {
                if (beginCourageDrain) courage.Draining = true;
                if (stopCourageDrain) courage.Draining = false;
            }
            if (setCheckpoint) StorySceneDirector.Instance?.SetCheckpoint(transform);
        }
    }
}
