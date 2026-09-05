using UnityEngine;

namespace LetGo
{
    public sealed class StoryDoor : ProximityInteractable
    {
        private const string CarryPrompt = "F · 带着手里的东西开门";
        [SerializeField] private bool requireObjectives = true;
        [SerializeField] private bool openEnding;
        [TextArea] [SerializeField] private string lockedLine = "我还没有准备好。";

        public void Configure(bool needsObjectives, bool ending, string promptText)
        {
            requireObjectives = needsObjectives;
            openEnding = ending;
            prompt = promptText;
        }

        protected override void Update()
        {
            base.Update();
            if (completed || player == null || Vector2.Distance(player.position, transform.position) > radius)
            {
                StorySceneDirector.Instance?.ClearPrompt(CarryPrompt);
                return;
            }
            if (player.GetComponent<HandConnection>()?.CurrentTarget != null)
                StorySceneDirector.Instance?.ShowPrompt(CarryPrompt);
            if (GameInput.UsePressed) Interact();
        }

        protected override void Interact()
        {
            var director = StorySceneDirector.Instance;
            if (director == null) return;
            if (requireObjectives && !director.ObjectivesComplete)
            {
                director.Say(lockedLine, 2f);
                return;
            }
            completed = true;
            if (gameObject.scene.name == "01_Kindergarten")
                JourneyChoices.TakeChildhoodToy(director.Player.GetComponent<HandConnection>()?.CurrentTarget?.TargetId == "comfort-toy");
            director.ClearPrompt(prompt);
            SceneAudio.Instance?.PlayDoor();
            if (openEnding) director.ShowOpenEnding();
            else director.LoadNextScene();
        }
    }
}
