using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    [RequireComponent(typeof(Image))]
    public sealed class CourageHUD : MonoBehaviour
    {
        private CourageSystem courage;
        private Image fill;

        private void Awake() => fill = GetComponent<Image>();

        private void Update()
        {
            if (courage == null)
            {
                courage = FindAnyObjectByType<CourageSystem>();
                return;
            }
            fill.fillAmount = courage.Normalized;
            fill.enabled = courage.Draining;
            var background = transform.parent.GetComponent<Image>();
            if (background != null) background.enabled = courage.Draining;
        }
    }
}
