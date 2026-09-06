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
        private static Font narrationFont;
        private static Font promptFont;

        // Kept across scene changes: labels must not each create and destroy their own atlas.
        public static Font BodyFont => bodyFont != null ? bodyFont : bodyFont =
            Font.CreateDynamicFontFromOSFont(new[]
            {
                "Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans CJK SC", "Arial"
            }, 24);

        // The supplied keycap is hand drawn. Use the closest widely available Chinese face for
        // short action copy, while body text keeps the calmer sans-serif used for longer reading.
        public static Font PromptFont => promptFont != null ? promptFont : promptFont =
            Font.CreateDynamicFontFromOSFont(new[]
            {
                "KaiTi", "STKaiti", "FangSong", "Microsoft YaHei UI", "Microsoft YaHei"
            }, 26);

        // A restrained Song/Ming face gives memory narration the tone of a printed page while
        // keeping full simplified-Chinese coverage. The fallbacks preserve that character on
        // Windows machines that do not have the bundled workstation's Noto family installed.
        public static Font NarrationFont => narrationFont != null ? narrationFont : narrationFont =
            Font.CreateDynamicFontFromOSFont(new[]
            {
                "Noto Serif SC", "Source Han Serif SC", "SimSun", "NSimSun", "FangSong",
                "Microsoft YaHei UI"
            }, 26);

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
            InlineKeyIcon.Ensure(text);
        }

        public static void ApplyTitle(Text text, int size = 36)
        {
            Apply(text, size, text != null ? text.alignment : TextAnchor.MiddleCenter);
            if (text != null) text.lineSpacing = 1.4f;
        }

        public static void ApplyNarration(Text text)
        {
            if (text == null) return;
            Apply(text, 24, TextAnchor.MiddleCenter);
            text.font = NarrationFont != null ? NarrationFont : BodyFont;
            text.color = new Color(0.94f, 0.89f, 0.79f, 1f);
            text.lineSpacing = 1.42f;

            var outline = text.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            var shadow = text.GetComponent<Shadow>();
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.022f, 0.018f, 0.72f);
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;
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
            Apply(text, 24, TextAnchor.MiddleLeft);
            text.font = PromptFont;
            text.color = new Color(0.96f, 0.92f, 0.84f);
            text.lineSpacing = 1.18f;
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
            var outline = text.GetComponent<Outline>();
            if (outline == null) outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.035f, 0.045f, 0.055f, 0.88f);
            outline.effectDistance = new Vector2(0.8f, -0.8f);
            outline.useGraphicAlpha = true;
            var keyIcon = text.GetComponent<PromptKeyIcon>();
            if (keyIcon == null) keyIcon = text.gameObject.AddComponent<PromptKeyIcon>();
            keyIcon.Initialize(text);
        }
    }
}
