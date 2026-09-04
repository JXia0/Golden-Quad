using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class ChoiceEchoLabel : MonoBehaviour
    {
        [SerializeField] private string category;

        public void Configure(string value) => category = value;

        private void Start() => GetComponent<TextMesh>().text = JourneyChoices.Get(category);
    }
}
