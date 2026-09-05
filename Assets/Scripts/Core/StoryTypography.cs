using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    /// <summary>One cached Chinese font and a quiet, legible hierarchy across every story canvas.</summary>
    public static class StoryTypography
    {
        public static readonly Color Paper = new Color(0.95f, 0.93f, 0.87f);
        public static readonly Color Secondary = new Color(0.72f, 0.74f, 0.74f);
        public static readonly Color Accent = new Color(0.90f, 0.76f, 0.54f);
        private static Font bodyFont;

        // Kept across scene changes: labels must not each create and destroy their own atlas.
        public static Font BodyFont => bodyFont != null ? bodyFont : bodyFont =
            Font.CreateDynamicFontFromOSFont(new[]
            {
                "Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans CJK SC", "Arial"
            }, 24);

        public static void Apply(Text text, int size, TextAnchor alignment = TextAnchor.MiddleCenter, bool muted = false)
        {
            if (text == null) return;
            text.font = BodyFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.color = muted ? Secondary : Paper;
            text.alignment = alignment;
            text.lineSpacing = 1.3f;
            text.supportRichText = true;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        public static void ApplyTitle(Text text, int size = 36)
        {
            Apply(text, size, text != null ? text.alignment : TextAnchor.MiddleCenter);
            if (text != null) text.lineSpacing = 1.4f;
        }

        public static void ApplyPrompt(Text text)
        {
            if (text == null) return;
            // Authored scenes used 1920 while runtime overlays used 1280, shrinking identical sizes.
            var canvas = text.GetComponentInParent<Canvas>();
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            Apply(text, 22, TextAnchor.MiddleLeft);
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(48f, 32f);
            rect.sizeDelta = new Vector2(520f, 112f);
            var shadow = text.GetComponent<Shadow>();
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.015f, 0.019f, 0.024f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1.5f);
            shadow.useGraphicAlpha = true;
            var keyIcon = text.GetComponent<PromptKeyIcon>();
            if (keyIcon == null) keyIcon = text.gameObject.AddComponent<PromptKeyIcon>();
            keyIcon.Initialize(text);
        }
    }
}
