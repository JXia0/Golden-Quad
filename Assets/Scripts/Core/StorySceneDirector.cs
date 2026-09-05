using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetGo
{
    public sealed class StorySceneDirector : MonoBehaviour
    {
        public static StorySceneDirector Instance { get; private set; }

        [Header("Scene")]
        [SerializeField] private string nextScene;
        [SerializeField] private Transform player;
        [SerializeField] private Transform initialCheckpoint;
        [SerializeField] private int requiredObjectives;

        [Header("UI")]
        [SerializeField] private Text narrationText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Image fadeImage;
        [SerializeField] private Text endingTitle;

        private Transform checkpoint;
        private int completedObjectives;
        private Coroutine narrationRoutine;
        private bool transitioning;
        public bool EndingVisible { get; private set; }
        public bool ReplayAvailable { get; private set; }
        public bool NarrationEnabled { get; set; } = true;
        public Transform Player => player;

        public bool ObjectivesComplete => completedObjectives >= requiredObjectives;
        public int CompletedObjectives => completedObjectives;
        public int RequiredObjectives => requiredObjectives;
        public void SetRequiredObjectives(int count) { requiredObjectives = count; UpdateObjectiveText(); }
        public void SetCompletedObjectives(int count)
        {
            completedObjectives = Mathf.Clamp(count, 0, requiredObjectives);
            UpdateObjectiveText();
        }

        private void Awake()
        {
            Instance = this;
            checkpoint = initialCheckpoint;
            if (endingTitle != null) endingTitle.gameObject.SetActive(false);
            UpdateObjectiveText();
        }

        private void Start()
        {
            if (gameObject.scene.name == "00_Prologue")
                foreach (var label in FindObjectsByType<TextMesh>())
                    if (label.name.StartsWith("Label -")) label.gameObject.SetActive(false);
            StoryTypography.Apply(narrationText, 22);
            if (narrationText != null)
            {
                narrationText.rectTransform.anchorMin = narrationText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                narrationText.rectTransform.anchoredPosition = new Vector2(0f, 234f);
                narrationText.rectTransform.sizeDelta = new Vector2(960f, 104f);
            }
            // Keep restored transitions authoritative even in older, manually laid out scenes.
            if (gameObject.scene.name == "01_Kindergarten") nextScene = "02_Interlude_Firsts";
            if (gameObject.scene.name == "03_Stage") nextScene = "04_Interlude_Growing";
            EmotionalJourney.Install(this);
            if (fadeImage != null) StartCoroutine(FadeFromBlack());
        }

        public void Configure(string next, Transform playerTransform, Transform checkpointTransform, int objectiveCount,
            Text narration, Text prompt, Text objective, Image fade, Text title)
        {
            nextScene = next;
            player = playerTransform;
            initialCheckpoint = checkpointTransform;
            checkpoint = checkpointTransform;
            requiredObjectives = objectiveCount;
            narrationText = narration;
            promptText = prompt;
            objectiveText = objective;
            fadeImage = fade;
            endingTitle = title;
            UpdateObjectiveText();
        }

        public void ShowPrompt(string text)
        {
            if (transitioning) return;
            if (promptText != null) promptText.text = text;
        }

        public void ClearPrompt(string expected = null)
        {
            if (promptText == null || string.IsNullOrEmpty(expected)) return;
            if (promptText.text == expected) promptText.text = string.Empty;
        }

        public void Say(string text, float duration = 3.5f)
        {
            if (!NarrationEnabled) return;
            if (narrationRoutine != null) StopCoroutine(narrationRoutine);
            narrationRoutine = StartCoroutine(SayRoutine(text, duration));
        }

        public void CompleteObjective(string completionLine = null)
        {
            completedObjectives = Mathf.Min(requiredObjectives, completedObjectives + 1);
            SceneAudio.Instance?.PlayObjective();
            if (player != null) player.GetComponent<CourageSystem>()?.RestoreFull();
            UpdateObjectiveText();
            if (!string.IsNullOrWhiteSpace(completionLine)) Say(completionLine);
        }

        public void SetCheckpoint(Transform value)
        {
            checkpoint = value;
            var courage = player != null ? player.GetComponent<CourageSystem>() : null;
            courage?.RestoreFull();
        }

        public void RespawnPlayer()
        {
            if (player == null || checkpoint == null) return;
            player.GetComponent<HandConnection>()?.CancelConnection();
            player.position = checkpoint.position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            player.GetComponent<CourageSystem>()?.RestoreFull();
        }

        public void LoadNextScene()
        {
            if (!transitioning && !string.IsNullOrWhiteSpace(nextScene)) StartCoroutine(LoadNextRoutine());
        }

        public void ShowOpenEnding()
        {
            if (!transitioning) StartCoroutine(OpenEndingRoutine());
        }

        private IEnumerator SayRoutine(string text, float duration)
        {
            if (narrationText == null) yield break;
            narrationText.text = text;
            var color = narrationText.color;
            color.a = 1f;
            narrationText.color = color;
            yield return new WaitForSeconds(duration);
            narrationText.text = string.Empty;
        }

        private IEnumerator FadeFromBlack()
        {
            var color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;
            for (var t = 0f; t < 1f; t += Time.deltaTime)
            {
                color.a = 1f - t;
                fadeImage.color = color;
                yield return null;
            }
            color.a = 0f;
            fadeImage.color = color;
            fadeImage.raycastTarget = false;
        }

        private IEnumerator FadeToBlack(float seconds)
        {
            if (fadeImage == null) yield break;
            fadeImage.raycastTarget = true;
            var color = fadeImage.color;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                color.a = Mathf.Clamp01(t / seconds);
                fadeImage.color = color;
                yield return null;
            }
            color.a = 1f;
            fadeImage.color = color;
        }

        private IEnumerator LoadNextRoutine()
        {
            transitioning = true;
            if (player != null) player.GetComponent<PlayerController2D>().ControlsEnabled = false;
            yield return FadeToBlack(0.8f);
            SceneManager.LoadScene(nextScene);
        }

        private IEnumerator OpenEndingRoutine()
        {
            transitioning = true;
            if (player != null)
            {
                player.GetComponent<PlayerController2D>().ControlsEnabled = false;
                player.GetComponent<HandConnection>()?.CancelConnection();
            }
            if (narrationRoutine != null) StopCoroutine(narrationRoutine);
            if (promptText != null) promptText.text = string.Empty;
            if (narrationText != null) narrationText.text = string.Empty;
            if (endingTitle != null) endingTitle.gameObject.SetActive(false);
            SceneAudio.Instance?.PlayFinal();
            yield return new WaitForSeconds(0.8f);
            yield return FadeToBlack(1.6f);
            yield return new WaitForSeconds(0.65f);
            var closing = JourneyOverlay.Create("Closing Credits");
            closing.GetComponent<Canvas>().sortingOrder = 32760;
            var title = closing.Label("Closing Title", new Vector2(0f, 28f), new Vector2(800f, 86f), 40);
            StoryTypography.ApplyTitle(title, 40);
            title.text = "放开我的手";
            var thanks = closing.Label("Closing Thanks", new Vector2(0f, -52f), new Vector2(760f, 42f), 19);
            thanks.text = "谢谢你，陪我们走到这里。";
            var replay = closing.Label("Closing Replay", new Vector2(0f, -260f), new Vector2(480f, 36f), 16);
            replay.text = "Enter　再走一次";
            replay.gameObject.SetActive(false);
            title.color = new Color(0.94f, 0.91f, 0.85f, 0f);
            thanks.color = new Color(0.7f, 0.7f, 0.69f, 0f);
            replay.color = new Color(0.6f, 0.62f, 0.62f, 0f);
            EndingVisible = true;
            for (var t = 0f; t < 2.8f; t += Time.deltaTime)
            {
                SetTextAlpha(title, Mathf.SmoothStep(0f, 1f, t / 1.2f));
                SetTextAlpha(thanks, Mathf.SmoothStep(0f, 1f, (t - 0.8f) / 1.2f));
                SetTextAlpha(replay, Mathf.SmoothStep(0f, 1f, (t - 1.8f)));
                yield return null;
            }
            SetTextAlpha(title, 1f);
            SetTextAlpha(thanks, 1f);
            SetTextAlpha(replay, 1f);
            replay.gameObject.SetActive(true);
            ReplayAvailable = true;
            while (UnityEngine.InputSystem.Keyboard.current == null ||
                !UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame) yield return null;
            JourneyChoices.Reset();
            SceneManager.LoadScene("00_Prologue");
        }

        private static void SetTextAlpha(Text text, float alpha)
        {
            var color = text.color;
            color.a = Mathf.Clamp01(alpha);
            text.color = color;
        }

        private void UpdateObjectiveText()
        {
            if (objectiveText == null) return;
            objectiveText.text = string.Empty;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
