using UnityEngine;

namespace LetGo
{
    public sealed class HandReleasePoint : ProximityInteractable
    {
        [SerializeField] private GuardianBond bond;

        public void Configure(GuardianBond value)
        {
            bond = value;
            prompt = "按 E 放开手";
        }

        protected override void Interact()
        {
            if (bond == null) return;
            completed = true;
            StorySceneDirector.Instance?.ClearPrompt(prompt);
            bond.ReleaseHand();
            StorySceneDirector.Instance?.Say("你没有跟进来。\n但当我回头时，你还在那里。", 4f);
        }
    }
}
