using System.Collections.Generic;
using UnityEngine;

namespace LetGo
{
    public sealed class JourneyVisuals : MonoBehaviour
    {
        public static readonly Color Warm = new(1f, 0.69f, 0.34f);
        public static readonly Color Cool = new(0.5f, 0.74f, 0.92f);
        private Material material;
        private Sprite square;
        private HandConnection hand;
        private LineRenderer breath;
        private readonly List<(Transform target, LineRenderer ring)> targets = new();
        private readonly List<GameObject> guideObjects = new();
        private bool guidesVisible = true;

        public void Initialize(StorySceneDirector director)
        {
            hand = director.Player != null ? director.Player.GetComponent<HandConnection>() : null;
            material = new Material(Shader.Find("Sprites/Default"));
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
            breath = Line("Breath in the chest", Warm, 0.045f);
        }

        public LineRenderer Line(string name, Color color, float width = 0.055f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            line.sortingOrder = 15;
            line.numCapVertices = 3;
            line.positionCount = 0;
            guideObjects.Add(go);
            go.SetActive(guidesVisible);
            return line;
        }

        public SpriteRenderer Prop(string name, Vector3 position, Vector2 size, Color color, string slot = "")
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = 8;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            if (string.IsNullOrEmpty(slot))
            {
                guideObjects.Add(go);
                go.SetActive(guidesVisible);
            }
            if (!string.IsNullOrEmpty(slot))
            {
                go.AddComponent<ArtSlot>().Configure(slot, renderer);
                // Runtime props use references authored on JourneyArtPalette by the editor importer.
                var palette = Resources.Load<JourneyArtPalette>("JourneyArtPalette");
                var sprite = palette != null ? palette.Find(slot) : null;
                if (sprite != null)
                {
                    renderer.sprite = sprite;
                    renderer.color = Color.white;
                    var scale = Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
                    go.transform.localScale = Vector3.one * scale;
                }
            }
            return renderer;
        }

        public void Track(Transform target, Color color)
        {
            targets.Add((target, Line("Reach - " + target.name, color, 0.035f)));
        }

        public void SetGuidesVisible(bool visible)
        {
            guidesVisible = visible;
            foreach (var guide in guideObjects) if (guide != null) guide.SetActive(visible);
        }

        public static void Ring(LineRenderer line, Vector3 center, float radius, float amount = 1f)
        {
            line.positionCount = 33;
            for (var i = 0; i < 33; i++)
            {
                var a = i / 32f * Mathf.PI * 2f * Mathf.Clamp01(amount);
                line.SetPosition(i, center + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0) * radius);
            }
        }

        private void LateUpdate()
        {
            if (hand == null) return;
            breath.enabled = hand.IsSelfAnchoring || hand.Stability01 > 0.05f;
            Ring(breath, hand.transform.position + Vector3.up * 0.3f,
                Mathf.Lerp(0.18f, 0.62f, hand.IsSelfAnchoring ? hand.SelfChargeNormalized : hand.Stability01),
                hand.IsSelfAnchoring ? Mathf.Max(0.03f, hand.SelfChargeNormalized) : 1f);
            foreach (var entry in targets)
            {
                if (entry.target == null) { entry.ring.enabled = false; continue; }
                var target = entry.target.GetComponent<HoldTarget>();
                entry.ring.enabled = target == null || (!target.IsPlaced && target.CanHold);
                var near = Vector2.Distance(hand.transform.position, entry.target.position) < 2.1f;
                Ring(entry.ring, entry.target.position, (near ? 0.52f : 0.35f) + Mathf.Sin(Time.time * 2f) * 0.025f);
            }
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (square != null) Destroy(square);
        }
    }
}
