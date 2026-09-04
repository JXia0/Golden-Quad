using UnityEngine;

namespace LetGo
{
    public sealed class HoldObjectiveStation : ProximityInteractable
    {
        [SerializeField] private float holdSeconds = 2f;
        [TextArea] [SerializeField] private string completionLine;
        [SerializeField] private string lockedPrompt = "先完成前面的步骤";
        [SerializeField] private int prerequisiteCount;
        private float progress;
        private string displayedPrompt;

        public void Configure(string promptText, string line, float seconds, int prerequisite,
            float interactionRadius = 1.5f, string lockedText = "先完成前面的步骤")
        {
            prompt = promptText;
            completionLine = line;
            holdSeconds = seconds;
            prerequisiteCount = prerequisite;
            radius = interactionRadius;
            lockedPrompt = lockedText;
        }

        protected override void Update()
        {
            if (completed) return;
            if (player == null)
            {
                var controller = FindAnyObjectByType<PlayerController2D>();
                if (controller != null) player = controller.transform;
                return;
            }
            var near = Vector2.Distance(player.position, transform.position) <= radius;
            var director = StorySceneDirector.Instance;
            if (!near)
            {
                progress = 0f;
                director?.ClearPrompt(displayedPrompt);
                displayedPrompt = null;
                return;
            }
            if (director != null && director.CompletedObjectives < prerequisiteCount)
            {
                SetPrompt(director, lockedPrompt);
                return;
            }
            if (GameInput.InteractHeld)
            {
                progress += Time.deltaTime;
                SetPrompt(director, $"{prompt}  {Mathf.RoundToInt(progress / holdSeconds * 100f)}%");
                if (progress >= holdSeconds) Complete();
            }
            else SetPrompt(director, prompt);
        }

        protected override void Interact() { }

        private void Complete()
        {
            completed = true;
            StorySceneDirector.Instance?.ClearPrompt(displayedPrompt);
            displayedPrompt = null;
            StorySceneDirector.Instance?.CompleteObjective(completionLine);
            player.GetComponent<CourageSystem>()?.RestoreFull();
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(1f, 0.72f, 0.25f);
        }

        private void SetPrompt(StorySceneDirector director, string value)
        {
            director?.ShowPrompt(value);
            displayedPrompt = value;
        }
    }
}
