using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    [DefaultExecutionOrder(1000)]
    public sealed class PromptKeyIcon : MonoBehaviour
    {
        private Text prompt;
        private Image icon;
        private Vector2 restingPosition;
        public bool IsVisible => icon != null && icon.enabled && icon.gameObject.activeInHierarchy;

        public void Initialize(Text owner)
        {
            prompt = owner;
            restingPosition = owner.rectTransform.anchoredPosition;
            if (icon == null)
            {
                var palette = Resources.Load<JourneyArtPalette>("JourneyArtPalette");
                var sprite = palette != null ? palette.Find("ui_interact_e") : null;
                if (sprite == null) return;
                icon = new GameObject("Prompt E Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(owner.transform, false);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = StoryTypography.Paper;
                var rect = icon.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = Vector2.one * 0.5f;
                rect.anchoredPosition = new Vector2(-26f, 0f);
                rect.sizeDelta = new Vector2(32f, 32f);
                icon.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (icon == null || prompt == null) return;
            var show = prompt.isActiveAndEnabled && prompt.color.a > 0f && HasEKey(prompt.text);
            icon.enabled = show;
            prompt.rectTransform.anchoredPosition = restingPosition + (show ? Vector2.right * 44f : Vector2.zero);
        }

        private static bool HasEKey(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 'E') continue;
                if ((i == 0 || !IsWord(value[i - 1])) && (i + 1 == value.Length || !IsWord(value[i + 1]))) return true;
            }
            return false;
        }

        private static bool IsWord(char value) => value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '_';

        private void OnDisable()
        {
            if (icon != null) icon.enabled = false;
            if (prompt != null) prompt.rectTransform.anchoredPosition = restingPosition;
        }
    }
}
