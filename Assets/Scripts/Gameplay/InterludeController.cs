using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    /// <summary>Two playable illustrated transitions; no timer can skip the player's actions.</summary>
    public sealed class InterludeController : MonoBehaviour
    {
        [TextArea] [SerializeField] private string[] cards;
        [SerializeField] private float secondsPerCard = 2f;
        private JourneyOverlay ui;
        private JourneyArtPalette palette;
        private Image illustration, marker, handFill, keepsake;
        private AspectRatioFitter illustrationFit;
        private Image topShade, bottomShade;
        private Text title, instruction, status, markerLabel;
        private readonly Text[] stops = new Text[3];
        private readonly RawImage[] pieces = new RawImage[3];
        private readonly Image[] sockets = new Image[3];
        private readonly Text[] pieceLabels = new Text[3];
        private Image fullLetter;
        private Text addresses;
        private Texture2D repairPaperTexture;
        private Sprite repairPaperSprite;
        private bool growing, leaving;
        private float readyAt;
        public FirstsJourney Firsts { get; private set; }
        public RepairJourney Repair { get; private set; }
        public string Instruction => instruction == null ? "" : instruction.text;
        public bool HasIllustration => illustration != null && illustration.sprite != null;
        public bool Ready => growing ? Repair.Ready : Firsts.Phase == FirstsPhase.Ready;
        public System.Action<string> MomentChanged;
        private string previousMoment;
        private static readonly string[] StopNames = { "朋友家", "学校", "家" };

        // Retained for scene-builder and saved-scene compatibility.
        public void Configure(string[] values, float interval) { cards = values; secondsPerCard = interval; }

        private void Start()
        {
            growing = gameObject.scene.name == "04_Interlude_Growing";
            Firsts = new FirstsJourney();
            Repair = new RepairJourney();
            palette = Resources.Load<JourneyArtPalette>("JourneyArtPalette");
            var unusedCourage = GameObject.Find("Courage");
            if (unusedCourage != null) unusedCourage.SetActive(false);
            ui = JourneyOverlay.Create("Playable Interlude UI");
            ui.Picture("Page backdrop", Vector2.zero, new Vector2(1280, 720), null, new Color(0.018f, 0.024f, 0.034f));
            illustration = ui.Picture("Existing montage artwork", Vector2.zero, new Vector2(1280, 720), null, Color.white);
            illustration.preserveAspect = true;
            illustrationFit = illustration.gameObject.AddComponent<AspectRatioFitter>();
            illustrationFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            topShade = ui.Picture("Top cinematic shade", new Vector2(0, 310), new Vector2(1280, 100), null, new Color(0.01f, 0.015f, 0.022f, 0.82f));
            bottomShade = ui.Picture("Bottom cinematic shade", new Vector2(0, -255), new Vector2(1280, 210), null, new Color(0.01f, 0.015f, 0.022f, 0.88f));
            title = ui.Label("Moment title", new Vector2(0, 310), new Vector2(1000, 48), 27, TextAnchor.MiddleLeft);
            status = ui.Label("Moment state", new Vector2(0, -277), new Vector2(1120, 42), 21);
            instruction = ui.Label("Moment controls", new Vector2(0, -326), new Vector2(1160, 42), 20);
            if (growing) BuildRepair(); else BuildRide();
            Draw();
        }

        private Sprite Art(string id) => palette != null ? palette.Find(id) : null;
        private static float Along(float value) => -450f + 900f * value;
        private static float PaperAlong(float value) => -320f + 640f * value;

        private void BuildRide()
        {
            title.text = "第一次，一个人去学校";
            ui.Picture("Bus route", new Vector2(0, -190), new Vector2(900, 3), null, new Color(0.5f, 0.74f, 0.92f, 0.72f));
            for (var i = 0; i < stops.Length; i++)
            {
                var x = Along(FirstsJourney.Stops[i]);
                ui.Picture("Bus stop " + i, new Vector2(x, -190), new Vector2(7, 20), null, i == 1 ? JourneyVisuals.Warm : JourneyVisuals.Cool);
                stops[i] = ui.Label("Stop name " + i, new Vector2(x, -224), new Vector2(150, 30), 18);
            }
            marker = ui.Picture("Your position", new Vector2(Along(0.02f), -190), new Vector2(22, 22), null, JourneyVisuals.Warm);
            marker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            markerLabel = ui.Label("Passenger", Vector2.zero, new Vector2(100, 30), 18);
            handFill = ui.Picture("Raised hand", new Vector2(0, -188), new Vector2(0, 6), null, JourneyVisuals.Warm);
            keepsake = ui.Picture("What you brought", new Vector2(-565, -192), new Vector2(52, 52), Art(JourneyChoices.TookChildhoodToy ? "prop_toy" : "prop_child_backpack"), Color.white);
        }

        private void BuildRepair()
        {
            title.text = "被退回的那一页";
            repairPaperTexture = CreateRepairPaper();
            repairPaperSprite = Sprite.Create(repairPaperTexture,
                new Rect(0, 0, repairPaperTexture.width, repairPaperTexture.height), Vector2.one * 0.5f, 100f,
                0, SpriteMeshType.FullRect);
            repairPaperSprite.name = "One repaired report page";
            for (var i = 0; i < pieces.Length; i++)
            {
                sockets[i] = ui.Picture("Repair outline " + i, new Vector2(PaperAlong(RepairJourney.Target(i)), -100), new Vector2(160, 240), null, new Color(1f, 0.86f, 0.64f, 0.16f));
                pieces[i] = ui.Rect("Paper fragment " + i, new Vector2(PaperAlong(Repair.Pieces[i]), -100), new Vector2(160, 240)).gameObject.AddComponent<RawImage>();
                pieces[i].raycastTarget = false;
                pieces[i].texture = repairPaperTexture;
                pieces[i].uvRect = new Rect(i / 3f, 0f, 1f / 3f, 1f);
                pieceLabels[i] = ui.Label("Fragment number " + i, Vector2.zero, new Vector2(90, 30), 21);
            }
            fullLetter = ui.Picture("Repaired letter", new Vector2(0, -100), new Vector2(480, 240), repairPaperSprite, Color.white);
            fullLetter.gameObject.SetActive(false);
            addresses = ui.Label("Where the page goes", new Vector2(0, -220), new Vector2(1000, 40), 24);
        }

        private static Texture2D CreateRepairPaper()
        {
            const int width = 768;
            const int height = 384;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = "Runtime single rejected report", wrapMode = TextureWrapMode.Clamp };
            var seed = new System.Random(1741);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                var roughEdge = 5 + (int)(3f * Mathf.Sin((x + y * 0.7f) * 0.11f));
                var alpha = edge < roughEdge ? 0f : Mathf.Clamp01((edge - roughEdge) / 4f);
                var grain = (float)(seed.NextDouble() - 0.5) * 0.035f;
                texture.SetPixel(x, y, new Color(0.82f + grain, 0.76f + grain, 0.62f + grain, alpha));
            }
            DrawPaperLine(texture, 72, 300, 600, new Color(0.2f, 0.25f, 0.31f, 0.72f), 4);
            DrawPaperLine(texture, 72, 254, 645, new Color(0.2f, 0.25f, 0.31f, 0.6f), 3);
            DrawPaperLine(texture, 72, 208, 560, new Color(0.2f, 0.25f, 0.31f, 0.6f), 3);
            DrawPaperLine(texture, 72, 162, 630, new Color(0.2f, 0.25f, 0.31f, 0.6f), 3);
            DrawPaperLine(texture, 72, 116, 500, new Color(0.2f, 0.25f, 0.31f, 0.6f), 3);
            DrawPaperLine(texture, 610, 68, 100, new Color(0.5f, 0.18f, 0.14f, 0.65f), 5);
            texture.Apply();
            return texture;
        }

        private static void DrawPaperLine(Texture2D texture, int x, int y, int length, Color color, int thickness)
        {
            for (var py = Mathf.Max(0, y - thickness); py <= Mathf.Min(texture.height - 1, y + thickness); py++)
            for (var px = Mathf.Max(0, x); px < Mathf.Min(texture.width, x + length); px++)
            {
                var existing = texture.GetPixel(px, py);
                texture.SetPixel(px, py, Color.Lerp(existing, color, color.a));
            }
        }

        private void Update()
        {
            if (ui == null || leaving) return;
            var wasReady = Ready;
            if (growing)
                Repair.Tick(Time.deltaTime, GameInput.Horizontal, GameInput.SelectionPressed, GameInput.InteractPressed, GameInput.InteractHeld, GameInput.InteractReleased);
            else
            {
                if (GameInput.UsePressed && Firsts.Phase == FirstsPhase.Riding) Firsts.Bell();
                Firsts.Tick(Time.deltaTime, GameInput.Horizontal, GameInput.InteractHeld, GameInput.InteractReleased);
            }
            if (!wasReady && Ready)
            {
                readyAt = Time.time;
                if (growing) JourneyChoices.RememberRepairedNote(Repair.Destination);
                else JourneyChoices.RememberBusStop(Firsts.ExitStop);
                SceneAudio.Instance?.PlayObjective();
            }
            Draw();
            if (Ready && Time.time - readyAt > 0.35f && GameInput.UsePressed)
            {
                leaving = true;
                StorySceneDirector.Instance?.LoadNextScene();
            }
        }

        private void Draw()
        {
            string art;
            if (!growing)
            {
                art = Firsts.Phase == FirstsPhase.Riding ? "montage02_takebus" :
                    Firsts.Phase >= FirstsPhase.RaisingHand ? "montage04_firstraisehand" :
                    Firsts.ExitStop == 2 ? "montage03_parentstired" : "montage01_stayupwithfriends";
                marker.rectTransform.anchoredPosition = new Vector2(Along(Firsts.Position), -190);
                markerLabel.rectTransform.anchoredPosition = new Vector2(Along(Firsts.Position), -157);
                markerLabel.text = Firsts.Phase == FirstsPhase.Riding ? "公交" : "你";
                for (var i = 0; i < stops.Length; i++) stops[i].text = StopNames[i] + (i == 1 ? " · 目的地" : "");
                handFill.gameObject.SetActive(Firsts.Phase == FirstsPhase.RaisingHand);
                handFill.rectTransform.sizeDelta = new Vector2(240f * Firsts.RaisedHand, 10);
                if (Firsts.Phase == FirstsPhase.Riding)
                {
                    status.text = Firsts.RequestedStop < 0 ? "目的地：学校。车会继续开，按铃才会在下一站停下。" : "已按铃：下一站 " + StopNames[Firsts.RequestedStop];
                    instruction.text = Firsts.RequestedStop < 0 ? "看站牌 · F 按铃，在下一站下车" : "F 再按一次可取消下车";
                }
                else if (Firsts.Phase == FirstsPhase.Walking)
                {
                    status.text = Firsts.ExitStop == 0 ? "在朋友家这站下了车。学校在右边，可以自己走过去。" : "坐到了家门口。家里的人睡着了，学校还在左边。";
                    instruction.text = "A / D 或方向键 · 沿路线走到学校";
                }
                else
                {
                    status.text = Firsts.Phase == FirstsPhase.Ready ? "你的手举起来了。" : "教室里，老师还在等有人举手。";
                    instruction.text = Firsts.Phase == FirstsPhase.Ready ? "F · 走向舞台" : Firsts.RaisedHand >= 0.98f ? "松开 E · 把手举出去" : "按住 E · 慢慢举起手";
                }
            }
            else
            {
                art = "bg_research_room";
                for (var i = 0; i < pieces.Length; i++)
                {
                    var show = Repair.RepairedCount < 3;
                    pieces[i].gameObject.SetActive(show);
                    sockets[i].gameObject.SetActive(show);
                    pieceLabels[i].gameObject.SetActive(show);
                    pieces[i].rectTransform.anchoredPosition = new Vector2(PaperAlong(Repair.Pieces[i]), -100 + (Repair.Holding && Repair.Selected == i ? 16 : 0));
                    pieces[i].color = Repair.Placed[i] ? new Color(0.8f, 1f, 0.8f) : Repair.Selected == i ? Color.white : new Color(0.65f, 0.65f, 0.65f);
                    pieceLabels[i].rectTransform.anchoredPosition = new Vector2(PaperAlong(Repair.Pieces[i]), 35);
                    pieceLabels[i].text = Repair.Placed[i] ? "✓" : "";
                }
                fullLetter.gameObject.SetActive(Repair.RepairedCount == 3);
                fullLetter.rectTransform.anchoredPosition = new Vector2(PaperAlong(Repair.LetterPosition), -100);
                addresses.text = Repair.RepairedCount == 3 ? "寄回家里                                        留进自己的笔记" : "";
                status.text = Repair.Ready ? Repair.Destination == "home" ? "纸寄出去了。家里回了一张画着窗与灯的小图。" : "修补过的这一页，留在了自己的笔记里。" :
                    Repair.RepairedCount == 3 ? "这一页已经完整。决定把它留在哪里。" : "对照浅色轮廓，把三片纸拼回原位。已拼好 " + Repair.RepairedCount + " / 3";
                instruction.text = Repair.Ready ? "F · 带着它继续，进入研究室" :
                    Repair.RepairedCount == 3 ? "按住 E 拿起整页 · A / D 移到左侧或右侧 · 松开 E 放下" :
                    Repair.Holding ? "A / D 移动纸片 · 对齐对应轮廓后松开 E" : "A / D 选纸片 · 按住 E 拿起，再用 A / D 移动";
            }
            illustration.sprite = Art(art);
            if (illustration.sprite != null && illustrationFit != null)
                illustrationFit.aspectRatio = illustration.sprite.rect.width / illustration.sprite.rect.height;
            if (previousMoment != art) { previousMoment = art; MomentChanged?.Invoke(art); }
        }

        private void OnDestroy()
        {
            if (ui != null) Destroy(ui.gameObject);
            if (repairPaperSprite != null) Destroy(repairPaperSprite);
            if (repairPaperTexture != null) Destroy(repairPaperTexture);
        }
    }
}
