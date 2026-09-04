using UnityEngine;

namespace LetGo
{
    public sealed class HoldObjectiveStation : ProximityInteractable
    {
        [SerializeField] private float holdSeconds = 2f;
        [TextArea] [SerializeField] private string completionLine;
        [SerializeField] private int prerequisiteCount;
        private float progress;

        public void Configure(string promptText, string line, float seconds, int prerequisite)
        {
            prompt = promptText;
            completionLine = line;
            holdSeconds = seconds;
            prerequisiteCount = prerequisite;
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
                director?.ClearPrompt();
                return;
            }
            if (director != null && director.CompletedObjectives < prerequisiteCount)
            {
                director.ShowPrompt("先完成前面的步骤");
                return;
            }
            if (GameInput.InteractHeld)
            {
                progress += Time.deltaTime;
                director?.ShowPrompt($"保持呼吸  {Mathf.RoundToInt(progress / holdSeconds * 100f)}%");
                if (progress >= holdSeconds) Complete();
            }
            else director?.ShowPrompt(prompt);
        }

        protected override void Interact() { }

        private void Complete()
        {
            completed = true;
            StorySceneDirector.Instance?.ClearPrompt();
            StorySceneDirector.Instance?.CompleteObjective(completionLine);
            player.GetComponent<CourageSystem>()?.RestoreFull();
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(1f, 0.72f, 0.25f);
        }
    }
}
