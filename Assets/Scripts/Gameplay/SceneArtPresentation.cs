using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetGo
{
    // Presentation only: reads existing gameplay state, never changes interactions or physics.
    [DefaultExecutionOrder(100)]
    public sealed class SceneArtPresentation : MonoBehaviour
    {
        private const float GroundY = -2.72f;
        private const float ViewSize = 2.95f;
        private static Sprite radialGlow;
        private static Sprite memoryBlackSprite;

        [SerializeField] private SpriteRenderer vignette;
        [SerializeField] private SpriteRenderer passageBlackout;
        [SerializeField] private Vector2 backdropLimits;

        private Camera view;
        private Collider2D playerBody;
        private HandConnection hand;
        private EmotionalEnvironment environment;
        private SpriteRenderer playerArt;
        private PlayerController2D player;
        private float playerFeetOffset;
        private bool playerFeetOffsetReady;
        private SpriteRenderer carriedReport;
        private ResearchExpedition research;
        private SpriteRenderer researchLearnerVisual;
        private Sprite researchLearnerIdle;
        private Sprite researchLearnerWalk;
        private Sprite researchLearnerFear;
        private Sprite researchCrateSprite;
        private Sprite stageLeftForegroundFold;
        private LearnerState? researchLearnerPose;
        private SpriteRenderer researchShutterSource;
        private SpriteRenderer researchShutterVisual;
        private SpriteRenderer researchLampGlow;
        private SpriteRenderer researchToyGlow;
        private LineRenderer stageBreathFeedback;
        private LineRenderer stageAimFeedback;
        private readonly List<LineRenderer> stageListenerFeedback = new();
        private bool stageFeedbackCached;
        private string visibleAge;
        private readonly List<(SpriteRenderer source, SpriteRenderer visible)> actors = new();
        private readonly List<SpriteRenderer> finalStageGroup = new();
        private readonly List<SpriteRenderer> finalResearchGroup = new();
        private readonly List<SpriteRenderer> finalUnknownGroup = new();
        private readonly Dictionary<SpriteRenderer, float> authoredAlpha = new();

        public void Configure(SpriteRenderer overlay, Vector2 limits, SpriteRenderer blackout = null)
        {
            vignette = overlay;
            backdropLimits = limits;
            passageBlackout = blackout;
        }

        private IEnumerator Start()
        {
            view = GetComponent<Camera>();
            if (view != null) view.orthographicSize = ViewSize;
            hand = FindAnyObjectByType<HandConnection>();
            environment = FindAnyObjectByType<EmotionalEnvironment>();
            player = FindAnyObjectByType<PlayerController2D>();
            if (player != null)
            {
                playerArt = player.CharacterRenderer;
                playerBody = player.GetComponent<Collider2D>();
                if (playerBody != null)
                {
                    playerFeetOffset = playerBody.bounds.min.y - player.transform.position.y;
                    playerFeetOffsetReady = true;
                }
                InstallHandTetherPresentation();
            }
            carriedReport = GameObject.Find("Carried Research Report")?.GetComponentInChildren<SpriteRenderer>();
            if (carriedReport != null) FitWorld(carriedReport, new Vector2(0.42f, 0.5f));
            passageBlackout ??= transform.Find("Passage Blackout")?.GetComponent<SpriteRenderer>();

            yield return null;
            var journeyVisuals = FindAnyObjectByType<JourneyVisuals>();
            journeyVisuals?.SetGuidesVisible(false);
            RestylePrompt();
            NormalizeSceneComposition();
            ReplaceCourageGlow();

            foreach (var renderer in FindObjectsByType<SpriteRenderer>())
            {
                if (renderer.name != "Grounded Visual") continue;
                var source = renderer.transform.parent.GetComponent<SpriteRenderer>();
                FitActor(renderer, ActorHeight(renderer.transform.parent.name));
                actors.Add((source, renderer));
            }

            var bag = GameObject.Find("Bag from home")?.GetComponent<SpriteRenderer>();
            if (bag != null && bag.sprite != null && bag.transform.Find("Bag Visual") == null)
            {
                var visible = new GameObject("Bag Visual", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                visible.sprite = bag.sprite;
                visible.sharedMaterial = bag.sharedMaterial;
                visible.sortingOrder = bag.sortingOrder;
                visible.transform.SetParent(bag.transform, false);
                visible.transform.localPosition = Vector3.zero;
                FitActor(visible, 0.55f);
                visible.transform.position += Vector3.up * (GroundY - visible.bounds.min.y);
                actors.Add((bag, visible));
                bag.enabled = false;
            }

            if (SceneManager.GetActiveScene().name == "06_FinalWalk")
                PrepareFinalMemoryGroups();
        }

        private void NormalizeSceneComposition()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (passageBlackout != null) passageBlackout.enabled = false;
            if (scene == "01_Kindergarten")
            {
                HideRenderers("Warm Light");
                HideRenderers("Classroom Hand Gate");
                HideRenderers("Unfamiliar Shadows");
                // The delivered hall layer was an alternate full-screen composition. Keeping it
                // over the room made walking back replay a transition, so the chapter now uses
                // one continuous world and the normal camera follow from its first frame.
                HideRenderers("Integrated Classroom Entrance");
            }
            else if (scene == "03_Stage")
            {
                SetupStageCurtains();
                var platform = GameObject.Find("Forward Platform")?.transform;
                HideRenderers("Riser Left Support");
                HideRenderers("Riser Right Support");
                BuildSolidRiser(platform);
                var chair = GameObject.Find("Stage Chair")?.GetComponent<SpriteRenderer>();
                if (chair != null)
                {
                    FitWorld(chair, new Vector2(0.8f, 1.15f));
                    chair.transform.position += Vector3.up * (GroundY - chair.bounds.min.y);
                }
                var stageVisual = GameObject.Find("Stage Background")?.transform.Find("Aligned Visual")?.GetComponent<SpriteRenderer>();
                if (stageVisual != null) FitWorld(stageVisual, new Vector2(38f, 12.7f));
                if (GetComponent<StageFeedbackPresentation>() == null)
                    gameObject.AddComponent<StageFeedbackPresentation>();
                CacheStageFeedback();
            }
            else if (scene == "05_Research")
            {
                FitDoor("Finished Report", new Vector2(1.18f, 2.68f));
                var research = GameObject.Find("Research Background")?.transform;
                AlignResearchEntrance(research);
                var hallway = research?.Find("Report Hallway")?.GetComponent<SpriteRenderer>();
                if (hallway != null)
                {
                    FitWorld(hallway, new Vector2(14f, 5.5f));
                    hallway.transform.position = research.position + Vector3.right * 15f;
                }
                NormalizeResearchPresentation();
                AddPassageOverlay(scene);
            }
            else if (scene == "06_FinalWalk")
            {
                if (view != null) view.backgroundColor = Color.black;
                FitDoor("Unknown Door", new Vector2(1.18f, 2.68f));
                foreach (var transform in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                    if (transform.name.StartsWith("Memory Picture ")) transform.gameObject.SetActive(false);
                BuildMemoryBlackPassages();
                AddPassageOverlay(scene);
            }
        }

        private static void AlignResearchEntrance(Transform room)
        {
            var source = room?.Find("Aligned Visual")?.GetComponent<SpriteRenderer>() ?? room?.GetComponent<SpriteRenderer>();
            var extension = room?.Find("Mentor Hallway")?.GetComponent<SpriteRenderer>();
            if (source == null || extension == null || source.sprite == null) return;
            // Reflect the same painted wall across its left edge: floor, light and wall details
            // meet continuously instead of splicing an unrelated office into the entrance.
            var bounds = source.bounds;
            extension.sprite = source.sprite;
            extension.sharedMaterial = source.sharedMaterial;
            extension.color = source.color;
            extension.sortingLayerID = source.sortingLayerID;
            extension.sortingOrder = source.sortingOrder;
            extension.flipX = !source.flipX;
            extension.flipY = source.flipY;
            FitWorldExact(extension, bounds.size);
            var center = new Vector3(bounds.min.x - bounds.extents.x + 0.05f, bounds.center.y, bounds.center.z);
            extension.transform.position += center - extension.bounds.center;
            extension.enabled = true;
        }

        private void InstallHandTetherPresentation()
        {
            if (hand == null || playerArt == null) return;
            var tether = GameObject.Find("Hand Connection Light")?.GetComponent<LineRenderer>();
            if (tether == null) return;
            var presentation = tether.GetComponent<HandTetherPresentation>();
            if (presentation == null) presentation = tether.gameObject.AddComponent<HandTetherPresentation>();
            presentation.Configure(hand, tether, playerArt);
        }

        private void NormalizeResearchPresentation()
        {
            var retiredDraft = GameObject.Find("Doubt Notes");
            if (retiredDraft != null) retiredDraft.SetActive(false);
            // The frame itself already contains a readable latch and status light. The separate
            // full-canvas latch sheets looked like person-sized floating machines in play.
            HideRenderers("Inside Shutter Latch Locked");
            HideRenderers("Inside Shutter Latch Open");

            GroundVisual("Workshop Crate", new Vector2(1.1f, 1.3f));
            SetupResearchCratePresentation();
            GroundVisual("Portable Lamp", new Vector2(0.58f, 0.72f));
            GroundVisual("Workshop Learner", new Vector2(0.56f, 0.9f));
            SetupResearchLearnerPresentation();
            SetupResearchAtmosphereEffects();
            GroundVisual("Workshop Shutter Frame", new Vector2(3.15f, 4.35f));
            GroundVisual("Workshop Shutter Door", new Vector2(2.8f, 3.75f));
            NudgePresentationVisual("Workshop Shutter Frame", -0.23f);
            NudgePresentationVisual("Workshop Shutter Door", -0.18f);
            BuildArchiveShelving();

            var notebook = GameObject.Find("Archive Notebook")?.GetComponent<SpriteRenderer>();
            if (notebook != null && notebook.sprite != null)
                notebook.transform.position += Vector3.up * (-0.26f - notebook.bounds.min.y);

            var plate = GameObject.Find("Pressure Plate")?.GetComponent<SpriteRenderer>();
            if (plate != null)
            {
                FitWorld(plate, new Vector2(1.35f, 0.22f));
                plate.transform.position += Vector3.up * (GroundY - plate.bounds.min.y + 0.01f);
                plate.sortingOrder = 10;
            }

            var frame = GameObject.Find("Workshop Shutter Frame")?.GetComponent<SpriteRenderer>();
            if (frame != null) frame.sortingOrder = 9;
            var door = GameObject.Find("Workshop Shutter Door")?.GetComponent<SpriteRenderer>();
            if (door != null) door.sortingOrder = 8;
            var doorRoot = GameObject.Find("Workshop Shutter Door")?.transform;
            researchShutterSource = doorRoot?.GetComponent<SpriteRenderer>();
            researchShutterVisual = doorRoot?.Find("Presentation Visual")?.GetComponent<SpriteRenderer>();
        }

        private void SetupStageCurtains()
        {
            var left = StageCurtain("Curtain Left");
            var right = StageCurtain("Curtain Right");
            if (left != null)
            {
                left.enabled = true;
                left.color = Color.white;
                left.sortingOrder = -1;
                FitWorld(left, new Vector2(17.5f, 5.8f));
                left.transform.position += Vector3.up * (GroundY - left.bounds.min.y);
            }
            if (right != null)
            {
                right.enabled = true;
                right.color = Color.white;
                right.sortingOrder = -1;
                FitWorld(right, new Vector2(17.5f, 5.8f));
                right.transform.position += Vector3.up * (GroundY - right.bounds.min.y);
            }
            if (left == null || left.sprite == null || GameObject.Find("Stage Left Foreground Fold") != null) return;

            var texture = left.sprite.texture;
            var scaleX = texture.width / 2172f;
            var scaleY = texture.height / 724f;
            // The source has transparent padding below the painted hem. Start the runtime crop at
            // the first consistently opaque row so a character passing behind the fold is covered
            // all the way to the stage floor instead of leaving detached feet below the curtain.
            var rect = new Rect(850f * scaleX, 90f * scaleY, 270f * scaleX, 634f * scaleY);
            rect.width = Mathf.Min(rect.width, texture.width - rect.x);
            rect.height = Mathf.Min(rect.height, texture.height - rect.y);
            stageLeftForegroundFold = Sprite.Create(texture, rect, Vector2.one * 0.5f, 100f,
                0, SpriteMeshType.FullRect);
            stageLeftForegroundFold.name = "stage_left_foreground_fold";
            var fold = new GameObject("Stage Left Foreground Fold", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            fold.sprite = stageLeftForegroundFold;
            fold.sharedMaterial = left.sharedMaterial;
            fold.color = Color.white;
            fold.sortingLayerID = left.sortingLayerID;
            fold.sortingOrder = 20;
            fold.transform.position = new Vector3(-3.45f, 0f, 0f);
            FitWorld(fold, new Vector2(2.35f, 5.8f));
            // Foreground cloth hangs slightly closer to camera than the actors' floor line.
            fold.transform.position += Vector3.up * (GroundY - 0.12f - fold.bounds.min.y);
        }

        private static SpriteRenderer StageCurtain(string rootName)
        {
            var root = GameObject.Find(rootName);
            if (root == null) return null;
            SpriteRenderer best = null;
            var bestArea = 0f;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null) continue;
                var area = renderer.sprite.rect.width * renderer.sprite.rect.height;
                if (area <= bestArea) continue;
                best = renderer;
                bestArea = area;
            }
            return best;
        }

        private void SetupResearchLearnerPresentation()
        {
            research = FindAnyObjectByType<ResearchExpedition>();
            var root = GameObject.Find("Workshop Learner")?.transform;
            researchLearnerVisual = root?.Find("Presentation Visual")?.GetComponent<SpriteRenderer>();
            if (research == null || researchLearnerVisual == null || researchLearnerVisual.sprite == null) return;
            var texture = researchLearnerVisual.sprite.texture;
            if (texture == null || texture.name != "prop_research_model") return;
            var scaleX = texture.width / 1882f;
            var scaleY = texture.height / 836f;
            researchLearnerIdle = ResearchLearnerSprite(texture, "prop_research_model_idle", new Rect(255f * scaleX, 12f * scaleY, 402f * scaleX, 806f * scaleY));
            researchLearnerWalk = ResearchLearnerSprite(texture, "prop_research_model_walk", new Rect(742f * scaleX, 11f * scaleY, 486f * scaleX, 795f * scaleY));
            researchLearnerFear = ResearchLearnerSprite(texture, "prop_research_model_fear", new Rect(1296f * scaleX, 0f, 430f * scaleX, 782f * scaleY));
            var oldBase = GameObject.Find("Learner Model Base");
            if (oldBase != null) oldBase.SetActive(false);
            UpdateResearchLearnerPose(true);
        }

        private void SetupResearchCratePresentation()
        {
            var visual = GameObject.Find("Workshop Crate")?.transform.Find("Presentation Visual")?.GetComponent<SpriteRenderer>();
            if (visual == null || visual.sprite == null) return;
            var texture = visual.sprite.texture;
            if (texture == null || texture.name != "prop_research_crate") return;

            // Trim the square source canvas to the painted wood. The visible box then shares the
            // same 1.1 x 1.3 world footprint as its collider and actually meets the floor.
            var scaleX = texture.width / 1254f;
            var scaleY = texture.height / 1254f;
            var rect = new Rect(144f * scaleX, 175f * scaleY, 962f * scaleX, 919f * scaleY);
            researchCrateSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 100f,
                0, SpriteMeshType.FullRect);
            researchCrateSprite.name = "prop_research_crate_trimmed";
            visual.sprite = researchCrateSprite;
            FitWorldExact(visual, new Vector2(1.1f, 1.3f));
            visual.transform.position += Vector3.up * (GroundY - visual.bounds.min.y);
        }

        private void SetupResearchAtmosphereEffects()
        {
            if (research == null) return;
            researchLampGlow = ResearchGlow("Workshop Lamp Glow", new Vector2(7.2f, 0.82f),
                new Color(1f, 0.7f, 0.28f, 0f));
            if (research.Toy != null)
                researchToyGlow = ResearchGlow("Workshop Toy Glow", new Vector2(1.45f, 0.34f),
                    new Color(1f, 0.65f, 0.3f, 0f));
            UpdateResearchAtmosphereEffects(true);
        }

        private static SpriteRenderer ResearchGlow(string name, Vector2 size, Color color)
        {
            var existing = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
            var renderer = existing ?? new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = EnsureRadialGlow();
            renderer.color = color;
            renderer.sortingOrder = -1;
            FitWorldExact(renderer, size);
            return renderer;
        }

        private static Sprite ResearchLearnerSprite(Texture2D texture, string name, Rect rect)
        {
            rect.x = Mathf.Clamp(rect.x, 0f, texture.width - 1f);
            rect.y = Mathf.Clamp(rect.y, 0f, texture.height - 1f);
            rect.width = Mathf.Clamp(rect.width, 1f, texture.width - rect.x);
            rect.height = Mathf.Clamp(rect.height, 1f, texture.height - rect.y);
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        private void UpdateResearchLearnerPose(bool force = false)
        {
            if (research == null || researchLearnerVisual == null) return;
            var state = research.State;
            if (!force && researchLearnerPose == state) return;
            researchLearnerPose = state;
            researchLearnerVisual.sprite = state == LearnerState.Walking || state == LearnerState.Recalling
                ? researchLearnerWalk
                : state == LearnerState.NeedsComfort || state == LearnerState.Returning
                    ? researchLearnerFear
                    : researchLearnerIdle;
            FitWorld(researchLearnerVisual, new Vector2(0.56f, 0.9f));
            researchLearnerVisual.transform.position += Vector3.up * (GroundY - researchLearnerVisual.bounds.min.y);
        }

        private static void GroundVisual(string rootName, Vector2 size)
        {
            var root = GameObject.Find(rootName)?.transform;
            var source = root?.GetComponent<SpriteRenderer>();
            if (root == null || source == null || source.sprite == null) return;
            var visual = root.Find("Presentation Visual")?.GetComponent<SpriteRenderer>();
            if (visual == null)
            {
                visual = new GameObject("Presentation Visual", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                visual.transform.SetParent(root, false);
            }
            visual.sprite = source.sprite;
            visual.sharedMaterial = source.sharedMaterial;
            visual.color = source.color;
            visual.sortingLayerID = source.sortingLayerID;
            visual.sortingOrder = source.sortingOrder;
            FitWorld(visual, size);
            visual.transform.position += Vector3.up * (GroundY - visual.bounds.min.y);
            source.enabled = false;
        }

        private static void BuildArchiveShelving()
        {
            var shelf = GameObject.Find("Archive Shelf")?.GetComponent<SpriteRenderer>();
            if (shelf == null || shelf.sprite == null || shelf.transform.Find("Left Archive Upright") != null) return;
            ShelfPiece(shelf, "Archive Full Top", new Vector3(-2f, -0.35f, 0f), new Vector2(3.2f, 0.3f), false);
            ShelfPiece(shelf, "Left Archive Upright", new Vector3(-3.25f, -1.46f, 0f), new Vector2(2.45f, 0.2f), true);
            ShelfPiece(shelf, "Right Archive Upright", new Vector3(-0.75f, -1.46f, 0f), new Vector2(2.45f, 0.2f), true);
            ShelfPiece(shelf, "Archive Middle Shelf", new Vector3(-2f, -1.58f, 0f), new Vector2(2.7f, 0.16f), false);
            ShelfPiece(shelf, "Archive Cabinet Base", new Vector3(-2f, -2.61f, 0f), new Vector2(2.85f, 0.18f), false);
            shelf.enabled = false;
        }

        private static void ShelfPiece(SpriteRenderer source, string name, Vector3 position, Vector2 size, bool vertical)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(source.transform, true);
            renderer.sprite = source.sprite;
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.color = new Color(0.62f, 0.56f, 0.48f, 1f);
            renderer.sortingOrder = source.sortingOrder - 1;
            renderer.transform.position = position;
            var parentScale = renderer.transform.parent == null ? Vector3.one : renderer.transform.parent.lossyScale;
            renderer.transform.localScale = new Vector3(
                size.x / renderer.sprite.bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                size.y / renderer.sprite.bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
            if (vertical) renderer.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static void NudgePresentationVisual(string rootName, float y)
        {
            var visual = GameObject.Find(rootName)?.transform.Find("Presentation Visual");
            if (visual != null) visual.position += Vector3.up * y;
        }

        private void AddPassageOverlay(string scene)
        {
            if (player == null) return;
            var overlay = player.GetComponent<PassageBlackoutOverlay>();
            if (overlay == null) overlay = player.gameObject.AddComponent<PassageBlackoutOverlay>();
            overlay.Configure(scene);
        }

        private static void BuildSolidRiser(Transform platform)
        {
            var top = platform?.Find("Delivered Visual")?.GetComponent<SpriteRenderer>();
            if (top == null || top.sprite == null) return;
            for (var i = 1; i <= 4; i++)
            {
                var child = platform.Find("Riser Layer " + i);
                var layer = child == null
                    ? new GameObject("Riser Layer " + i, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>()
                    : child.GetComponent<SpriteRenderer>();
                layer.transform.SetParent(platform, false);
                layer.sprite = top.sprite;
                layer.sharedMaterial = top.sharedMaterial;
                layer.sortingOrder = top.sortingOrder - i;
                FitWorld(layer, new Vector2(top.bounds.size.x, 0.38f));
                layer.transform.position = top.bounds.center + Vector3.down * (0.3f * i);
                var shade = 1f - i * 0.07f;
                layer.color = new Color(shade, shade, shade, 1f);
            }
        }

        private static void ReplaceCourageGlow()
        {
            var renderer = GameObject.Find("Courage Glow")?.GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            renderer.sprite = EnsureRadialGlow();
            FitWorld(renderer, new Vector2(1.65f, 1.65f));
        }

        private static Sprite EnsureRadialGlow()
        {
            if (radialGlow == null)
            {
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                    { name = "Runtime Courage Glow", wrapMode = TextureWrapMode.Clamp };
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var delta = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    var alpha = Mathf.Pow(Mathf.Clamp01(1f - delta.magnitude), 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                texture.Apply();
                radialGlow = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size,
                    0, SpriteMeshType.FullRect);
            }
            return radialGlow;
        }

        private static void FitDoor(string rootName, Vector2 size)
        {
            var root = GameObject.Find(rootName)?.transform;
            if (root == null) return;
            var renderer = root.Find("Aligned Visual")?.GetComponent<SpriteRenderer>() ?? root.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return;
            FitWorld(renderer, size);
            renderer.transform.position += Vector3.up * (GroundY - renderer.bounds.min.y);
        }

        private static void FitWorld(SpriteRenderer renderer, Vector2 size)
        {
            if (renderer == null || renderer.sprite == null) return;
            var factor = Mathf.Min(size.x / renderer.sprite.bounds.size.x, size.y / renderer.sprite.bounds.size.y);
            var parentScale = renderer.transform.parent == null ? Vector3.one : renderer.transform.parent.lossyScale;
            renderer.transform.localScale = new Vector3(factor / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                factor / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
        }

        private static void FitWorldExact(SpriteRenderer renderer, Vector2 size)
        {
            if (renderer == null || renderer.sprite == null) return;
            var parentScale = renderer.transform.parent == null ? Vector3.one : renderer.transform.parent.lossyScale;
            renderer.transform.localScale = new Vector3(
                size.x / renderer.sprite.bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                size.y / renderer.sprite.bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
        }

        private static void HideRenderers(string name)
        {
            var root = GameObject.Find(name);
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
        }

        private void LateUpdate()
        {
            FitPlayerForCurrentAge();
            PlaceCarriedReport();

            foreach (var pair in actors)
            {
                if (pair.source == null || pair.visible == null) continue;
                pair.visible.color = pair.source.color;
                pair.visible.flipX = pair.source.flipX;
                pair.visible.flipY = pair.source.flipY;
            }

            if (view == null) return;
            RevealFinalMemoryGroups();
            RestyleStageFeedback();
            MaintainResearchPresentation();
            UpdateResearchLearnerPose();
            UpdateResearchAtmosphereEffects();
            ClampCamera();
            FitOverlay(vignette);
            UpdateVignette();
        }

        private void MaintainResearchPresentation()
        {
            if (SceneManager.GetActiveScene().name != "05_Research") return;
            if (researchShutterSource == null || researchShutterVisual == null) return;
            researchShutterVisual.enabled = researchShutterSource.enabled;
            researchShutterSource.enabled = false;
        }

        private void UpdateResearchAtmosphereEffects(bool immediate = false)
        {
            if (research == null) return;
            if (researchLampGlow != null && research.Lamp != null)
            {
                SetGlowAlpha(researchLampGlow, research.LampWorking ? 0.2f : 0.025f, immediate);
                researchLampGlow.transform.position = new Vector3(research.Lamp.transform.position.x,
                    GroundY + 0.11f, 0f);
            }
            if (researchToyGlow != null && research.Toy != null)
            {
                var active = research.ToySoundRemaining > 0f || research.ToyWinding01 > 0f;
                var pulse = active ? 0.13f + 0.055f * (0.5f + 0.5f * Mathf.Sin(Time.time * 5.5f)) : 0f;
                SetGlowAlpha(researchToyGlow, pulse, immediate);
                researchToyGlow.transform.position = new Vector3(research.Toy.transform.position.x,
                    GroundY + 0.08f, 0f);
            }
        }

        private static void SetGlowAlpha(SpriteRenderer renderer, float target, bool immediate)
        {
            var color = renderer.color;
            color.a = immediate ? target : Mathf.MoveTowards(color.a, target, Time.deltaTime * 0.45f);
            renderer.color = color;
            renderer.enabled = color.a > 0.002f;
        }

        private void FitPlayerForCurrentAge()
        {
            if (playerArt == null || playerArt.sprite == null || player == null) return;
            var spriteName = playerArt.sprite.name;
            var age = spriteName.StartsWith("char_adult") ? "adult" :
                spriteName.StartsWith("char_teen") ? "teen" : "child";
            if (age != visibleAge)
            {
                visibleAge = age;
                var height = age == "adult" ? 1.78f : age == "teen" ? 1.48f : 1.02f;
                player.SetCharacterScale(Vector3.one * (height / playerArt.sprite.bounds.size.y));
            }
            if (playerFeetOffsetReady)
            {
                var interpolatedFeet = player.transform.position.y + playerFeetOffset;
                playerArt.transform.position += Vector3.up * (interpolatedFeet - playerArt.bounds.min.y);
            }
            if (SceneManager.GetActiveScene().name == "03_Stage" && age == "teen")
                playerArt.transform.position += Vector3.down * 0.08f;
        }

        private void PlaceCarriedReport()
        {
            if (carriedReport == null || playerArt == null || !carriedReport.enabled) return;
            var facing = Mathf.Sign(playerArt.transform.lossyScale.x);
            var position = playerArt.bounds.center;
            position.x += facing * playerArt.bounds.extents.x * 0.72f;
            position.y -= playerArt.bounds.extents.y * 0.04f;
            position.z = carriedReport.transform.position.z;
            carriedReport.transform.position = position;
        }

        private void RestylePrompt()
        {
            var prompt = GameObject.Find("Prompt")?.GetComponent<Text>();
            StoryTypography.ApplyPrompt(prompt);
        }

        private void ClampCamera()
        {
            var halfWidth = view.orthographicSize * view.aspect;
            var cameraPosition = view.transform.position;
            var limits = SceneManager.GetActiveScene().name == "06_FinalWalk"
                ? FinalPassageLayout.WorldLimits
                : backdropLimits;
            cameraPosition.x = Mathf.Clamp(cameraPosition.x, limits.x + halfWidth, limits.y - halfWidth);
            view.transform.position = cameraPosition;
        }

        private void CacheStageFeedback()
        {
            if (SceneManager.GetActiveScene().name != "03_Stage") return;
            stageListenerFeedback.Clear();
            foreach (var line in FindObjectsByType<LineRenderer>())
            {
                if (line.name.StartsWith("Listener response"))
                    stageListenerFeedback.Add(line);
                else if (line.name == "A breath becomes a phrase")
                    stageBreathFeedback = line;
                else if (line.name == "Where the voice is going")
                    stageAimFeedback = line;
            }
            stageFeedbackCached = stageBreathFeedback != null && stageAimFeedback != null && stageListenerFeedback.Count >= 2;
        }

        private void RestyleStageFeedback()
        {
            if (SceneManager.GetActiveScene().name != "03_Stage") return;
            if (!stageFeedbackCached) CacheStageFeedback();
            foreach (var listener in stageListenerFeedback)
                if (listener != null) listener.enabled = false;
            if (stageBreathFeedback != null && stageBreathFeedback.positionCount > 0)
                RestyleRing(stageBreathFeedback, 0.68f, 0.12f, GroundY + 0.07f,
                    JourneyVisuals.Warm, 0.55f, 0.024f);
            if (stageAimFeedback != null && stageAimFeedback.positionCount > 0)
                RestyleLine(stageAimFeedback, JourneyVisuals.Warm, 0.38f, 0.022f);
        }

        private static void RestyleRing(LineRenderer line, float xScale, float yScale, float targetY,
            Color color, float alpha, float width)
        {
            var sourceCenter = LineCenter(line);
            var center = sourceCenter;
            if (!float.IsNaN(targetY)) center.y = targetY;
            for (var i = 0; i < line.positionCount; i++)
            {
                var delta = line.GetPosition(i) - sourceCenter;
                line.SetPosition(i, center + new Vector3(delta.x * xScale, delta.y * yScale, delta.z));
            }
            RestyleLine(line, color, alpha, width);
        }

        private static Vector3 LineCenter(LineRenderer line)
        {
            var center = Vector3.zero;
            for (var i = 0; i < line.positionCount; i++) center += line.GetPosition(i);
            return center / Mathf.Max(1, line.positionCount);
        }

        private static void RestyleLine(LineRenderer line, Color color, float alpha, float width)
        {
            color.a = alpha;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
        }

        private void PrepareFinalMemoryGroups()
        {
            CaptureEnabledRenderers(finalStageGroup, "Memory Spotlight",
                "Remembered Steady Route", "Remembered Forward Route");
            CaptureEnabledRenderers(finalResearchGroup, "Remembered Question",
                "Remembered Photo Evidence", "Remembered Data Evidence", "Remembered Conclusion");
            // The farewell owns the next person's animation and disappearance at the doorway.
            CaptureEnabledRenderers(finalUnknownGroup, "Unknown Door");
            RememberAuthoredAlpha(finalStageGroup);
            RememberAuthoredAlpha(finalResearchGroup);
            RememberAuthoredAlpha(finalUnknownGroup);
            SetAuthoredOpacity(finalStageGroup, 0f);
            SetAuthoredOpacity(finalResearchGroup, 0f);
            SetAuthoredOpacity(finalUnknownGroup, 0f);
        }

        private static void CaptureEnabledRenderers(List<SpriteRenderer> destination, params string[] roots)
        {
            foreach (var rootName in roots)
            {
                var root = GameObject.Find(rootName);
                if (root == null) continue;
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.enabled) destination.Add(renderer);
            }
        }

        private void RevealFinalMemoryGroups()
        {
            if (playerBody == null || finalStageGroup.Count == 0) return;
            var x = playerBody.bounds.center.x;
            var centers = FinalPassageLayout.Centers;
            SetAuthoredOpacity(finalStageGroup, x >= centers[0] && x < centers[1] ? 1f : 0f);
            SetAuthoredOpacity(finalResearchGroup, x >= centers[1] && x < centers[2] ? 1f : 0f);
            SetAuthoredOpacity(finalUnknownGroup, x >= centers[2] ? 1f : 0f);
        }

        private void BuildMemoryBlackPassages()
        {
            if (view == null || GameObject.Find("Memory Black Passage 1") != null) return;
            memoryBlackSprite ??= Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
                Vector2.one * 0.5f, 1f, 0, SpriteMeshType.FullRect);
            memoryBlackSprite.name = "memory_black_passage";
            var width = 2f * view.orthographicSize * view.aspect + 2.8f;
            var height = 2f * view.orthographicSize + 2f;
            var centers = FinalPassageLayout.Centers;
            for (var i = 0; i < centers.Length; i++)
            {
                var passage = new GameObject("Memory Black Passage " + (i + 1), typeof(SpriteRenderer));
                var renderer = passage.GetComponent<SpriteRenderer>();
                renderer.sprite = memoryBlackSprite;
                renderer.color = Color.black;
                renderer.sortingOrder = 30000;
                passage.transform.position = new Vector3(centers[i], view.transform.position.y, 0f);
                passage.transform.localScale = new Vector3(width, height, 1f);
            }
        }

        private void RememberAuthoredAlpha(SpriteRenderer renderer)
        {
            if (renderer != null && !authoredAlpha.ContainsKey(renderer)) authoredAlpha.Add(renderer, renderer.color.a);
        }

        private void RememberAuthoredAlpha(List<SpriteRenderer> renderers)
        {
            foreach (var renderer in renderers) RememberAuthoredAlpha(renderer);
        }

        private void SetAuthoredOpacity(List<SpriteRenderer> renderers, float opacity)
        {
            foreach (var renderer in renderers) SetAuthoredOpacity(renderer, opacity);
        }

        private void SetAuthoredOpacity(SpriteRenderer renderer, float opacity)
        {
            if (renderer == null) return;
            RememberAuthoredAlpha(renderer);
            var alpha = authoredAlpha[renderer] * Mathf.Clamp01(opacity);
            var color = renderer.color;
            color.a = alpha;
            renderer.color = color;
            renderer.enabled = alpha > 0.002f;
        }

        private void FitOverlay(SpriteRenderer overlay)
        {
            if (overlay == null || overlay.sprite == null) return;
            var size = overlay.sprite.bounds.size;
            overlay.transform.localScale = new Vector3(2f * view.orthographicSize * view.aspect / size.x,
                2f * view.orthographicSize / size.y, 1f);
        }

        private void UpdateVignette()
        {
            if (vignette == null) return;
            var completed = StorySceneDirector.Instance != null && StorySceneDirector.Instance.ObjectivesComplete;
            var tension = hand == null || environment == null || completed ? 0f : 1f - hand.Stability01;
            var color = vignette.color;
            color.a = Mathf.Lerp(color.a, 0.2f * tension, Time.deltaTime * 3f);
            vignette.color = color;
        }

        private void OnDestroy()
        {
            if (researchLearnerIdle != null) Destroy(researchLearnerIdle);
            if (researchLearnerWalk != null) Destroy(researchLearnerWalk);
            if (researchLearnerFear != null) Destroy(researchLearnerFear);
            if (researchCrateSprite != null) Destroy(researchCrateSprite);
            if (stageLeftForegroundFold != null) Destroy(stageLeftForegroundFold);
        }

        private static float ActorHeight(string actorName)
        {
            if (actorName.Contains("Crying")) return 0.9f;
            if (actorName.Contains("Young Presenter")) return 1.48f;
            if (actorName.Contains("Parent")) return 1.68f;
            if (actorName.Contains("Mentor")) return 1.78f;
            if (actorName.Contains("Teacher")) return 1.62f;
            return 1.55f;
        }

        private static void FitActor(SpriteRenderer renderer, float worldHeight)
        {
            if (renderer == null || renderer.sprite == null) return;
            var parentScale = renderer.transform.parent == null ? Vector3.one : renderer.transform.parent.lossyScale;
            var factor = worldHeight / renderer.sprite.bounds.size.y;
            renderer.transform.localScale = new Vector3(factor / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                factor / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
            renderer.transform.position += Vector3.up * (GroundY - renderer.bounds.min.y);
        }
    }

    // Kept on the player so the transition remains active while cameras and age visuals swap.
    public sealed class PassageBlackoutOverlay : MonoBehaviour
    {
        private string sceneName;
        private Rigidbody2D body;
        public float CurrentAlpha { get; private set; }

        public void Configure(string value)
        {
            sceneName = value;
            body = GetComponent<Rigidbody2D>();
        }

        private void OnGUI()
        {
            var alpha = 0f;
            CurrentAlpha = alpha;
            if (alpha <= 0.001f) return;
            var previousColor = GUI.color;
            var previousDepth = GUI.depth;
            GUI.depth = -2000;
            GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }


    }
}
