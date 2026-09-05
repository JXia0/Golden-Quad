using UnityEngine;
using UnityEngine.SceneManagement;

namespace LetGo
{
    /// <summary>Leave actual walking space between remembered rooms for the foreground curtains.</summary>
    public sealed class FinalPassageLayout : MonoBehaviour
    {
        public static readonly Vector2 WorldLimits = new(-11f, 92f);
        public static readonly float[] Centers = { 12f, 40f, 68f };

        public static void EnsureInstalled(Scene scene)
        {
            if (scene.name != "06_FinalWalk") return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<FinalPassageLayout>() != null) return;
            var marker = new GameObject("Cinematic Passage Layout", typeof(FinalPassageLayout));
            SceneManager.MoveGameObjectToScene(marker, scene);
            Shift(14f, "Stage Memory", "Memory Spotlight", "Remembered Steady Route", "Remembered Forward Route");
            Shift(28f, "Research Memory", "Remembered Question", "Remembered Photo Evidence",
                "Remembered Data Evidence", "Remembered Conclusion");
            Shift(42f, "Unknown Wall", "Young Presenter", "Unknown Door");
            MoveBoundary("Teen Memory", Centers[0]);
            MoveBoundary("Adult Memory", Centers[1]);
            var camera = Object.FindAnyObjectByType<CameraFollow2D>();
            camera?.SetHorizontalBounds(WorldLimits.x, WorldLimits.y);
            var ground = GameObject.Find("Ground")?.GetComponent<Collider2D>();
            if (ground != null && ground.bounds.max.x < WorldLimits.y)
            {
                var left = ground.bounds.min.x;
                var width = WorldLimits.y - left;
                var scale = ground.transform.localScale;
                scale.x *= width / ground.bounds.size.x;
                ground.transform.localScale = scale;
                var position = ground.transform.position;
                position.x = left + width * 0.5f;
                ground.transform.position = position;
            }
        }

        private static void Shift(float distance, params string[] names)
        {
            foreach (var name in names)
            {
                var root = GameObject.Find(name);
                if (root != null) root.transform.position += Vector3.right * distance;
            }
        }

        private static void MoveBoundary(string name, float x)
        {
            var root = GameObject.Find(name);
            if (root == null) return;
            var position = root.transform.position;
            position.x = x;
            root.transform.position = position;
        }
    }
}
