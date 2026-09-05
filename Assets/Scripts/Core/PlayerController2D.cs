using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.8f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float groundCheckDistance = 0.12f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private Transform characterVisual;

        private Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private SpriteRenderer characterRenderer;
        private bool controlsEnabled = true;
        private bool interactionLocked;
        private Vector3 characterScale;
        private float facing = 1f;
        private float footstepTimer;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private ContactFilter2D groundFilter;

        public bool ControlsEnabled
        {
            get => controlsEnabled;
            set
            {
                controlsEnabled = value;
                if (!value && body != null) body.linearVelocity = Vector2.zero;
            }
        }

        public Vector2 Velocity => body == null ? Vector2.zero : body.linearVelocity;
        public float FacingDirection => facing;
        // A companion can offer movement; explicit left input still lets the player pull back.
        public float? AssistedHorizontal { get; set; }
        public void SetJumpForce(float value) => jumpForce = value;
        public SpriteRenderer CharacterRenderer => characterRenderer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            if (characterVisual == null) characterVisual = transform.Find("Character Visual");
            characterRenderer = characterVisual == null ? null : characterVisual.GetComponent<SpriteRenderer>();
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            characterScale = characterVisual == null ? Vector3.one : characterVisual.localScale;
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(groundMask);
            groundFilter.useTriggers = false;
        }

        private void Update()
        {
            if (!controlsEnabled) return;
            if (GameInput.RestartPressed) StorySceneDirector.Instance?.RespawnPlayer();
            if (interactionLocked) return;
            if (GameInput.JumpPressed && IsGrounded()) body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            UpdateFootsteps();
        }

        private void FixedUpdate()
        {
            if (!controlsEnabled || interactionLocked)
            {
                if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }
            var horizontal = AssistedHorizontal ?? GameInput.Horizontal;
            body.linearVelocity = new Vector2(horizontal * moveSpeed, body.linearVelocity.y);
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                var nextFacing = Mathf.Sign(horizontal);
                if (nextFacing != facing) { facing = nextFacing; ApplyScale(); }
            }
        }

        public void SetCharacterScale(Vector3 value)
        {
            characterScale = value;
            ApplyScale();
        }

        public void ConfigureCharacterVisual(Transform visual)
        {
            characterVisual = visual;
            characterRenderer = visual == null ? null : visual.GetComponent<SpriteRenderer>();
        }

        public void FitCharacterVisualToCollider()
        {
            var renderer = CharacterRenderer;
            if (renderer == null || renderer.sprite == null || bodyCollider == null) return;

            characterVisual.localScale = Vector3.one;
            var currentBounds = renderer.bounds.size;
            if (currentBounds.x <= 0.0001f || currentBounds.y <= 0.0001f) return;

            var targetBounds = bodyCollider.bounds.size;
            var factor = Mathf.Min(targetBounds.x / currentBounds.x, targetBounds.y / currentBounds.y);
            SetCharacterScale(new Vector3(factor, factor, 1f));
        }

        public void SetInteractionLocked(bool value)
        {
            interactionLocked = value;
            if (value && body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private void ApplyScale()
        {
            if (characterVisual == null) return;
            characterVisual.localScale = new Vector3(
                Mathf.Abs(characterScale.x) * facing,
                characterScale.y,
                characterScale.z);
        }

        private bool IsGrounded()
        {
            var bounds = bodyCollider.bounds;
            var origin = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
            var hitCount = Physics2D.BoxCast(origin, new Vector2(bounds.size.x * 0.75f, 0.05f), 0f,
                Vector2.down, groundFilter, groundHits, groundCheckDistance);
            for (var i = 0; i < hitCount; i++)
            {
                var hitCollider = groundHits[i].collider;
                if (hitCollider != null && hitCollider != bodyCollider) return true;
            }
            return false;
        }

        private void UpdateFootsteps()
        {
            if (Mathf.Abs(AssistedHorizontal ?? GameInput.Horizontal) < 0.1f || !IsGrounded())
            {
                footstepTimer = 0f;
                return;
            }
            footstepTimer -= Time.deltaTime;
            if (footstepTimer > 0f) return;
            footstepTimer = 0.42f;
            SceneAudio.Instance?.PlayFootstep();
        }
    }
}
