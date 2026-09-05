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

        [SerializeField] private SpriteRenderer vignette;
        [SerializeField] private SpriteRenderer passageBlackout;
        [SerializeField] private Vector2 backdropLimits;

        private Camera view;
        private Collider2D playerBody;
        private HandConnection hand;
        private EmotionalEnvironment environment;
        private SpriteRenderer playerArt;
        private PlayerController2D player;
        private SpriteRenderer carriedReport;
        private SpriteRenderer kindergartenEntrance;
        private SpriteRenderer kindergartenCubby;
        private string visibleAge;
        private readonly List<(SpriteRenderer source, SpriteRenderer visible)> actors = new();
        private readonly List<SpriteRenderer> kindergartenRevealProps = new();
        private readonly List<SpriteRenderer> finalStageGroup = new();
        private readonly List<SpriteRenderer> finalResearchGroup = new();
        private readonly List<SpriteRenderer> finalUnknownGroup = new();
        private bool finalStageRevealed;
        private bool finalResearchRevealed;
        private bool finalUnknownRevealed;

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

            if (kindergartenEntrance != null)
                CaptureKindergartenRevealProps();
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
                kindergartenEntrance = GameObject.Find("Integrated Classroom Entrance")?.GetComponent<SpriteRenderer>();
                kindergartenCubby = GameObject.Find("Cubby")?.GetComponent<SpriteRenderer>();
                AddPassageOverlay(scene);
            }
            else if (scene == "03_Stage")
            {
                HideRenderers("Curtain Left");
                HideRenderers("Curtain Right");
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
            }
            else if (scene == "05_Research")
            {
                FitDoor("Finished Report", new Vector2(1.18f, 2.68f));
                var research = GameObject.Find("Research Background")?.transform;
                var hallway = research?.Find("Report Hallway")?.GetComponent<SpriteRenderer>();
                if (hallway != null)
                {
                    FitWorld(hallway, new Vector2(14f, 5.5f));
                    hallway.transform.position = research.position + Vector3.right * 15f;
                }
                AddPassageOverlay(scene);
            }
            else if (scene == "06_FinalWalk")
            {
                if (view != null) view.backgroundColor = Color.black;
                FitDoor("Unknown Door", new Vector2(1.18f, 2.68f));
                foreach (var transform in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                    if (transform.name.StartsWith("Memory Picture ")) transform.gameObject.SetActive(false);
                AddPassageOverlay(scene);
            }
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
            renderer.sprite = radialGlow;
            FitWorld(renderer, new Vector2(1.65f, 1.65f));
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
            ComposeKindergartenEntrance();
            RevealFinalMemoryGroups();
            ClampCamera();
            FitOverlay(vignette);
            UpdateVignette();
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
            if (playerBody != null)
                playerArt.transform.position += Vector3.up * (playerBody.bounds.min.y - playerArt.bounds.min.y);
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
            if (prompt == null) return;
            prompt.fontSize = 20;
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            prompt.verticalOverflow = VerticalWrapMode.Truncate;
            var rect = prompt.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(760f, 54f);
        }

        private void ClampCamera()
        {
            var halfWidth = view.orthographicSize * view.aspect;
            var cameraPosition = view.transform.position;
            cameraPosition.x = Mathf.Clamp(cameraPosition.x, backdropLimits.x + halfWidth, backdropLimits.y - halfWidth);
            view.transform.position = cameraPosition;
        }

        private void ComposeKindergartenEntrance()
        {
            if (kindergartenEntrance == null || playerBody == null) return;
            CaptureKindergartenRevealProps();
            var beforeThreshold = playerBody.bounds.center.x < -1.8f;
            kindergartenEntrance.enabled = beforeThreshold;
            if (kindergartenCubby != null) kindergartenCubby.enabled = !beforeThreshold;
            foreach (var renderer in kindergartenRevealProps)
                if (renderer != null) renderer.enabled = !beforeThreshold;
            if (!beforeThreshold) return;
            var cameraPosition = view.transform.position;
            cameraPosition.x = -5.15f;
            view.transform.position = cameraPosition;
        }

        private void CaptureKindergartenRevealProps()
        {
            if (kindergartenRevealProps.Count > 0) return;
            var bagRoot = GameObject.Find("Bag from home");
            if (bagRoot == null) return;
            foreach (var renderer in bagRoot.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.enabled) kindergartenRevealProps.Add(renderer);
        }

        private void PrepareFinalMemoryGroups()
        {
            CaptureEnabledRenderers(finalStageGroup, "Stage Memory", "Memory Spotlight",
                "Remembered Steady Route", "Remembered Forward Route");
            CaptureEnabledRenderers(finalResearchGroup, "Research Memory", "Remembered Question",
                "Remembered Photo Evidence", "Remembered Data Evidence", "Remembered Conclusion");
            CaptureEnabledRenderers(finalUnknownGroup, "Unknown Wall", "Young Presenter", "Unknown Door");
            SetVisible(finalStageGroup, false);
            SetVisible(finalResearchGroup, false);
            SetVisible(finalUnknownGroup, false);
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
            if (!finalStageRevealed && x >= 6f)
            {
                finalStageRevealed = true;
                SetVisible(finalStageGroup, true);
            }
            if (!finalResearchRevealed && x >= 20f)
            {
                finalResearchRevealed = true;
                SetVisible(finalResearchGroup, true);
            }
            if (!finalUnknownRevealed && x >= 33f)
            {
                finalUnknownRevealed = true;
                SetVisible(finalUnknownGroup, true);
            }
        }

        private static void SetVisible(List<SpriteRenderer> renderers, bool visible)
        {
            foreach (var renderer in renderers)
                if (renderer != null) renderer.enabled = visible;
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

        public void Configure(string value)
        {
            sceneName = value;
            body = GetComponent<Rigidbody2D>();
        }

        private void OnGUI()
        {
            var x = body != null ? body.position.x : transform.position.x;
            var alpha = sceneName == "01_Kindergarten" ? Alpha(x, -1.8f) :
                sceneName == "05_Research" ? Alpha(x, 20f) :
                sceneName == "06_FinalWalk"
                    ? Mathf.Max(Alpha(x, 6f), Alpha(x, 20f), Alpha(x, 33f))
                    : 0f;
            if (alpha <= 0.001f) return;
            var previousColor = GUI.color;
            var previousDepth = GUI.depth;
            GUI.depth = -2000;
            GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        private static float Alpha(float x, float center)
        {
            return 1f - Mathf.SmoothStep(1f, 2f, Mathf.Abs(x - center));
        }
    }
}
