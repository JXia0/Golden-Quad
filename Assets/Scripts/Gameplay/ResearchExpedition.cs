using System;
using UnityEngine;

namespace LetGo
{
    /// <summary>A reusable-object workshop with optional exploration, physical gates and several solutions.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class ResearchExpedition : MonoBehaviour
    {
        private StorySceneDirector director;
        private HandConnection hand;
        private PlayerController2D player;
        private JourneyVisuals visuals;
        private readonly ExpeditionLearner learner = new();
        private HoldTarget crate, plank, lamp, toy, actor, report;
        private HoldSocket desk;
        private BoxCollider2D crateCollider, shutter;
        private Transform shutterVisual;
        private SpriteRenderer shutterDoorVisual, plateVisual, latchLockedVisual, latchUnlockedVisual;
        private Sprite plateRaisedSprite, platePressedSprite;
        private readonly System.Collections.Generic.Dictionary<Texture, Material> blackKeyMaterials = new();
        private LineRenderer wire, gap, comfort, wind, map, toySound;
        private ResearchGuide guide;
        public bool ShowingLampRange => comfort != null && comfort.enabled && comfort.gameObject.activeInHierarchy && comfort.startWidth > 0f;
        private float gateGrace, toyUntil;
        private string activePrompt;
        private bool arrived, gateWasOpen, clearingShutter;
        private PhysicsMaterial2D toolSurface;
        private LearnerState previous;
        public bool GateLatched { get; private set; }
        public bool GateOpen => GateLatched || gateGrace > 0f;
        public bool WindActive { get; private set; }
        public bool ShortcutOpen => arrived;
        public bool ReportReturned => desk != null && desk.Completed;
        public int CluesFound { get; private set; }
        public LearnerState State => learner.State;
        public float LearnedUntil => learner.LearnedUntil;
        public int Rehearsals => learner.Rehearsals;
        public HoldTarget Crate => crate;
        public HoldTarget Plank => plank;
        public HoldTarget Lamp => lamp;
        public HoldTarget Toy => toy;
        public HoldTarget Learner => actor;
        public HoldTarget Report => report;
        public bool LampWorking => !WindActive || lamp.transform.position.x < 17f;
        public float ToySoundRemaining => Mathf.Max(0f, toyUntil - Time.time);
        public float ToyWinding01 => toy != null && toy.IsHeld ? Mathf.Clamp01(toy.HeldDuration / 0.5f) : 0f;
        public Action<LearnerState> LearnerChanged;
        public Action<bool> WindowChanged;
        public Action ShortcutOpened;
        public Action ToyWound;
        private bool archiveRead, plateRead;
        private float clueUntil;
        private string clueText;

        public void Initialize(StorySceneDirector owner, JourneyVisuals drawing)
        {
            director = owner;
            visuals = drawing;
            player = owner.Player.GetComponent<PlayerController2D>();
            player.SetJumpForce(9.2f);
            hand = owner.Player.GetComponent<HandConnection>();
            director.SetRequiredObjectives(2);
            // Retire the delivery chain, including its invisible socket acceptance rules.
            foreach (var old in FindObjectsByType<HoldTarget>()) old.gameObject.SetActive(false);
            foreach (var old in FindObjectsByType<HoldSocket>()) old.gameObject.SetActive(false);
            // These belonged to the retired card-delivery puzzle and suggest false interactions.
            foreach (var item in FindObjectsByType<Transform>())
                if (item.name == "Pinned Draft" || item.name == "Desk Lamp" || item.name == "Research Notes" || item.name == "Research Desk")
                    item.gameObject.SetActive(false);
            WindActive = JourneyChoices.BeginWorkshop() % 2 == 1;

            crate = Tool("Workshop Crate", "workshop-crate", new Vector3(4f, -2.1f), new Vector2(1.1f, 1.3f), "prop_research_crate", "E · 搬箱子");
            crate.CarryOffset = new Vector3(1.2f, 0.4f);
            crateCollider = Box(crate.transform, new Vector2(1.1f, 1.3f));
            toolSurface = new PhysicsMaterial2D("Workshop smooth tool edges") { friction = 0f, bounciness = 0f };
            crateCollider.sharedMaterial = toolSurface;
            Platform("Archive Shelf", new Vector3(-2f, -0.35f), new Vector2(3.2f, 0.3f));
            plank = Tool("Folding Plank", "workshop-plank", new Vector3(-2f, -0.12f), new Vector2(1.6f, 0.18f), "prop_research_bridge", "E · 拿折叠板");
            plank.Configure("workshop-plank", HoldTargetMode.Carryable, "E · 拿折叠板", 1f, 3f);
            plank.DropFloorY = -2.5f;
            lamp = Tool("Portable Lamp", "workshop-lamp", new Vector3(12.6f, -2.2f), new Vector2(0.55f, 0.7f), "prop_researchlight", "E · 拿灯");
            if (JourneyChoices.TookChildhoodToy)
            {
                toy = Tool("The Toy You Kept", "workshop-toy", new Vector3(-5f, -2.3f), new Vector2(0.5f, 0.6f), "prop_toy", "E · 给熟悉的玩具上弦");
                toy.Released += value =>
                {
                    if (value.HeldDuration < 0.5f) return;
                    toyUntil = Time.time + 12f;
                    ToyWound?.Invoke();
                };
            }
            actor = Tool("Workshop Learner", "workshop-learner", new Vector3(12f, -2.25f), new Vector2(0.45f, 0.8f), "prop_research_model", "E · 牵住   F · 让它试走");
            actor.Configure("workshop-learner", HoldTargetMode.Recipient, "E · 牵住   F · 让它试走", 1.5f, 2.6f);
            report = Tool("Your Working Report", "workshop-report", new Vector3(22f, -2.1f), new Vector2(0.6f, 0.75f), "prop_research_report", "E · 带报告回工作台");
            report.gameObject.SetActive(false);
            var bench = visuals.Prop("Return Workbench", new Vector3(1f, -2.1f), new Vector2(2.4f, 1.1f), JourneyVisuals.Warm, "workshop-desk");
            desk = bench.gameObject.AddComponent<HoldSocket>();
            desk.Configure("workshop-report", "", 1.5f, 1, "");
            desk.Acceptance = target => arrived;
            var shutterFrame = visuals.Prop("Workshop Shutter Frame", new Vector3(9f, -0.7f), new Vector2(3.15f, 4.35f), Color.white, "workshop-shutter_frame");
            KeyBlack(shutterFrame);
            var gate = visuals.Prop("Workshop Shutter Door", new Vector3(9f, -0.75f), new Vector2(2.8f, 3.75f), Color.white, "workshop-shutter_door");
            shutterVisual = gate.transform;
            shutterDoorVisual = gate;
            KeyBlack(shutterDoorVisual);
            shutter = Box(gate.transform, new Vector2(0.75f, 4f));
            shutter.sharedMaterial = toolSurface;
            plateVisual = visuals.Prop("Pressure Plate", new Vector3(6.5f, -2.61f), new Vector2(1.5f, 0.2f), Color.white, "workshop-plate");
            PreparePressurePlateSprites();
            latchLockedVisual = visuals.Prop("Inside Shutter Latch Locked", new Vector3(10.5f, -1.5f), new Vector2(0.3f, 0.72f), Color.white, "workshop-shutter_locked");
            latchUnlockedVisual = visuals.Prop("Inside Shutter Latch Open", new Vector3(10.5f, -1.5f), new Vector2(0.3f, 0.72f), Color.white, "workshop-shutter_unlocked");
            KeyBlack(latchLockedVisual);
            KeyBlack(latchUnlockedVisual);
            latchUnlockedVisual.enabled = false;
            var learnerBase = visuals.Prop("Learner Model Base", new Vector3(12f, -2.67f), new Vector2(0.65f, 0.1f), Color.white, "workshop-plate");
            if (platePressedSprite != null)
            {
                learnerBase.sprite = platePressedSprite;
                FitSprite(learnerBase, new Vector2(0.65f, 0.1f));
                KeyBlack(learnerBase);
            }
            var gapShadow = visuals.Prop("Workshop Gap", new Vector3(16f, -2.72f), new Vector2(1.3f, 0.28f), Color.black, "workshop-gap-shadow");
            gapShadow.sortingOrder = 7;
            visuals.Prop("Draft Window Handle", new Vector3(21f, -1.5f), new Vector2(0.5f, 0.65f), JourneyVisuals.Cool, "workshop-window");
            visuals.Prop("Archive Notebook", new Vector3(-2.7f, 0.3f), new Vector2(0.4f, 0.35f), JourneyVisuals.Warm, "prop_research_book");
            wire = visuals.GameplayLine("Plate to shutter circuit", JourneyVisuals.Warm, 0.045f);
            gap = visuals.GameplayLine("Learner crossing gap", JourneyVisuals.Cool);
            comfort = visuals.GameplayLine("Portable lamp coverage", JourneyVisuals.Warm, 0.025f);
            wind = visuals.GameplayLine("Draft through the window", JourneyVisuals.Cool, 0.045f);
            map = visuals.GameplayLine("Workshop route sketch", JourneyVisuals.Warm, 0.04f);
            toySound = visuals.GameplayLine("Familiar toy sound range", JourneyVisuals.Warm, 0.025f);
            guide = gameObject.AddComponent<ResearchGuide>();
            guide.Initialize(this, hand);
        }

        private HoldTarget Tool(string name, string id, Vector3 position, Vector2 size, string slot, string prompt)
        {
            var renderer = visuals.Prop(name, position, size, JourneyVisuals.Warm, string.IsNullOrEmpty(slot) ? "workshop-report" : slot);
            var target = renderer.gameObject.AddComponent<HoldTarget>();
            target.Configure(id, HoldTargetMode.Carryable, prompt, 1.5f, 3f);
            target.DropFloorY = position.y;
            return target;
        }

        private static BoxCollider2D Box(Transform item, Vector2 size)
        {
            var collider = item.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(size.x / Mathf.Abs(item.lossyScale.x), size.y / Mathf.Abs(item.lossyScale.y));
            return collider;
        }

        private void Platform(string name, Vector3 position, Vector2 size)
        {
            var item = visuals.Prop(name, position, size, JourneyVisuals.Cool, "workshop-platform");
            var collider = Box(item.transform, size);
            collider.usedByEffector = true;
            item.gameObject.AddComponent<PlatformEffector2D>().useOneWay = true;
        }

        private bool At(float x, float radius = 1.3f) => Mathf.Abs(player.transform.position.x - x) < radius;
        private static bool OnFloorAt(HoldTarget item, float x, float radius) => item != null && !item.IsHeld &&
            Mathf.Abs(item.transform.position.x - x) < radius && item.transform.position.y < -1.7f;

        private void Update()
        {
            if (player == null) return;
            if (!string.IsNullOrEmpty(activePrompt)) director.ClearPrompt(activePrompt);
            activePrompt = null;
            if (GameInput.ReconsiderPressed && GateLatched && player.ControlsEnabled && hand.CurrentTarget == null && !ReportReturned)
            {
                var before = learner.Rehearsals;
                learner.Recall();
                if (learner.Rehearsals != before)
                {
                    arrived = false;
                    report.gameObject.SetActive(false);
                    actor.ReopenInteraction();
                    director.SetCompletedObjectives(0);
                    SceneAudio.Instance?.PlayInteract();
                }
            }
            crateCollider.enabled = !crate.IsHeld;
            var pressed = At(6.5f, 0.7f) && player.transform.position.y < -1.8f || OnFloorAt(crate, 6.5f, 0.8f) || OnFloorAt(plank, 6.5f, 0.8f);
            gateGrace = pressed ? 0.12f : Mathf.Max(0f, gateGrace - Time.deltaTime);
            // Never close a collision shape around the player; an inside latch makes the return route permanent.
            if (gateWasOpen && !GateOpen && At(9f, 0.8f)) clearingShutter = true;
            if (!At(9f, 0.8f)) clearingShutter = false;
            shutter.enabled = !GateOpen && !clearingShutter;
            gateWasOpen = GateOpen;
            if (shutterDoorVisual != null) shutterDoorVisual.enabled = !GateOpen;
            if (plateVisual != null && plateRaisedSprite != null)
                plateVisual.sprite = pressed ? platePressedSprite : plateRaisedSprite;
            if (latchLockedVisual != null) latchLockedVisual.enabled = !GateLatched;
            if (latchUnlockedVisual != null) latchUnlockedVisual.enabled = GateLatched;
            if (At(10.5f) && player.transform.position.x > 9.3f && !GateLatched)
            {
                activePrompt = "F · 拉开内侧门闩";
                if (GameInput.UsePressed) { GateLatched = true; SceneAudio.Instance?.PlayDoor(); }
            }
            if (At(21f) && !arrived)
            {
                activePrompt = WindActive ? "F · 关上漏风的窗" : "F · 打开窗";
                if (GameInput.UsePressed) { WindActive = !WindActive; WindowChanged?.Invoke(WindActive); }
            }
            if (At(6.5f) && !plateRead && hand.CurrentTarget == null)
            {
                activePrompt ??= "F · 看地上的压痕";
                if (GameInput.UsePressed) { plateRead = true; ReadClue("压痕比脚印宽，和箱底一样；连线通向门。", 0); }
            }
            if (At(-3f) && player.transform.position.y > -0.5f)
            {
                activePrompt = "F · 翻看旧实验本";
                if (GameInput.UsePressed && !archiveRead) { archiveRead = true; ReadClue("同一个箱子：垫脚、压住机关，也能做落脚点。灯旁画着一扇关上的窗。", 1); }
            }
            if (At(learner.X) && !learner.Running)
            {
                activePrompt = "F · 让它试走   E · 牵着试走";
                if (GameInput.UsePressed) learner.Start();
            }
            if (player.ControlsEnabled && !arrived)
            {
                var support = OnFloorAt(plank, ExpeditionLearner.GapX, 0.8f) ? "plank" : OnFloorAt(crate, ExpeditionLearner.GapX, 0.8f) ? "crate" : "";
                var light = LampWorking && Mathf.Abs(lamp.transform.position.x - learner.X) < 3.6f;
                var familiar = toy != null && Time.time < toyUntil && Mathf.Abs(toy.transform.position.x - learner.X) < 3.5f;
                learner.Tick(Time.deltaTime, support, hand.CurrentTarget == actor, player.transform.position.x, light, familiar);
                actor.transform.position = new Vector3(learner.X, -2.25f);
                if (previous != learner.State) { previous = learner.State; LearnerChanged?.Invoke(previous); }
                if (learner.State == LearnerState.Arrived) Arrive();
            }
            if (arrived && At(23f))
            {
                activePrompt = "E · 拿报告   F · 返回工作台   空手 Q · 召回重试";
                if (GameInput.UsePressed)
                {
                    player.transform.position = new Vector3(-0.4f, -2.1f);
                    player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                    if (hand.CurrentTarget != null) hand.CurrentTarget.transform.position = player.transform.position + hand.CurrentTarget.CarryOffset;
                }
            }
            if (At(1f))
            {
                activePrompt = ReportReturned ? "F · 带着这次的经验去汇报" : hand.CurrentTarget == report ? "松开 E · 留下你的报告" : "让实验小人走到右侧出口";
                if (ReportReturned && GameInput.UsePressed) director.LoadNextScene();
            }
            if (hand.CurrentTarget == toy && toy != null) activePrompt = "按住 E 上弦 · 松开后它会继续发声一阵子";
            if (Time.time < clueUntil) activePrompt = clueText;
            if (activePrompt != null) director.ShowPrompt(activePrompt);
            DrawFeedback();
        }

        private void ReadClue(string text, int index)
        {
            CluesFound++;
            clueText = text;
            clueUntil = Time.time + 5f;
            map.positionCount = 5;
            var center = player.transform.position + Vector3.up * 1.4f;
            map.SetPosition(0, center + Vector3.left);
            map.SetPosition(1, center + Vector3.up * 0.5f);
            map.SetPosition(2, center);
            map.SetPosition(3, center + Vector3.right);
            map.SetPosition(4, center + new Vector3(1, 0.5f));
        }

        private void Arrive()
        {
            arrived = true;
            actor.MarkPlaced(actor.transform.position);
            report.transform.position = new Vector3(22f, -2.1f);
            report.gameObject.SetActive(true);
            director.CompleteObjective();
            JourneyChoices.RememberWorkshop(learner.Support, learner.Crossing, learner.Independent);
            JourneyChoices.Record("research", learner.Independent ? "我留下了帮助，让他自己走过去。" : "我陪他一起走过了黑暗。", false);
            ShortcutOpened?.Invoke();
        }

        private void DrawFeedback()
        {
            var hints = guide != null && guide.HelpVisible;
            wire.enabled = !GateLatched && (At(6.5f, 4f) || hints);
            wire.positionCount = 3;
            wire.SetPosition(0, new Vector3(6.5f, -2.6f));
            wire.SetPosition(1, new Vector3(9f, -2.6f));
            wire.SetPosition(2, new Vector3(9f, 1.2f));
            wire.startColor = wire.endColor = GateOpen ? JourneyVisuals.Warm : JourneyVisuals.Cool;
            gap.positionCount = 4;
            gap.enabled = !arrived && (At(16f, 4f) || hints);
            gap.SetPosition(0, new Vector3(15.35f, -2.6f));
            gap.SetPosition(1, new Vector3(15.35f, -2.95f));
            gap.SetPosition(2, new Vector3(16.65f, -2.95f));
            gap.SetPosition(3, new Vector3(16.65f, -2.6f));
            // Show usable reach at floor level only while placing the tool or asking for help.
            comfort.enabled = !arrived && (hand.CurrentTarget == lamp || hints && At(lamp.transform.position.x, 4f));
            DrawRange(comfort, lamp.transform.position.x, LampWorking ? 3.6f : 0.2f);
            wind.enabled = WindActive && !arrived && (At(21f, 3f) || hints);
            wind.positionCount = 3;
            wind.SetPosition(0, new Vector3(22f, -0.6f));
            wind.SetPosition(1, new Vector3(20f, -0.9f));
            wind.SetPosition(2, new Vector3(18.5f, -0.6f));
            map.enabled = false;
            toySound.enabled = !arrived && toy != null && ToySoundRemaining > 0f && (hand.CurrentTarget == toy || hints);
            if (toySound.enabled) DrawRange(toySound, toy.transform.position.x, 3.5f);
            HideDebugLine(wire);
            HideDebugLine(gap);
            HideDebugLine(wind);
            HideDebugLine(map);
        }

        private static void DrawRange(LineRenderer line, float x, float radius)
        {
            line.positionCount = 4;
            line.SetPosition(0, new Vector3(x - radius, -2.48f));
            line.SetPosition(1, new Vector3(x - radius, -2.62f));
            line.SetPosition(2, new Vector3(x + radius, -2.62f));
            line.SetPosition(3, new Vector3(x + radius, -2.48f));
        }

        private void PreparePressurePlateSprites()
        {
            if (plateVisual == null || plateVisual.sprite == null) return;
            var texture = plateVisual.sprite.texture;
            // The delivered sheet contains raised and pressed states side by side.
            // Current delivered sheet is 1448 x 1086; crop both complete plates at the same baseline.
            var cropScale = new Vector2(texture.width / 1448f, texture.height / 1086f);
            plateRaisedSprite = Sprite.Create(texture, new Rect(40f * cropScale.x, 400f * cropScale.y, 665f * cropScale.x, 165f * cropScale.y), Vector2.one * 0.5f, 100f);
            platePressedSprite = Sprite.Create(texture, new Rect(745f * cropScale.x, 400f * cropScale.y, 665f * cropScale.x, 165f * cropScale.y), Vector2.one * 0.5f, 100f);
            plateRaisedSprite.name = "Workshop Plate Raised";
            platePressedSprite.name = "Workshop Plate Pressed";
            plateVisual.sprite = plateRaisedSprite;
            FitSprite(plateVisual, new Vector2(1.5f, 0.2f));
            KeyBlack(plateVisual);
        }

        private static void FitSprite(SpriteRenderer renderer, Vector2 size)
        {
            if (renderer == null || renderer.sprite == null) return;
            var scale = Mathf.Min(size.x / renderer.sprite.bounds.size.x, size.y / renderer.sprite.bounds.size.y);
            renderer.transform.localScale = Vector3.one * scale;
        }

        private void KeyBlack(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return;
            var texture = renderer.sprite.texture;
            if (!blackKeyMaterials.TryGetValue(texture, out var material))
            {
                var shader = Resources.Load<Shader>("BlackKeySprite");
                if (shader == null) return;
                material = new Material(shader) { mainTexture = texture };
                blackKeyMaterials.Add(texture, material);
            }
            renderer.sharedMaterial = material;
        }

        private static void HideDebugLine(LineRenderer line)
        {
            if (line == null) return;
            line.startWidth = 0f;
            line.endWidth = 0f;
        }

        private void OnDestroy()
        {
            if (toolSurface != null) Destroy(toolSurface);
            if (plateRaisedSprite != null) Destroy(plateRaisedSprite);
            if (platePressedSprite != null) Destroy(platePressedSprite);
            foreach (var material in blackKeyMaterials.Values) Destroy(material);
        }
    }
}
