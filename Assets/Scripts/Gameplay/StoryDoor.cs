using UnityEngine;

namespace LetGo
{
    public sealed class StoryDoor : ProximityInteractable
    {
        [SerializeField] private bool requireObjectives = true;
        [SerializeField] private bool openEnding;
        [TextArea] [SerializeField] private string lockedLine = "我还没有准备好。";

        public void Configure(bool needsObjectives, bool ending, string promptText)
        {
            requireObjectives = needsObjectives;
            openEnding = ending;
            prompt = promptText;
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
            director.ClearPrompt(prompt);
            SceneAudio.Instance?.PlayDoor();
            if (openEnding) director.ShowOpenEnding();
            else director.LoadNextScene();
        }
    }
}
