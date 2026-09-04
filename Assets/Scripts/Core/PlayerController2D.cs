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

        private Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private bool controlsEnabled = true;
        private Vector3 characterScale;
        private float facing = 1f;

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

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            body.freezeRotation = true;
            characterScale = transform.localScale;
        }

        private void Update()
        {
            if (!controlsEnabled) return;
            if (GameInput.JumpPressed && IsGrounded()) body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            if (GameInput.RestartPressed) StorySceneDirector.Instance?.RespawnPlayer();
        }

        private void FixedUpdate()
        {
            if (!controlsEnabled) return;
            body.linearVelocity = new Vector2(GameInput.Horizontal * moveSpeed, body.linearVelocity.y);
            if (Mathf.Abs(GameInput.Horizontal) > 0.01f)
            {
                facing = Mathf.Sign(GameInput.Horizontal);
                ApplyScale();
            }
        }

        public void SetCharacterScale(Vector3 value)
        {
            characterScale = value;
            ApplyScale();
        }

        private void ApplyScale()
        {
            transform.localScale = new Vector3(Mathf.Abs(characterScale.x) * facing, characterScale.y, characterScale.z);
        }

        private bool IsGrounded()
        {
            var bounds = bodyCollider.bounds;
            var origin = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
            var hit = Physics2D.BoxCast(origin, new Vector2(bounds.size.x * 0.75f, 0.05f), 0f, Vector2.down,
                groundCheckDistance, groundMask);
            return hit.collider != null && hit.collider != bodyCollider;
        }
    }
}
