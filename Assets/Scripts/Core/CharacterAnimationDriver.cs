using UnityEngine;

namespace LetGo
{
    [RequireComponent(typeof(Animator), typeof(PlayerController2D))]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MovingId = Animator.StringToHash("Moving");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private static readonly int CarryingId = Animator.StringToHash("Carrying");
        private static readonly int SelfAnchoringId = Animator.StringToHash("SelfAnchoring");

        private Animator animator;
        private PlayerController2D player;
        private CarryInventory inventory;
        private HandConnection connection;
        private bool hasSpeed;
        private bool hasMoving;
        private bool hasVerticalSpeed;
        private bool hasCarrying;
        private bool hasSelfAnchoring;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            player = GetComponent<PlayerController2D>();
            inventory = GetComponent<CarryInventory>();
            connection = GetComponent<HandConnection>();

            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == SpeedId && parameter.type == AnimatorControllerParameterType.Float) hasSpeed = true;
                if (parameter.nameHash == MovingId && parameter.type == AnimatorControllerParameterType.Bool) hasMoving = true;
                if (parameter.nameHash == VerticalSpeedId && parameter.type == AnimatorControllerParameterType.Float) hasVerticalSpeed = true;
                if (parameter.nameHash == CarryingId && parameter.type == AnimatorControllerParameterType.Bool) hasCarrying = true;
                if (parameter.nameHash == SelfAnchoringId && parameter.type == AnimatorControllerParameterType.Bool) hasSelfAnchoring = true;
            }
        }

        private void Update()
        {
            var velocity = player.Velocity;
            var horizontalSpeed = Mathf.Abs(velocity.x);
            if (hasSpeed) animator.SetFloat(SpeedId, horizontalSpeed);
            if (hasMoving) animator.SetBool(MovingId, horizontalSpeed > 0.05f);
            if (hasVerticalSpeed) animator.SetFloat(VerticalSpeedId, velocity.y);
            if (hasCarrying) animator.SetBool(CarryingId, inventory != null && inventory.HasItem);
            if (hasSelfAnchoring) animator.SetBool(SelfAnchoringId, connection != null && connection.IsSelfAnchoring);
        }
    }
}
