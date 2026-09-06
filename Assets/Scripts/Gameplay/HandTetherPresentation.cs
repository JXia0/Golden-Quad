using UnityEngine;

namespace LetGo
{
    /// <summary>Presentation-only hand thread; HandConnection remains the gameplay authority.</summary>
    [DefaultExecutionOrder(150)]
    public sealed class HandTetherPresentation : MonoBehaviour
    {
        private const int PointCount = 16;
        private HandConnection connection;
        private LineRenderer line;
        private SpriteRenderer playerRenderer;
        private HoldTarget cachedTarget;
        private SpriteRenderer targetRenderer;

        public bool IsVisible => line != null && line.enabled;
        public Vector3 StartPoint { get; private set; }
        public Vector3 EndPoint { get; private set; }

        public void Configure(HandConnection source, LineRenderer targetLine, SpriteRenderer playerArt)
        {
            connection = source;
            line = targetLine;
            playerRenderer = playerArt;
            if (line == null) return;
            line.useWorldSpace = true;
            line.positionCount = PointCount;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.sortingOrder = 14;
            line.enabled = false;
        }

        private void LateUpdate()
        {
            if (connection == null || line == null || playerRenderer == null)
            {
                if (line != null) line.enabled = false;
                return;
            }

            var target = connection.CurrentTarget;
            // The parent artwork already shows the joined hands; it needs no extra thread.
            if (target == null || target.Mode == HoldTargetMode.Carryable ||
                target.TargetId == "parent" || target.TargetId == "stage-parent")
            {
                line.enabled = false;
                cachedTarget = null;
                targetRenderer = null;
                return;
            }
            if (target != cachedTarget)
            {
                cachedTarget = target;
                targetRenderer = FindVisibleRenderer(target.transform);
            }
            if (targetRenderer == null || !targetRenderer.enabled)
            {
                line.enabled = false;
                return;
            }

            var direction = Mathf.Sign(targetRenderer.bounds.center.x - playerRenderer.bounds.center.x);
            if (Mathf.Approximately(direction, 0f)) direction = 1f;
            StartPoint = HandPoint(playerRenderer, direction);
            EndPoint = HandPoint(targetRenderer, -direction);
            var distance = Vector2.Distance(StartPoint, EndPoint);
            var maximum = Mathf.Max(0.5f, target.MaxDistance);
            var tension = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(maximum * 0.42f, maximum * 0.94f, distance));
            var sag = Mathf.Lerp(Mathf.Min(0.24f, distance * 0.12f), 0.025f, tension);

            line.enabled = true;
            line.positionCount = PointCount;
            for (var i = 0; i < PointCount; i++)
            {
                var t = i / (float)(PointCount - 1);
                var eased = t * t * (3f - 2f * t);
                var point = Vector3.Lerp(StartPoint, EndPoint, eased);
                point.y -= Mathf.Sin(t * Mathf.PI) * sag;
                line.SetPosition(i, point);
            }

            line.startWidth = Mathf.Lerp(0.028f, 0.016f, tension);
            line.endWidth = Mathf.Lerp(0.022f, 0.013f, tension);
            var warm = new Color(1f, 0.73f, 0.42f, Mathf.Lerp(0.62f, 0.82f, tension));
            var pale = new Color(1f, 0.9f, 0.7f, Mathf.Lerp(0.48f, 0.68f, tension));
            line.startColor = warm;
            line.endColor = pale;
        }

        private static Vector3 HandPoint(SpriteRenderer renderer, float side)
        {
            var bounds = renderer.bounds;
            return new Vector3(
                bounds.center.x + side * bounds.extents.x * 0.42f,
                bounds.min.y + bounds.size.y * 0.55f,
                renderer.transform.position.z);
        }

        private static SpriteRenderer FindVisibleRenderer(Transform root)
        {
            SpriteRenderer best = null;
            var bestArea = 0f;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.enabled || renderer.sprite == null) continue;
                var area = renderer.bounds.size.x * renderer.bounds.size.y;
                if (area <= bestArea) continue;
                best = renderer;
                bestArea = area;
            }
            return best;
        }
    }
}
