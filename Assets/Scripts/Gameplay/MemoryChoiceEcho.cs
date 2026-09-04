using UnityEngine;

namespace LetGo
{
    public sealed class MemoryChoiceEcho : MonoBehaviour
    {
        [SerializeField] private string category;
        [SerializeField] private string expectedValue;

        public void Configure(string choiceCategory, string choiceValue)
        {
            category = choiceCategory;
            expectedValue = choiceValue;
        }

        private void Start()
        {
            gameObject.SetActive(JourneyChoices.Get(category) == expectedValue);
        }
    }
}
