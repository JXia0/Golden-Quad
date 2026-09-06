using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LetGo
{
    /// <summary>The delivered title image is the entry to the existing playable prologue.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private JourneyOverlay menu;
        private CanvasGroup visibility;
        private PlayerController2D player;
        private bool inputReleased;
        private float shownAt;
        public bool HasStarted { get; private set; }
        public bool Visible => menu != null;
        public Button StartButton { get; private set; }
        public Image Background { get; private set; }

        public void Initialize(StorySceneDirector director)
        {
            player = director.Player == null ? null : director.Player.GetComponent<PlayerController2D>();
            if (player != null) player.ControlsEnabled = false;
            var palette = Resources.Load<JourneyArtPalette>("JourneyArtPalette");
            menu = JourneyOverlay.Create("Title Screen");
            menu.GetComponent<Canvas>().sortingOrder = 110;
            visibility = menu.gameObject.AddComponent<CanvasGroup>();
            menu.gameObject.AddComponent<GraphicRaycaster>();
            var backing = menu.Picture("Title Letterbox", Vector2.zero, Vector2.zero, null, Color.black);
            backing.rectTransform.anchorMin = Vector2.zero;
            backing.rectTransform.anchorMax = Vector2.one;
            backing.rectTransform.sizeDelta = Vector2.zero;
            Background = menu.Picture("Delivered Title Art", Vector2.zero, new Vector2(1280f, 720f),
                palette == null ? null : palette.Find("menu_background"), Color.white);
            var startImage = menu.Picture("Start Game", new Vector2(338f, -174f), new Vector2(300f, 120f),
                palette == null ? null : palette.Find("menu_start"), Color.white);
            startImage.raycastTarget = true;
            StartButton = startImage.gameObject.AddComponent<Button>();
            StartButton.targetGraphic = startImage;
            var colors = StartButton.colors;
            colors.normalColor = new Color(0.89f, 0.89f, 0.89f);
            colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            colors.fadeDuration = 0.12f;
            StartButton.colors = colors;
            StartButton.onClick.AddListener(BeginGame);
            var help = menu.Label("Start Keyboard Hint", new Vector2(338f, -228f), new Vector2(350f, 30f), 17);
            help.text = "点击开始 · Enter / Space";
            help.color = new Color(0.92f, 0.85f, 0.68f);
            if (startImage.sprite == null)
            {
                startImage.color = new Color(0.16f, 0.12f, 0.065f);
                var label = menu.Label("Start Fallback", new Vector2(338f, -174f), new Vector2(260f, 48f), 30);
                label.text = "开始";
            }
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("Menu Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            shownAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (HasStarted || menu == null) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!keyboard.enterKey.isPressed && !keyboard.spaceKey.isPressed) inputReleased = true;
            // Enter held over from the previous ending must not also dismiss the menu.
            if (inputReleased && Time.unscaledTime - shownAt > 0.3f &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) BeginGame();
        }

        public void BeginGame()
        {
            if (HasStarted || Time.unscaledTime - shownAt < 0.3f) return;
            HasStarted = true;
            StartButton.interactable = false;
            visibility.interactable = false;
            visibility.blocksRaycasts = false;
            SceneAudio.Instance?.PlayInteract();
            StartCoroutine(RevealPrologue());
        }

        private IEnumerator RevealPrologue()
        {
            const float seconds = 0.65f;
            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                visibility.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / seconds);
                yield return null;
            }
            Destroy(menu.gameObject);
            menu = null;
            if (player != null) player.ControlsEnabled = true;
        }

        private void OnDestroy()
        {
            if (menu != null) Destroy(menu.gameObject);
        }
    }
}
