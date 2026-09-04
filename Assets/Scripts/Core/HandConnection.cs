using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(PlayerController2D))]
    public sealed class HandConnection : MonoBehaviour
    {
        [SerializeField] private LineRenderer tether;
        [SerializeField] private bool selfAnchorEnabled;
        [SerializeField] private float selfChargeSeconds = 1.2f;
        [SerializeField] private float stableMoveSeconds = 3f;

        private PlayerController2D player;
        private Rigidbody2D body;
        private Animator animator;
        private HoldTarget currentTarget;
        private HoldTarget promptedTarget;
        private bool selfAnchoring;
        private float selfCharge;
        private float stableRemaining;
        private string displayedPrompt;

        public HoldTarget CurrentTarget => currentTarget;
        public bool IsSelfAnchoring => selfAnchoring;
        public float SelfChargeNormalized => selfChargeSeconds <= 0f ? 1f : Mathf.Clamp01(selfCharge / selfChargeSeconds);
        public float LastFullSelfReleaseTime { get; private set; } = -100f;
        public float Stability01
        {
            get
            {
                if (currentTarget != null) return 1f;
                if (selfAnchoring) return Mathf.Lerp(0.45f, 1f, SelfChargeNormalized);
                return stableMoveSeconds <= 0f ? 0f : Mathf.Clamp01(stableRemaining / stableMoveSeconds);
            }
        }

        public void Configure(LineRenderer line, bool allowSelfAnchor)
        {
            tether = line;
            selfAnchorEnabled = allowSelfAnchor;
            if (tether != null) tether.enabled = false;
        }

        private void Awake()
        {
            player = GetComponent<PlayerController2D>();
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            stableRemaining = Mathf.Max(0f, stableRemaining - Time.deltaTime);

            if (currentTarget != null)
            {
                ClearTargetPrompt();
                currentTarget.TickHeld(this, Time.deltaTime);
                UpdateTether();
                ConstrainDistance();
                if (GameInput.InteractReleased || !GameInput.InteractHeld) Detach();
                return;
            }

            if (tether != null) tether.enabled = false;
            var nearest = HoldTarget.FindNearest(transform.position);
            if (nearest != null)
            {
                StopSelfAnchor(false);
                ShowTargetPrompt(nearest);
                if (GameInput.InteractHeld) Attach(nearest);
                return;
            }

            ClearTargetPrompt();
            if (!selfAnchorEnabled)
            {
                StopSelfAnchor(false);
                return;
            }

            if (GameInput.InteractHeld)
            {
                if (!selfAnchoring)
                {
                    selfAnchoring = true;
                    selfCharge = 0f;
                    player.SetInteractionLocked(true);
                }
                selfCharge = Mathf.Min(selfChargeSeconds, selfCharge + Time.deltaTime);
            }
            else StopSelfAnchor(true);
        }

        private void Attach(HoldTarget target)
        {
            currentTarget = target;
            target.BeginHold(this);
            if (target.TargetId == "parent") SetAnimatorBool("HoldingParent", true);
            SceneAudio.Instance?.PlayInteract();
            player.SetInteractionLocked(false);
            if (tether != null) tether.enabled = true;
            StorySceneDirector.Instance?.ClearPrompt(displayedPrompt);
            displayedPrompt = null;
            promptedTarget = null;
        }

        private void Detach()
        {
            var released = currentTarget;
            currentTarget = null;
            if (tether != null) tether.enabled = false;
            if (released != null && released.TargetId == "parent") SetAnimatorBool("HoldingParent", false);
            if (released != null && released.Mode == HoldTargetMode.Carryable) SceneAudio.Instance?.PlayItemMove();
            else SceneAudio.Instance?.PlayRelease();
            released?.EndHold();
        }

        private void StopSelfAnchor(bool grantStableMovement)
        {
            if (!selfAnchoring) return;
            if (grantStableMovement && SelfChargeNormalized >= 0.98f)
            {
                stableRemaining = stableMoveSeconds;
                LastFullSelfReleaseTime = Time.time;
                SceneAudio.Instance?.PlayBreath();
            }
            selfAnchoring = false;
            selfCharge = 0f;
            player.SetInteractionLocked(false);
        }

        private void UpdateTether()
        {
            if (tether == null || currentTarget == null) return;
            tether.SetPosition(0, transform.position + new Vector3(0.35f, 0.35f, 0f));
            tether.SetPosition(1, currentTarget.transform.position + new Vector3(0f, 0.35f, 0f));
            var tension = Mathf.Clamp01(Vector2.Distance(transform.position, currentTarget.transform.position) / currentTarget.MaxDistance);
            tether.startWidth = tether.endWidth = Mathf.Lerp(0.16f, 0.07f, tension);
            var color = Color.Lerp(new Color(1f, 0.62f, 0.25f), Color.white, tension);
            tether.startColor = tether.endColor = color;
        }

        private void ConstrainDistance()
        {
            if (currentTarget == null || currentTarget.MaxDistance <= 0f) return;
            var offset = (Vector2)transform.position - (Vector2)currentTarget.transform.position;
            if (offset.magnitude <= currentTarget.MaxDistance) return;
            var clamped = (Vector2)currentTarget.transform.position + offset.normalized * currentTarget.MaxDistance;
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private void ShowTargetPrompt(HoldTarget target)
        {
            if (promptedTarget == target && displayedPrompt == target.Prompt) return;
            StorySceneDirector.Instance?.ClearPrompt(displayedPrompt);
            promptedTarget = target;
            displayedPrompt = target.Prompt;
            StorySceneDirector.Instance?.ShowPrompt(displayedPrompt);
        }

        private void ClearTargetPrompt()
        {
            StorySceneDirector.Instance?.ClearPrompt(displayedPrompt);
            displayedPrompt = null;
            promptedTarget = null;
        }

        private void OnDisable()
        {
            ClearTargetPrompt();
            player?.SetInteractionLocked(false);
        }

        private void SetAnimatorBool(string parameter, bool value)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var item in animator.parameters)
            {
                if (item.name != parameter || item.type != AnimatorControllerParameterType.Bool) continue;
                animator.SetBool(parameter, value);
                return;
            }
        }
    }
}
