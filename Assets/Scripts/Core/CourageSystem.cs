using UnityEngine;

namespace LetGo
{
    public sealed class CourageSystem : MonoBehaviour
    {
        [SerializeField] private float maxCourage = 10f;
        [SerializeField] private float drainPerSecond = 1f;
        [SerializeField] private float breatheRecoveryPerSecond = 2.2f;
        [SerializeField] private SpriteRenderer innerLight;
        [SerializeField] private bool draining;
        private float courage;

        public float Normalized => maxCourage <= 0f ? 1f : courage / maxCourage;
        public bool Draining { get => draining; set => draining = value; }

        public void Configure(float duration, float drainRate)
        {
            maxCourage = duration;
            drainPerSecond = drainRate;
            courage = maxCourage;
        }

        private void Awake() => courage = maxCourage;

        private void Update()
        {
            if (draining)
            {
                courage -= drainPerSecond * Time.deltaTime;
                if (GameInput.InteractHeld) courage += breatheRecoveryPerSecond * Time.deltaTime;
                courage = Mathf.Clamp(courage, 0f, maxCourage);
                if (courage <= 0f)
                {
                    StorySceneDirector.Instance?.Say("没关系。再试一次。", 1.5f);
                    StorySceneDirector.Instance?.RespawnPlayer();
                }
            }
            if (innerLight != null)
            {
                var color = innerLight.color;
                color.a = Mathf.Lerp(0.15f, 0.85f, Normalized);
                innerLight.color = color;
                innerLight.transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1.8f, Normalized);
            }
        }

        public void RestoreFull() => courage = maxCourage;
        public void AddGrowth(float amount)
        {
            maxCourage += amount;
            courage = maxCourage;
        }
    }
}
