using UnityEngine;

namespace LetGo
{
    public sealed class CourageSystem : MonoBehaviour
    {
        [SerializeField] private float maxCourage = 10f;
        [SerializeField] private float drainPerSecond = 1f;
        [SerializeField] private float breatheRecoveryPerSecond = 2.2f;
        [SerializeField] private bool allowBreathingRecovery = true;
        [SerializeField] private HandConnection supportConnection;
        [SerializeField] private string supportTargetId;
        [SerializeField] private float supportRecoveryPerSecond = 4f;
        [SerializeField] private float afterglowSeconds = 3f;
        [SerializeField] private SpriteRenderer innerLight;
        [SerializeField] private bool draining;
        private float courage;
        private float afterglowRemaining;
        private bool wasSupported;

        public float Normalized => maxCourage <= 0f ? 1f : courage / maxCourage;
        public bool Draining { get => draining; set => draining = value; }
        public bool IsProtected => IsSupported || afterglowRemaining > 0f;
        public float Protection01 => IsSupported
            ? 1f
            : afterglowSeconds <= 0f ? 0f : Mathf.Clamp01(afterglowRemaining / afterglowSeconds);

        private bool IsSupported => supportConnection != null
            && supportConnection.CurrentTarget != null
            && supportConnection.CurrentTarget.TargetId == supportTargetId;

        public void Configure(float duration, float drainRate)
        {
            maxCourage = duration;
            drainPerSecond = drainRate;
            courage = maxCourage;
        }

        public void ConfigureSupport(HandConnection connection, string targetId, float graceSeconds,
            float recoveryPerSecond = 4f, bool allowBreathing = true)
        {
            supportConnection = connection;
            supportTargetId = targetId;
            afterglowSeconds = graceSeconds;
            supportRecoveryPerSecond = recoveryPerSecond;
            allowBreathingRecovery = allowBreathing;
        }

        private void Awake() => courage = maxCourage;

        private void Update()
        {
            var supported = IsSupported;
            if (wasSupported && !supported) afterglowRemaining = afterglowSeconds;
            wasSupported = supported;

            if (!supported) afterglowRemaining = Mathf.Max(0f, afterglowRemaining - Time.deltaTime);

            if (draining)
            {
                if (supported) courage += supportRecoveryPerSecond * Time.deltaTime;
                else if (afterglowRemaining <= 0f) courage -= drainPerSecond * Time.deltaTime;

                if (allowBreathingRecovery && GameInput.InteractHeld)
                    courage += breatheRecoveryPerSecond * Time.deltaTime;
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
