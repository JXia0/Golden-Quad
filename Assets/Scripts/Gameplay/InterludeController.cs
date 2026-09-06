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
        private GrowingInterludePresentation bridgePresentation;
        private bool growing, leaving;
        private float readyAt;
        public FirstsJourney Firsts { get; private set; }
        public RepairJourney Repair { get; private set; }
        public PaperBridgeJourney Bridge { get; private set; }
        public string Instruction => instruction == null ? "" : instruction.text;
        public bool HasIllustration => illustration != null && illustration.sprite != null;
        public bool Ready => growing ? Bridge != null && Bridge.Ready : Firsts != null && Firsts.Phase == FirstsPhase.Ready;
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
            if (growing) Bridge = new PaperBridgeJourney();
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
            title = ui.Label("Moment title", new Vector2(0, 310), new Vector2(1120, 48), 30, TextAnchor.MiddleLeft);
            StoryTypography.ApplyTitle(title, 30);
            status = ui.Label("Moment state", new Vector2(0, -277), new Vector2(1120, 42), 21);
            instruction = ui.Label("Moment controls", new Vector2(0, -326), new Vector2(1120, 42), 20);
            instruction.color = StoryTypography.Secondary;
            if (growing) BuildBridge(); else BuildRide();
            Draw();
        }

        private Sprite Art(string id) => palette != null ? palette.Find(id) : null;
        private static float Along(float value) => -450f + 900f * value;

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
            handFill = ui.Picture("Raised hand", new Vector2(Along(FirstsJourney.Stops[1]), -190), new Vector2(0, 6), null, JourneyVisuals.Warm);
            keepsake = ui.Picture("What you brought", new Vector2(-565, -192), new Vector2(52, 52), Art(JourneyChoices.TookChildhoodToy ? "prop_toy" : "prop_child_backpack"), Color.white);
        }

        private void BuildBridge()
        {
            title.text = "被退回的那一页";
            bridgePresentation = new GrowingInterludePresentation(ui, palette);
        }

        private void Update()
        {
            if (GameInput.IsPaused) return;
            if (ui == null || leaving) return;
            var wasReady = Ready;
            if (growing)
            {
                var previousPhase = Bridge.Phase;
                Bridge.Tick(Time.deltaTime, GameInput.Horizontal, GameInput.SelectionPressed, GameInput.InteractHeld,
                    GameInput.JumpPressed, GameInput.UsePressed, GameInput.ReconsiderPressed);
                if (GameInput.JumpPressed && previousPhase != PaperBridgePhase.Walking && previousPhase != PaperBridgePhase.Complete)
                    SceneAudio.Instance?.PlayItemMove();
                if (previousPhase == PaperBridgePhase.Walking && Bridge.Phase == PaperBridgePhase.Blocked)
                    SceneAudio.Instance?.PlayRelease();
            }
            else
            {
                if (GameInput.UsePressed && Firsts.Phase == FirstsPhase.Riding) Firsts.Bell();
                Firsts.Tick(Time.deltaTime, GameInput.Horizontal, GameInput.InteractHeld, GameInput.InteractReleased);
            }
            if (!wasReady && Ready)
            {
                readyAt = Time.time;
                if (growing) JourneyChoices.RememberPaperBridge(Bridge.Independent, Bridge.AttemptCount);
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
                    status.text = Firsts.RequestedStop < 0 ? "去学校的车，还在向前开。" : "下一站：" + StopNames[Firsts.RequestedStop];
                    instruction.text = Firsts.RequestedStop < 0 ? "F · 按铃，在下一站下车" : "F · 取消下车";
                }
                else if (Firsts.Phase == FirstsPhase.Walking)
                {
                    status.text = Firsts.ExitStop == 0 ? "提前下车了。学校在右边。" : "坐过站了。学校在左边。";
                    instruction.text = "A D 或方向键 · 走到学校";
                }
                else
                {
                    status.text = Firsts.Phase == FirstsPhase.Ready ? "轮到你了。" : "老师在等一个举起的手。";
                    instruction.text = Firsts.Phase == FirstsPhase.Ready ? "F · 继续" : Firsts.RaisedHand >= 0.98f ? "松开 E · 举起手" : "按住 E · 慢慢抬起手";
                }
            }
            else
            {
                bridgePresentation.Draw(Bridge, Time.deltaTime, Time.time);
                art = bridgePresentation.MomentArt;
                status.text = bridgePresentation.Status;
                instruction.text = bridgePresentation.Instruction;
            }
            illustration.sprite = Art(art);
            if (illustration.sprite != null && illustrationFit != null)
                illustrationFit.aspectRatio = illustration.sprite.rect.width / illustration.sprite.rect.height;
            if (previousMoment != art) { previousMoment = art; MomentChanged?.Invoke(art); }
        }

        private void OnDestroy()
        {
            if (ui != null) Destroy(ui.gameObject);
            bridgePresentation?.Dispose();
        }
    }
}
