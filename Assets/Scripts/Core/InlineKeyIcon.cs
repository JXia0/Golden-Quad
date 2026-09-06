using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    /// <summary>Reflows key pictures without changing the gameplay-owned Text.text value.</summary>
    [RequireComponent(typeof(Text)), DisallowMultipleComponent, DefaultExecutionOrder(1100)]
    public sealed class InlineKeyIcon : BaseMeshEffect
    {
        private struct Token { public int SourceIndex; public char Key; public Color32 Marker; public int PictureSize; }
        private struct Placement { public int TokenIndex; public Vector2 Center; public float Size; }
        private Text owner;
        private JourneyArtPalette palette;
        private Sprite eSprite, fSprite;
        private readonly TextGenerator generator = new();
        private readonly StringBuilder visible = new();
        private readonly StringBuilder display = new();
        private readonly List<int> sourceIndices = new();
        private readonly List<Token> tokens = new();
        private readonly List<Placement> placements = new();
        private readonly List<Image> images = new();
        private readonly List<BaseMeshEffect> effects = new();
        private readonly UIVertex[] quad = new UIVertex[4];
        private string preparedSource, layoutSource, displayedText;
        private bool preparedRichText;
        private int preparedFontSize;
        private char placeholder;
        private int renderedCount;
        private Font originalFont;
        private bool usesActionFont;

        public int RenderedIconCount => isActiveAndEnabled && owner != null && owner.isActiveAndEnabled ? renderedCount : 0;
        public int ReplacedQuadCount => placements.Count;
        public int ReplacementTokenCount => tokens.Count;
        public int GeneratedVertexCount { get; private set; }
        public int RemovedMarkerVertexCount { get; private set; }
        public int TextVertexCountBeforeEffects { get; private set; }
        public string SourceTextAtLastBuild => layoutSource;
        public string DisplayText => displayedText;
        public Vector2 GetIconCenterLocal(int index) => index >= 0 && index < renderedCount ? (Vector2)images[index].rectTransform.localPosition : Vector2.zero;
        public Rect GetIconRectLocal(int index)
        {
            if (index < 0 || index >= renderedCount) return default;
            var rect = images[index].rectTransform;
            return new Rect((Vector2)rect.localPosition - rect.sizeDelta * 0.5f, rect.sizeDelta);
        }
        public char GetIconKey(int index) => index >= 0 && index < placements.Count ? tokens[placements[index].TokenIndex].Key : '\0';

        public static InlineKeyIcon Ensure(Text text)
        {
            if (text == null) return null;
            var value = text.GetComponent<InlineKeyIcon>();
            return value != null ? value : text.gameObject.AddComponent<InlineKeyIcon>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            owner = GetComponent<Text>();
            Font.textureRebuilt += FontRebuilt;
            preparedSource = layoutSource = null;
        }
        protected override void OnDisable()
        {
            Font.textureRebuilt -= FontRebuilt;
            HideImages();
            base.OnDisable();
        }
        protected override void OnDestroy() { ((System.IDisposable)generator).Dispose(); base.OnDestroy(); }
        private void FontRebuilt(Font changed) { if (owner != null && owner.font == changed) generator.Invalidate(); }
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            layoutSource = null;
        }

        private void LateUpdate()
        {
            if (owner == null) owner = GetComponent<Text>();
            if (owner == null || !owner.isActiveAndEnabled || owner.color.a <= 0f) { HideImages(); return; }
            palette ??= Resources.Load<JourneyArtPalette>("JourneyArtPalette");
            var nextE = palette != null ? palette.Find("ui_interact_e") : null;
            var nextF = palette != null ? palette.Find("ui_interact_f") : null;
            if (eSprite != nextE || fSprite != nextF)
            {
                eSprite = nextE;
                fSprite = nextF;
                preparedSource = null;
                owner.SetVerticesDirty();
            }
            if (preparedSource != owner.text || preparedRichText != owner.supportRichText || preparedFontSize != owner.fontSize)
            {
                PrepareDisplay();
                owner.SetVerticesDirty();
            }
            if (tokens.Count > 0 && owner.font != StoryTypography.PromptFont)
            {
                originalFont = owner.font;
                usesActionFont = true;
                owner.font = StoryTypography.PromptFont;
                layoutSource = null;
            }
            else if (tokens.Count == 0 && usesActionFont)
            {
                // A label reused for prose gets its original typography back.
                if (owner.font == StoryTypography.PromptFont && originalFont != null) owner.font = originalFont;
                usesActionFont = false;
                layoutSource = null;
            }
            // Canvas mesh modification only stores positions. Creating or updating Images there
            // would register another Graphic while CanvasUpdateRegistry is already rebuilding.
            while (images.Count < tokens.Count)
            {
                var picture = new GameObject("Inline Key " + images.Count, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                picture.transform.SetParent(owner.transform, false);
                picture.rectTransform.anchorMin = picture.rectTransform.anchorMax = owner.rectTransform.pivot;
                picture.rectTransform.pivot = Vector2.one * 0.5f;
                picture.preserveAspect = true;
                picture.raycastTarget = false;
                picture.enabled = false;
                images.Add(picture);
            }
            renderedCount = layoutSource == owner.text ? Mathf.Min(placements.Count, images.Count) : 0;
            for (var i = 0; i < images.Count; i++)
            {
                var picture = images[i];
                var show = i < renderedCount;
                if (picture.enabled != show) picture.enabled = show;
                if (!show) continue;
                var placement = placements[i];
                var token = tokens[placement.TokenIndex];
                var sprite = token.Key == 'E' ? eSprite : fSprite;
                if (picture.sprite != sprite) picture.sprite = sprite;
                if (picture.color != owner.color) picture.color = owner.color;
                var rect = picture.rectTransform;
                if ((Vector2)rect.localPosition != placement.Center) rect.localPosition = placement.Center;
                var size = Vector2.one * placement.Size;
                if (rect.sizeDelta != size) rect.sizeDelta = size;
            }
        }

        private void HideImages()
        {
            renderedCount = 0;
            foreach (var picture in images) if (picture != null && picture.enabled) picture.enabled = false;
        }

        private void PrepareDisplay()
        {
            preparedSource = owner.text ?? "";
            preparedRichText = owner.supportRichText;
            preparedFontSize = owner.fontSize;
            layoutSource = null;
            placements.Clear();
            tokens.Clear();
            BuildVisible(preparedSource, preparedRichText, visible, sourceIndices);
            for (var i = 0; i < visible.Length; i++)
            {
                var key = visible[i];
                if ((key != 'E' || eSprite == null) && (key != 'F' || fSprite == null)) continue;
                if (!IsStandalone(visible, i)) continue;
                var index = tokens.Count;
                var marker = new Color32((byte)(16 + index % 30 * 8), 251, (byte)(17 + index / 30 * 8), 255);
                tokens.Add(new Token { SourceIndex = sourceIndices[i], Key = key, Marker = marker,
                    PictureSize = Mathf.CeilToInt(EffectiveFontSize(preparedSource, sourceIndices[i], owner.fontSize, preparedRichText) * 1.8f) });
            }
            placeholder = owner.font != null && owner.font.HasCharacter('口') ? '口' : 'M';
            display.Clear();
            var start = 0;
            foreach (var token in tokens)
            {
                AppendSource(start, token.SourceIndex - start);
                // Two en-spaces plus a full-width glyph reserve about two em horizontally.
                // Keeping the glyph's original size lets a 22px action remain valid in a 32px
                // high Text; the complete 40px key picture can use the surrounding line space.
                display.Append('\u2002').Append("<color=#")
                    .Append(token.Marker.r.ToString("X2")).Append(token.Marker.g.ToString("X2"))
                    .Append(token.Marker.b.ToString("X2")).Append('>').Append(placeholder).Append("</color>").Append('\u2002');
                start = token.SourceIndex + 1;
            }
            AppendSource(start, preparedSource.Length - start);
            displayedText = display.ToString();
        }

        private void AppendSource(int start, int count)
        {
            if (preparedRichText) { display.Append(preparedSource, start, count); return; }
            // A zero-width separator leaves literal '<b>' visible when rich text was disabled,
            // while the private generator can still understand our own marker tags.
            for (var i = start; i < start + count; i++)
            {
                display.Append(preparedSource[i]);
                if (preparedSource[i] == '<') display.Append('\u200B');
            }
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || owner == null || owner.font == null) return;
            if (preparedSource != owner.text || preparedRichText != owner.supportRichText || preparedFontSize != owner.fontSize) PrepareDisplay();
            placements.Clear();
            layoutSource = owner.text;
            GeneratedVertexCount = RemovedMarkerVertexCount = TextVertexCountBeforeEffects = 0;
            if (tokens.Count == 0) return;
            var settings = owner.GetGenerationSettings(owner.rectTransform.rect.size);
            settings.richText = true;
            generator.PopulateWithErrors(displayedText, settings, owner.gameObject);
            if (generator.lineCount > 1 && settings.lineSpacing < 1.9f)
            {
                var lines = generator.lines;
                var last = lines[lines.Count - 1];
                var height = (Mathf.Abs(lines[0].topY - last.topY) + last.height) / owner.pixelsPerUnit;
                if (height * (1.9f / settings.lineSpacing) <= owner.rectTransform.rect.height)
                {
                    settings.lineSpacing = 1.9f;
                    generator.PopulateWithErrors(displayedText, settings, owner.gameObject);
                }
            }
            var vertices = generator.verts;
            GeneratedVertexCount = vertices.Count;
            if (vertices.Count == 0) { mesh.Clear(); return; }
            var units = 1f / owner.pixelsPerUnit;
            var origin = (Vector2)vertices[0].position * units;
            var rounding = owner.PixelAdjustPoint(origin) - origin;
            mesh.Clear();
            // Matches local UGUI Text.OnPopulateMesh's quad iteration and pixel adjustment.
            // String indices are never assumed to equal generated glyph indices.
            for (var v = 0; v + 3 < vertices.Count; v += 4)
            {
                var markerIndex = FindMarker(vertices[v].color);
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                for (var j = 0; j < 4; j++)
                {
                    quad[j] = vertices[v + j];
                    quad[j].position *= units;
                    quad[j].position += (Vector3)rounding;
                    min = Vector2.Min(min, quad[j].position);
                    max = Vector2.Max(max, quad[j].position);
                }
                if (markerIndex >= 0)
                {
                    RemovedMarkerVertexCount += 4;
                    // Truncated and whitespace quads must not put a stray key at the origin.
                    if (max.x - min.x > 0.01f && max.y - min.y > 0.01f)
                        placements.Add(new Placement { TokenIndex = markerIndex, Center = (min + max) * 0.5f,
                            Size = tokens[markerIndex].PictureSize });
                }
                else mesh.AddUIVertexQuad(quad);
            }
            TextVertexCountBeforeEffects = mesh.currentVertCount;
            // The replacement supersedes effects applied before us. Reapply preceding stateless
            // Shadow/Outline effects; later effects are applied once by UGUI itself.
            GetComponents(effects);
            foreach (var effect in effects)
            {
                if (effect == this) break;
                if (effect is Shadow && effect.IsActive()) effect.ModifyMesh(mesh);
            }
        }

        private int FindMarker(Color32 value)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                var color = tokens[i].Marker;
                if (value.r == color.r && value.g == color.g && value.b == color.b) return i;
                var multiplied = (Color32)((Color)color * owner.color);
                if (value.r == multiplied.r && value.g == multiplied.g && value.b == multiplied.b) return i;
            }
            return -1;
        }

        public static int CountStandaloneKeys(string source, char key, bool richText = true)
        {
            var text = new StringBuilder();
            var indices = new List<int>();
            BuildVisible(source ?? "", richText, text, indices);
            var count = 0;
            for (var i = 0; i < text.Length; i++) if (text[i] == key && IsStandalone(text, i)) count++;
            return count;
        }

        private static int EffectiveFontSize(string source, int position, int initialSize, bool richText)
        {
            if (!richText) return initialSize;
            var current = initialSize;
            var previous = new Stack<int>();
            for (var i = 0; i < position; i++)
            {
                if (source[i] != '<') continue;
                var end = source.IndexOf('>', i + 1);
                if (end < 0 || end >= position) continue;
                var tag = source.Substring(i + 1, end - i - 1).Trim();
                if (tag.StartsWith("size=", System.StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(tag.Substring(5), out var size) && size > 0)
                {
                    previous.Push(current);
                    current = size;
                }
                else if (tag.Equals("/size", System.StringComparison.OrdinalIgnoreCase) && previous.Count > 0)
                    current = previous.Pop();
                i = end;
            }
            return current;
        }

        private static bool IsStandalone(StringBuilder value, int i)
        {
            if (i > 0 && IsWord(value[i - 1]) || i + 1 < value.Length && IsWord(value[i + 1])) return false;
            if (i > 1 && (value[i - 1] == '-' || value[i - 1] == '\'') && IsWord(value[i - 2])) return false;
            if (i + 2 < value.Length && (value[i + 1] == '-' || value[i + 1] == '\'') && IsWord(value[i + 2])) return false;
            if (i + 2 < value.Length && value[i + 1] == ':' && (value[i + 2] == '/' || value[i + 2] == '\\')) return false;
            return true;
        }
        private static bool IsWord(char value) => value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '_';

        private static void BuildVisible(string source, bool richText, StringBuilder result, List<int> indices)
        {
            result.Clear();
            indices.Clear();
            for (var i = 0; i < source.Length; i++)
            {
                if (richText && source[i] == '<')
                {
                    var end = source.IndexOf('>', i + 1);
                    if (end > i && IsFormattingTag(source, i + 1, end)) { i = end; continue; }
                }
                result.Append(source[i]);
                indices.Add(i);
            }
        }
        private static bool IsFormattingTag(string source, int start, int end)
        {
            if (source[start] == '/') start++;
            var length = 0;
            while (start + length < end && char.IsLetter(source[start + length])) length++;
            var name = source.Substring(start, length).ToLowerInvariant();
            return name == "b" || name == "i" || name == "size" || name == "color" || name == "material" || name == "quad";
        }
    }
}
