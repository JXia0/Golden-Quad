using UnityEngine;

namespace LetGo
{
    public sealed class InspectPoint : ProximityInteractable
    {
        [TextArea] [SerializeField] private string observation;

        public void Configure(string promptText, string line)
        {
            prompt = promptText;
            observation = line;
        }

        protected override void Interact()
        {
            completed = true;
            StorySceneDirector.Instance?.ClearPrompt(prompt);
            StorySceneDirector.Instance?.Say(observation, 3f);
        }
    }
}
