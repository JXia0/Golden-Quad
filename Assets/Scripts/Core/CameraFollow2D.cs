using UnityEngine;

namespace LetGo
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 40f;
        private Vector3 velocity;

        public void SetTarget(Transform value) => target = value;
        public void SetHorizontalBounds(float minimum, float maximum)
        {
            minX = minimum;
            maxX = Mathf.Max(minimum, maximum);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var desired = new Vector3(Mathf.Clamp(target.position.x, minX, maxX), transform.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
