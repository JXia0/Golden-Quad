#if UNITY_EDITOR
using System.Collections;
using LetGo;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class JourneyPlaythroughDriver
{
    private IEnumerator ReviewInlineKeys()
    {
        phase = "inline E and F pictures in actual UI meshes";
        var ui = JourneyOverlay.Create("Inline key QA");
        ui.GetComponent<Canvas>().sortingOrder = 32740;
        ui.Picture("Backdrop", Vector2.zero, new Vector2(1280f, 720f), null, new Color(0.045f, 0.055f, 0.065f));
        var label = ui.Label("Action text", new Vector2(0f, 150f), new Vector2(980f, 210f), 28, TextAnchor.UpperLeft);
        var inheritedFont = label.font;
        var icon = label.GetComponent<InlineKeyIcon>();
        var palette = Resources.Load<JourneyArtPalette>("JourneyArtPalette");
        var hasF = palette != null && palette.Find("ui_interact_f") != null;
        var expected = hasF ? 3 : 2;
        var source = "按住 E，再松开 E\nF · 开始　Enter / English / E-mail · 保留原样";
        var priorTimeScale = Time.timeScale;
        try
        {
            if (!Check(icon != null && palette != null && palette.Find("ui_interact_e") != null,
                "shared typography installs inline replacement and resolves the delivered E picture")) yield break;
            label.text = source;
            yield return Keys(0.25f);
            Canvas.ForceUpdateCanvases();
            yield return null;
            if (!Check(label.text == source && icon.SourceTextAtLastBuild == source && icon.RenderedIconCount == expected &&
                icon.ReplacedQuadCount == expected && icon.RemovedMarkerVertexCount == expected * 4 &&
                icon.TextVertexCountBeforeEffects == icon.GeneratedVertexCount - expected * 4,
                "multiple key glyph quads are actually removed and replaced while Text.text stays unchanged")) yield break;
            if (!Check(InlineKeyIcon.CountStandaloneKeys(icon.DisplayText, 'E') == 0 &&
                icon.DisplayText.Contains("Enter / English / E-mail") &&
                (hasF || InlineKeyIcon.CountStandaloneKeys(icon.DisplayText, 'F') == 1),
                "Enter and English words remain visible; F remains text when no real F art exists")) yield break;
            if (!Check(!icon.GetIconRectLocal(0).Overlaps(icon.GetIconRectLocal(1)) &&
                icon.GetIconCenterLocal(1).x > icon.GetIconCenterLocal(0).x &&
                (!hasF || icon.GetIconCenterLocal(2).y < icon.GetIconCenterLocal(0).y - 15f),
                "each picture occupies its own position and explicit line breaks place later keys on the next line")) yield break;
            if (!Check(label.font == StoryTypography.PromptFont && label.fontSize == 28,
                "action labels adopt the delivered prompt typography while keeping their own size hierarchy")) yield break;
            yield return CaptureInline("ui-inline-keys-multiple-and-words");

            label.fontSize = 17;
            label.text = "A D 选纸　按住 E + A D 移动　F 试走";
            yield return Keys(0.25f);
            if (!Check(icon.RenderedIconCount == (hasF ? 2 : 1) && icon.GetIconRectLocal(0).width >= 30f &&
                (!hasF || !icon.GetIconRectLocal(0).Overlaps(icon.GetIconRectLocal(1))),
                "17-pixel operation text reserves at least 30 pixels for each complete key picture")) yield break;
            yield return CaptureInline("ui-inline-keys-small-controls-readable");
            label.fontSize = 22;
            label.rectTransform.sizeDelta = new Vector2(980f, 32f);
            label.text = "按住 E · 牵手　F · 开始";
            yield return Keys(0.25f);
            if (!Check(icon.RenderedIconCount == (hasF ? 2 : 1) && icon.ReplacedQuadCount == (hasF ? 2 : 1) &&
                icon.GetIconRectLocal(0).width >= 39f && icon.TextVertexCountBeforeEffects > 0 &&
                label.canvasRenderer.GetMesh() != null && label.canvasRenderer.GetMesh().vertexCount > 0,
                "a 22px instruction in a 32px-high label keeps its real text mesh and legible full-size key pictures")) yield break;
            yield return CaptureInline("ui-inline-keys-narrow-height-readable");
            label.fontSize = 28;
            label.rectTransform.sizeDelta = new Vector2(980f, 210f);

            phase = "inline key rich text and resized layout";
            source = "<b>E</b>nter 保留\n<size=40>按住 E</size>，然后\n<color=#B6D7E8><size=22>松开 E</size></color>";
            label.text = source;
            yield return Keys(0.25f);
            if (!Check(label.text == source && icon.RenderedIconCount == 2 && icon.RemovedMarkerVertexCount == 8 &&
                icon.DisplayText.Contains("<b>E</b>nter") && icon.GetIconRectLocal(0).width > icon.GetIconRectLocal(1).width * 1.3f &&
                icon.GetIconCenterLocal(0).y > icon.GetIconCenterLocal(1).y + 15f,
                "rich formatting across an English word does not invent a key; inline pictures follow actual nested sizes and lines")) yield break;
            label.rectTransform.sizeDelta = new Vector2(300f, 240f);
            label.alignment = TextAnchor.MiddleCenter;
            yield return Keys(0.25f);
            if (!Check(icon.RenderedIconCount == 2 && !icon.GetIconRectLocal(0).Overlaps(icon.GetIconRectLocal(1)) &&
                label.rectTransform.rect.Contains(icon.GetIconCenterLocal(0)) && label.rectTransform.rect.Contains(icon.GetIconCenterLocal(1)),
                "resizing and centered alignment keep key pictures within the reflowed label without overlap")) yield break;
            yield return CaptureInline("ui-inline-keys-rich-wrap");

            phase = "inline key text switch and pause";
            label.text = "Enter / English";
            yield return Keys(0.2f);
            if (!Check(icon.RenderedIconCount == 0 && icon.ReplacedQuadCount == 0 && label.font == inheritedFont,
                "switching to prose removes every old picture and restores the original font")) yield break;
            Time.timeScale = 0f;
            source = "暂停中\nE · 返回　F · 继续";
            label.text = source;
            for (var frame = 0; frame < 8; frame++) yield return null;
            if (!Check(icon.RenderedIconCount == (hasF ? 2 : 1) && label.text == source,
                "paused UI still lays out real key pictures without advancing game time")) yield break;
            yield return CaptureInline("ui-inline-keys-while-paused");
            // Same exact comparison used by StorySceneDirector.ClearPrompt(expected).
            if (label.text == source) label.text = "";
            for (var frame = 0; frame < 6; frame++) yield return null;
            if (!Check(label.text == "" && icon.RenderedIconCount == 0 && icon.ReplacedQuadCount == 0,
                "exact-string prompt clearing still succeeds and leaves no key picture behind")) yield break;
            Time.timeScale = priorTimeScale;

            phase = "inline key shadow component order";
            var before = ui.Rect("Outline before inline", new Vector2(0f, 100f), new Vector2(900f, 110f)).gameObject.AddComponent<Text>();
            before.gameObject.AddComponent<Outline>().effectDistance = new Vector2(1f, -1f);
            StoryTypography.Apply(before, 28, TextAnchor.MiddleLeft);
            var after = ui.Label("Outline after inline", new Vector2(0f, -70f), new Vector2(900f, 110f), 28, TextAnchor.MiddleLeft);
            after.gameObject.AddComponent<Outline>().effectDistance = new Vector2(1f, -1f);
            source = "按住 E · 等一下，再松开 E";
            before.text = after.text = source;
            yield return Keys(0.25f);
            Canvas.ForceUpdateCanvases();
            yield return null;
            var firstMesh = before.canvasRenderer.GetMesh();
            var secondMesh = after.canvasRenderer.GetMesh();
            var beforeIcons = before.GetComponent<InlineKeyIcon>();
            var afterIcons = after.GetComponent<InlineKeyIcon>();
            if (!Check(beforeIcons.RenderedIconCount == 2 && afterIcons.RenderedIconCount == 2 &&
                firstMesh.vertexCount > beforeIcons.TextVertexCountBeforeEffects && firstMesh.vertexCount == secondMesh.vertexCount &&
                before.text == source && after.text == source,
                "outline before or after inline replacement produces the same nonempty outlined text mesh exactly once")) yield break;
            yield return CaptureInline("ui-inline-keys-outline-order");
        }
        finally
        {
            Time.timeScale = priorTimeScale;
            Destroy(ui.gameObject);
        }
    }

    private static IEnumerator CaptureInline(string name)
    {
        Capture(name);
        // CaptureScreenshot saves at the end of the frame; keep this text and its icons intact.
        yield return new WaitForSecondsRealtime(0.15f);
    }
}
#endif
