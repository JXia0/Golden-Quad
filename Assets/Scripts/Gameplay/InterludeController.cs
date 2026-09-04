using System.Collections;
using UnityEngine;

namespace LetGo
{
    public sealed class InterludeController : MonoBehaviour
    {
        [TextArea] [SerializeField] private string[] cards;
        [SerializeField] private float secondsPerCard = 2f;

        public void Configure(string[] values, float interval)
        {
            cards = values;
            secondsPerCard = interval;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.8f);
            foreach (var card in cards)
            {
                StorySceneDirector.Instance?.Say(card, secondsPerCard - 0.2f);
                yield return new WaitForSeconds(secondsPerCard);
            }
            StorySceneDirector.Instance?.LoadNextScene();
        }
    }
}
