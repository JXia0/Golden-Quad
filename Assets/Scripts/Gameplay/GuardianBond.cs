using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class GuardianBond : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform guardian;
        [SerializeField] private float maximumDistance = 6f;
        [SerializeField] private bool holding = true;
        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
        }

        public void Configure(Transform child, Transform adult)
        {
            player = child;
            guardian = adult;
        }

        private void LateUpdate()
        {
            if (player == null || guardian == null) return;
            line.enabled = holding;
            if (!holding) return;
            line.SetPosition(0, guardian.position + Vector3.up * 0.6f);
            line.SetPosition(1, player.position + Vector3.up * 0.5f);
            var delta = player.position - guardian.position;
            if (delta.magnitude > maximumDistance)
            {
                player.position = guardian.position + delta.normalized * maximumDistance;
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null) body.linearVelocity = new Vector2(Mathf.Min(0f, body.linearVelocity.x), body.linearVelocity.y);
            }
        }

        public void ReleaseHand()
        {
            holding = false;
            line.enabled = false;
            var courage = player != null ? player.GetComponent<CourageSystem>() : null;
            if (courage != null) courage.Draining = true;
        }
    }
}
