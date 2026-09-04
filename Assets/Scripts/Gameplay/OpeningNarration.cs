using System.Collections;
using UnityEngine;

namespace LetGo
{
    public sealed class OpeningNarration : MonoBehaviour
    {
        [TextArea] [SerializeField] private string line;
        [SerializeField] private float delay = 0.8f;
        [SerializeField] private float duration = 5f;

        public void Configure(string value, float wait = 0.8f, float showFor = 5f)
        {
            line = value;
            delay = wait;
            duration = showFor;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(delay);
            StorySceneDirector.Instance?.Say(line, duration);
        }
    }
}
