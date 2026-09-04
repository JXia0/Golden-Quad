using UnityEngine;

namespace LetGo
{
    public sealed class JourneyStartReset : MonoBehaviour
    {
        private void Awake() => JourneyChoices.Reset();
    }
}
