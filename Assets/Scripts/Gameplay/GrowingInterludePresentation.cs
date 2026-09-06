using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    /// <summary>Replaceable presentation for the paper model. All geometry is driven by the same support model as the walk.</summary>
    public sealed class GrowingInterludePresentation : System.IDisposable
    {
        private const float DiagramWidth = 860f;
        private const float DeckY = 18f;
        private const float FootHeight = (PaperBridgeJourney.FlatWidth - PaperBridgeJourney.FoldedWidth) * DiagramWidth * 0.5f;
        public const float SupportBaselineY = DeckY - FootHeight;
        private readonly Image[] decks = new Image[3];
        private readonly Image[] leftFeet = new Image[3];
        private readonly Image[] rightFeet = new Image[3];
        private readonly float[] drop = new float[3];
        private readonly Image walker;
        private readonly Image selection;
        private readonly Text selectedLabel;
        private readonly Text handLabel;
        private readonly Text failureMark;
        private readonly Text rule;
        private Sprite paperStrip;
        private Sprite walkerIdle;
        private Sprite walkerWalk;
        private Sprite walkerFear;
        private bool ownsWalkerSprites;

        public string Status { get; private set; }
        public string Instruction { get; private set; }
        public string MomentArt => "montage_growth_story";
        public bool HasPaperArtwork => paperStrip != null;
        public bool HasWalkerArtwork => walker != null && walker.sprite != null;

        public GrowingInterludePresentation(JourneyOverlay ui, JourneyArtPalette palette)
        {
            var note = palette != null ? palette.Find("prop_researchnote") : null;
            if (note != null)
            {
                var texture = note.texture;
                // Reuse the delivered note's paper grain. This is a functional strip, not a new artwork asset.
                var scaleX = texture.width / 1298f;
                var scaleY = texture.height / 1212f;
                paperStrip = Sprite.Create(texture, new Rect(150f * scaleX, 740f * scaleY, 310f * scaleX, 55f * scaleY),
                    Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect);
                paperStrip.name = "Paper bridge strip from delivered note";
            }

            // Keep the delivered memory visible. The bridge sits on one quiet work area instead of
            // a full opaque debug panel pasted over the illustration.
            ui.Picture("Paper bridge work area", new Vector2(0f, 24f), new Vector2(1050f, 250f), null,
                new Color(0.018f, 0.025f, 0.033f, 0.68f));
            rule = ui.Label("Paper bridge rule", new Vector2(0f, 150f), new Vector2(920f, 36f), 18);
            rule.text = "展平更长。折出桥脚，就能站在桌上。";
            rule.color = StoryTypography.Secondary;
            ui.Picture("Paper bridge desk edge", new Vector2(0f, SupportBaselineY - 4f), new Vector2(990f, 10f), null,
                new Color(0.17f, 0.15f, 0.13f, 0.92f));
            ui.Picture("Paper bridge support baseline", new Vector2(0f, SupportBaselineY + 1f), new Vector2(990f, 2f), null,
                new Color(0.76f, 0.61f, 0.39f, 0.68f));

            var book = palette != null ? palette.Find("prop_research_report") : null;
            for (var side = 0; side < 2; side++)
            {
                var bankCenter = side == 0 ? -447.5f : 447.5f;
                var bank = ui.Picture(side == 0 ? "Paper bridge left book" : "Paper bridge right book",
                    new Vector2(bankCenter, DeckY - FootHeight * 0.5f), new Vector2(135f, FootHeight), book,
                    new Color(0.78f, 0.73f, 0.63f, 0.92f));
                bank.preserveAspect = true;
                var bookTop = ui.Picture("Book top " + side, new Vector2(bankCenter, DeckY), new Vector2(225f, 7f), paperStrip,
                    new Color(0.9f, 0.82f, 0.66f));
                bookTop.preserveAspect = false;
            }

            selection = ui.Picture("Selected bridge paper", Vector2.zero, new Vector2(20f, 3f), null, JourneyVisuals.Warm);
            for (var i = 0; i < decks.Length; i++)
            {
                leftFeet[i] = ui.Picture("Paper bridge left foot " + i, Vector2.zero, new Vector2(9f, FootHeight), paperStrip, Color.white);
                rightFeet[i] = ui.Picture("Paper bridge right foot " + i, Vector2.zero, new Vector2(9f, FootHeight), paperStrip, Color.white);
                decks[i] = ui.Picture("Paper bridge span " + i, Vector2.zero, new Vector2(300f, 12f), paperStrip, Color.white);
                leftFeet[i].preserveAspect = rightFeet[i].preserveAspect = decks[i].preserveAspect = false;
            }
            selectedLabel = ui.Label("Selected bridge paper description", Vector2.zero, new Vector2(190f, 28f), 16);
            handLabel = ui.Label("Paper bridge holding hand", Vector2.zero, new Vector2(170f, 35f), 18);
            handLabel.color = JourneyVisuals.Warm;
            failureMark = ui.Label("Paper bridge exact failure", Vector2.zero, new Vector2(40f, 45f), 30);
            failureMark.color = new Color(1f, 0.60f, 0.44f);
            failureMark.text = "↓";
            SetupWalkerSprites(palette != null ? palette.Find("prop_research_model") : null);
            walker = ui.Picture("Paper bridge walker", Vector2.zero, new Vector2(48f, 84f), walkerIdle, Color.white);
            walker.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        public void Draw(PaperBridgeJourney journey, float dt, float time)
        {
            var testing = journey.Phase != PaperBridgePhase.Editing;
            for (var i = 0; i < decks.Length; i++)
            {
                var collapse = testing && !journey.IsStable(i) ? FootHeight - 6f : 0f;
                drop[i] = Mathf.MoveTowards(drop[i], collapse, Mathf.Max(0f, dt) * 220f);
                var center = Along(journey.Positions[i]);
                var width = journey.Width(i) * DiagramWidth;
                decks[i].rectTransform.anchoredPosition = new Vector2(center, DeckY - drop[i]);
                decks[i].rectTransform.sizeDelta = new Vector2(width, 12f);
                var color = i == journey.Selected ? Color.white : new Color(0.78f, 0.76f, 0.72f);
                decks[i].color = color;
                leftFeet[i].color = rightFeet[i].color = color;
                leftFeet[i].gameObject.SetActive(journey.Folded[i]);
                rightFeet[i].gameObject.SetActive(journey.Folded[i]);
                leftFeet[i].rectTransform.anchoredPosition = new Vector2(center - width * 0.5f + 4.5f, DeckY - FootHeight * 0.5f);
                rightFeet[i].rectTransform.anchoredPosition = new Vector2(center + width * 0.5f - 4.5f, DeckY - FootHeight * 0.5f);
            }
            selection.rectTransform.anchoredPosition = new Vector2(Along(journey.Positions[journey.Selected]), DeckY - drop[journey.Selected] + 12f);
            selection.rectTransform.sizeDelta = new Vector2(journey.Width(journey.Selected) * DiagramWidth, 2f);
            var selectedX = Along(journey.Positions[journey.Selected]);
            selectedLabel.text = "第 " + (journey.Selected + 1) + " 片 · " + (journey.Folded[journey.Selected] ? "已折桥脚" : "展平");
            selectedLabel.rectTransform.anchoredPosition = new Vector2(selectedX, DeckY + 44f);
            handLabel.gameObject.SetActive(journey.Holding);
            handLabel.text = journey.Phase == PaperBridgePhase.Editing ? "移动中" : "手托住这里";
            handLabel.rectTransform.anchoredPosition = new Vector2(selectedX, SupportBaselineY - 38f);
            failureMark.gameObject.SetActive(journey.Phase == PaperBridgePhase.Blocked);
            failureMark.rectTransform.anchoredPosition = new Vector2(Along(journey.FailureX), DeckY + 29f);
            var walking = journey.Phase == PaperBridgePhase.Walking;
            walker.sprite = journey.Phase == PaperBridgePhase.Blocked ? walkerFear :
                walking && Mathf.Repeat(time * 3.5f, 1f) > 0.5f ? walkerWalk : walkerIdle;
            walker.rectTransform.anchoredPosition = new Vector2(Along(journey.WalkerX), DeckY + 6f + (walking ? Mathf.Abs(Mathf.Sin(time * 11f)) * 2.5f : 0f));

            if (journey.Ready)
            {
                Status = journey.Independent ? "手松开了。桥还站着，它走到了对面。" : "你托着这页纸，送它走到了对面。";
                Instruction = "F · 带着这次尝试继续　Q · 留下来，再试一种搭法";
                rule.text = journey.Independent ? "这副折纸桥脚，带去下一张工作台。" : "它已经过去了。现在可以松开手。";
            }
            else if (walking)
            {
                Status = journey.Holding ? "手正托着第 " + (journey.Selected + 1) + " 片纸。" : "让它试着走过去。";
                Instruction = "按住 E · 托住选中的纸　松开 E · 放手　Q · 召回，保留布局";
                rule.text = "它会踩过你实际搭出的每一段。";
            }
            else
            {
                Status = journey.Phase == PaperBridgePhase.Blocked ?
                    journey.Failure == PaperBridgeFailure.Gap ? "这里断开了。把两段接起来，再试一次。" :
                    "这里塌下了。长纸两端需要支撑，也可以用手托住。" :
                    "用这页纸，给它搭一条路。";
                Instruction = "A D · 选纸　按住 E + A D · 移动　空格 · 折叠 / 展平　F · 试走";
                rule.text = "展平更长。折出桥脚，就能站在桌上。";
            }
        }

        private void SetupWalkerSprites(Sprite source)
        {
            if (source == null) return;
            walkerIdle = walkerWalk = walkerFear = source;
            if (source.texture.name != "prop_research_model") return;
            var texture = source.texture;
            var sx = texture.width / 1882f;
            var sy = texture.height / 836f;
            walkerIdle = CropWalker(texture, new Rect(255f * sx, 12f * sy, 402f * sx, 806f * sy), "Paper walker idle");
            walkerWalk = CropWalker(texture, new Rect(742f * sx, 11f * sy, 486f * sx, 795f * sy), "Paper walker walking");
            walkerFear = CropWalker(texture, new Rect(1296f * sx, 0f, 430f * sx, 782f * sy), "Paper walker stopped");
            ownsWalkerSprites = true;
        }

        private static Sprite CropWalker(Texture2D texture, Rect rect, string name)
        {
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        private static float Along(float value) => (value - 0.5f) * DiagramWidth;

        public void Dispose()
        {
            if (paperStrip != null) Object.Destroy(paperStrip);
            if (!ownsWalkerSprites) return;
            if (walkerIdle != null) Object.Destroy(walkerIdle);
            if (walkerWalk != null) Object.Destroy(walkerWalk);
            if (walkerFear != null) Object.Destroy(walkerFear);
        }
    }
}
