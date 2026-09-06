using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetGo
{
    /// <summary>Freezes the running world while retaining connections and deferring input changes until resume.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PauseMenuController : MonoBehaviour
    {
        private StorySceneDirector director;
        private JourneyOverlay menu;
        private JourneyArtPalette palette;
        private RectTransform composition;
        private float previousTimeScale = 1f;
        private bool previousAudioPause;
        public bool IsPaused { get; private set; }
        public bool Visible => menu != null && menu.gameObject.activeSelf;
        public Button ContinueButton { get; private set; }
        public Button MainMenuButton { get; private set; }
        public Button QuitButton { get; private set; }

        public void Initialize(StorySceneDirector sceneDirector) => director = sceneDirector;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPauseState()
        {
            GameInput.ClearPause();
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (IsPaused)
            {
                GameInput.ObservePauseInput();
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ResumeGame();
                return;
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) PauseGame();
        }

        public bool PauseGame()
        {
            if (IsPaused || director == null || director.IsTransitioning || director.EndingVisible) return false;
            var title = director.GetComponent<MainMenuController>();
            if (title != null && title.Visible) return false;
            GameInput.BeginPause();
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            IsPaused = true;
            if (menu == null) BuildMenu();
            menu.gameObject.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(ContinueButton.gameObject);
            return true;
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;
            RestoreWorld(true);
        }

        public void ReturnToMainMenu()
        {
            if (!IsPaused) return;
            RestoreWorld(false);
            JourneyChoices.Reset();
            SceneManager.LoadScene("00_Prologue");
        }

        public void QuitGame()
        {
            if (!IsPaused) return;
            RestoreWorld(false);
            Application.Quit();
        }

        private void RestoreWorld(bool continuing)
        {
            if (continuing) GameInput.EndPause();
            else GameInput.ClearPause();
            Time.timeScale = continuing ? previousTimeScale : 1f;
            AudioListener.pause = continuing && previousAudioPause;
            IsPaused = false;
            if (menu != null) menu.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void BuildMenu()
        {
            menu = JourneyOverlay.Create("Pause Menu");
            palette = Resources.Load<JourneyArtPalette>("JourneyArtPalette");
            menu.GetComponent<Canvas>().sortingOrder = 32750;
            menu.gameObject.AddComponent<GraphicRaycaster>();
            var shade = menu.Picture("Pause Backdrop", Vector2.zero, Vector2.zero, null,
                Color.black);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = Vector2.one;
            shade.rectTransform.sizeDelta = Vector2.zero;
            shade.raycastTarget = true;
            composition = menu.Rect("Pause Composition", Vector2.zero, new Vector2(1080f, 720f));
            composition.localScale = Vector3.one * 0.85f;
            // The delivered 3:2 composition is the menu. Fit its complete 1536 x 1024 frame into
            // one composition so artwork, input targets and secondary UI always scale together.
            var artwork = menu.Picture("Delivered Pause Menu", Vector2.zero, new Vector2(1080f, 720f),
                Art("pause_background"), new Color(0.63f, 0.61f, 0.57f));
            Attach(artwork.rectTransform);
            artwork.preserveAspect = true;
            ContinueButton = MakeArtworkHotspot("Continue Game", new Vector2(0f, -35f),
                new Vector2(580f, 94f), ResumeGame);
            QuitButton = MakeArtworkHotspot("Quit Game", new Vector2(0f, -152f),
                new Vector2(580f, 94f), QuitGame);
            MainMenuButton = MakeSecondaryButton("Return To Title", "返回主菜单", new Vector2(-405f, -315f),
                ReturnToMainMenu);
            var sceneControls = menu.Label("Current Scene Controls", new Vector2(140f, -315f), new Vector2(720f, 30f), 17);
            Attach(sceneControls.rectTransform);
            sceneControls.text = ControlsForScene(SceneManager.GetActiveScene().name);
            sceneControls.color = new Color(0.82f, 0.75f, 0.63f, 0.82f);
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("Pause Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SetNavigation(ContinueButton, QuitButton, MainMenuButton);
            SetNavigation(MainMenuButton, ContinueButton, QuitButton);
            SetNavigation(QuitButton, MainMenuButton, ContinueButton);
        }

        private static string ControlsForScene(string scene)
        {
            return scene switch
            {
                "02_Interlude_Firsts" => "F 按铃　A D 行走　按住 E 举手",
                "03_Stage" => "按住 E 蓄气　A D 选择听众　松开 E 发声　F 接唱",
                "04_Interlude_Growing" => "A D 选纸　按住 E + A D 移动　Space 折叠　F 试走",
                "05_Research" => "A D 移动　Space 跳跃　E 拿物 / 牵手　F 互动　T 示范　Q 召回",
                "06_FinalWalk" => "A D 移动　按住 E 牵手 / 稳定自己",
                _ => "A D 移动　Space 跳跃　按住 E 牵手 / 拿物"
            };
        }

        private Sprite Art(string id) => palette != null ? palette.Find(id) : null;

        private void Attach(RectTransform rect)
        {
            if (rect != null && composition != null) rect.SetParent(composition, false);
        }

        private Button MakeArtworkHotspot(string name, Vector2 position, Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            var surface = menu.Picture(name, position, size, null, Color.clear);
            Attach(surface.rectTransform);
            surface.raycastTarget = true;
            var button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            var colors = button.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = colors.selectedColor = new Color(1f, 0.72f, 0.28f, 0.14f);
            colors.pressedColor = new Color(1f, 0.78f, 0.35f, 0.24f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener(action);
            return button;
        }

        private Button MakeSecondaryButton(string name, string caption, Vector2 position,
            UnityEngine.Events.UnityAction action)
        {
            var surface = menu.Picture(name, position, new Vector2(190f, 36f), null,
                new Color(0.09f, 0.052f, 0.025f, 0.78f));
            Attach(surface.rectTransform);
            surface.raycastTarget = true;
            var button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            var colors = button.colors;
            colors.normalColor = new Color(0.22f, 0.12f, 0.045f, 0.82f);
            colors.highlightedColor = colors.selectedColor = new Color(0.52f, 0.31f, 0.09f, 0.96f);
            colors.pressedColor = new Color(0.68f, 0.42f, 0.12f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);
            var label = menu.Label(name + " Label", position, new Vector2(176f, 30f), 16);
            Attach(label.rectTransform);
            label.text = caption;
            label.color = new Color(0.98f, 0.86f, 0.62f);
            return button;
        }

        private static void SetNavigation(Button button, Button up, Button down)
        {
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            button.navigation = navigation;
        }

        private void OnDisable()
        {
            if (IsPaused) RestoreWorld(false);
        }

        private void OnDestroy()
        {
            if (IsPaused) RestoreWorld(false);
            if (menu != null) Destroy(menu.gameObject);
        }
    }
}
