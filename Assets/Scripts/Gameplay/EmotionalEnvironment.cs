using UnityEngine;

namespace LetGo
{
    public sealed class EmotionalEnvironment : MonoBehaviour
    {
        [SerializeField] private HandConnection connection;
        [SerializeField] private Transform unstableRoot;
        [SerializeField] private SpriteRenderer[] fearObjects;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Color stableColor;
        [SerializeField] private Color unstableColor;
        [SerializeField] private int activeAfterObjectives;
        [SerializeField] private int deactivateAtObjectives = -1;
        private Vector3 rootOrigin;

        public void Configure(HandConnection handConnection, Transform root, SpriteRenderer[] fears, Camera camera,
            Color stable, Color unstable, int activateAfter = 0, int deactivateAt = -1)
        {
            connection = handConnection;
            unstableRoot = root;
            fearObjects = fears;
            targetCamera = camera;
            stableColor = stable;
            unstableColor = unstable;
            activeAfterObjectives = activateAfter;
            deactivateAtObjectives = deactivateAt;
            if (unstableRoot != null) rootOrigin = unstableRoot.localPosition;
        }

        private void Start()
        {
            if (unstableRoot != null) rootOrigin = unstableRoot.localPosition;
        }

        private void Update()
        {
            if (connection == null) return;
            var objectiveCount = StorySceneDirector.Instance == null ? 0 : StorySceneDirector.Instance.CompletedObjectives;
            var active = objectiveCount >= activeAfterObjectives &&
                         (deactivateAtObjectives < 0 || objectiveCount < deactivateAtObjectives);
            var instability = active ? 1f - connection.Stability01 : 0f;
            SceneAudio.Instance?.SetTension(instability);
            if (unstableRoot != null)
                unstableRoot.localPosition = rootOrigin + new Vector3(
                    Mathf.Sin(Time.time * 5.1f) * 0.06f * instability,
                    Mathf.Sin(Time.time * 6.7f) * 0.035f * instability, 0f);
            if (targetCamera != null) targetCamera.backgroundColor = Color.Lerp(stableColor, unstableColor, instability);
            if (fearObjects == null) return;
            foreach (var fear in fearObjects)
            {
                if (fear == null) continue;
                var color = fear.color;
                color.a = Mathf.Lerp(0.08f, 0.7f, instability);
                fear.color = color;
            }
        }
    }
}
