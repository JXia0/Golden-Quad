using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    // Functional, replaceable UI. Its canvas stays below the existing scene fade.
    public sealed class JourneyOverlay : MonoBehaviour
    {
        public static JourneyOverlay Create(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            var ui = go.AddComponent<JourneyOverlay>();
            return ui;
        }

        public RectTransform Rect(string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public Image Picture(string name, Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            var image = Rect(name, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public Text Label(string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = Rect(name, position, size).gameObject.AddComponent<Text>();
            StoryTypography.Apply(text, fontSize, alignment);
            return text;
        }

        public Vector2 ScreenPoint(Vector3 world)
        {
            var point = Camera.main.WorldToViewportPoint(world);
            return new Vector2((point.x - 0.5f) * 1280f, (point.y - 0.5f) * 720f);
        }

    }
}
