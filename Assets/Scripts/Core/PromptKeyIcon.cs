using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    public sealed class PromptKeyIcon : MonoBehaviour
    {
        private InlineKeyIcon inline;
        public bool IsVisible => inline != null && inline.RenderedIconCount > 0;

        public void Initialize(Text owner)
        {
            inline = InlineKeyIcon.Ensure(owner);
            var old = owner.transform.Find("Prompt E Icon");
            if (old != null) old.gameObject.SetActive(false);
        }
    }
}
