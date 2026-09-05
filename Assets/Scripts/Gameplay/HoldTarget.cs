using System.Collections.Generic;
using UnityEngine;

namespace LetGo
{
    public enum HoldTargetMode
    {
        Support,
        Companion,
        Carryable,
        Recipient
    }

    public sealed class HoldTarget : MonoBehaviour
    {
        private static readonly List<HoldTarget> ActiveTargets = new();

        [SerializeField] private string targetId;
        [SerializeField] private HoldTargetMode mode;
        [SerializeField] private string prompt = "按住 E 抓住";
        [SerializeField] private float interactionRadius = 1.8f;
        [SerializeField] private float maxDistance = 2.4f;
        [SerializeField] private int prerequisiteCount;
        [SerializeField] private float followOffset = 1.1f;
        [SerializeField] private float followSpeed = 4f;
        [SerializeField] private float maximumFollowX = 999f;
        [SerializeField] private string selectionGroup;
        [SerializeField] private string choiceCategory;
        [SerializeField] private string choiceValue;

        private HandConnection heldBy;
        private bool placed;
        private bool unavailable;
        private Vector3 restPosition;
        private float reassurance;
        private bool needsReassurance;
        private float walkRemaining;
        private Vector3 walkDestination;
        public bool GentleCompanion { get; set; }
        public bool ExternallyGuided { get; set; }
        public bool AllowReclaim { get; set; }
        public float? DropFloorY { get; set; }
        public Vector3 CarryOffset { get; set; } = new Vector3(0.8f, 0.55f);
        public bool IsWaiting => GentleCompanion && needsReassurance;
        public float Reassurance01 => Mathf.Clamp01(reassurance / 0.8f);
        public bool IsPlaced => placed;
        public bool IsHeld => heldBy != null;
        public System.Action<HoldTarget> Released;

        public string TargetId => targetId;
        public HoldTargetMode Mode => mode;
        public string Prompt => prompt;
        public float MaxDistance => maxDistance;
        public float HeldDuration { get; private set; }
        public string SelectionGroup => selectionGroup;
        public string ChoiceCategory => choiceCategory;
        public string ChoiceValue => choiceValue;
        public bool CanHold => (!placed || AllowReclaim) && !unavailable &&
            (StorySceneDirector.Instance == null || StorySceneDirector.Instance.CompletedObjectives >= prerequisiteCount);

        public void Configure(string id, HoldTargetMode targetMode, string promptText, float radius, float distance,
            int prerequisite = 0, float companionOffset = 1.1f, float companionSpeed = 4f,
            float companionMaxX = 999f, string group = "", string category = "", string value = "")
        {
            targetId = id;
            mode = targetMode;
            prompt = promptText;
            interactionRadius = radius;
            maxDistance = distance;
            prerequisiteCount = prerequisite;
            followOffset = companionOffset;
            followSpeed = companionSpeed;
            maximumFollowX = companionMaxX;
            selectionGroup = group;
            choiceCategory = category;
            choiceValue = value;
        }

        private void OnEnable()
        {
            restPosition = transform.position;
            if (!ActiveTargets.Contains(this)) ActiveTargets.Add(this);
        }

        private void OnDisable() => ActiveTargets.Remove(this);

        public static HoldTarget FindNearest(Vector3 playerPosition)
        {
            HoldTarget nearest = null;
            var bestDistance = float.MaxValue;
            foreach (var target in ActiveTargets)
            {
                if (target == null || !target.CanHold) continue;
                var distance = Vector2.Distance(playerPosition, target.transform.position);
                if (distance > target.interactionRadius || distance >= bestDistance) continue;
                nearest = target;
                bestDistance = distance;
            }
            return nearest;
        }

        public void BeginHold(HandConnection connection)
        {
            if (placed && AllowReclaim)
            {
                HoldSocket.Reclaim(this);
                restPosition = transform.position;
                transform.SetParent(null, true);
                placed = false;
                AllowReclaim = false;
            }
            heldBy = connection;
            HeldDuration = 0f;
            if (GentleCompanion) { needsReassurance = true; reassurance = 0f; }
        }

        public void TickHeld(HandConnection connection, float deltaTime)
        {
            if (heldBy != connection) return;
            HeldDuration += deltaTime;
            var playerPosition = connection.transform.position;
            if (mode == HoldTargetMode.Carryable)
            {
                var desired = playerPosition + CarryOffset;
                if (targetId == "conclusion")
                    desired += new Vector3(Mathf.Sin(Time.time * 9f), Mathf.Sin(Time.time * 7f), 0f) * 0.045f;
                transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-12f * deltaTime));
            }
            else if (mode == HoldTargetMode.Companion)
            {
                if (ExternallyGuided) return;
                if (GentleCompanion)
                {
                    var distance = Mathf.Abs(playerPosition.x - transform.position.x);
                    if (distance > maxDistance * 0.82f) { needsReassurance = true; reassurance = 0f; }
                    if (needsReassurance)
                    {
                        reassurance = distance < maxDistance * 0.62f ? reassurance + deltaTime : 0f;
                        if (reassurance < 0.8f) return;
                        needsReassurance = false;
                    }
                }
                var desiredX = Mathf.Min(playerPosition.x - followOffset, maximumFollowX);
                var desired = new Vector3(desiredX, transform.position.y, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, desired, followSpeed * deltaTime);
            }
        }

        public void EndHold()
        {
            heldBy = null;
            if (mode == HoldTargetMode.Carryable || mode == HoldTargetMode.Companion)
                HoldSocket.TryPlace(this);
            Released?.Invoke(this);
            if (!placed && mode == HoldTargetMode.Carryable)
            {
                // Put it within reach on the floor even if released during a jump.
                transform.position = new Vector3(transform.position.x, DropFloorY ?? restPosition.y, transform.position.z);
            }
        }

        public void CancelHold()
        {
            heldBy = null;
            if (mode == HoldTargetMode.Carryable && !placed) transform.position = restPosition;
        }

        public void ReturnToRest()
        {
            placed = false;
            transform.position = restPosition;
        }

        public void ReopenInteraction() => placed = false;

        public void WalkAway(Vector3 destination)
        {
            placed = true;
            walkDestination = new Vector3(destination.x, transform.position.y, transform.position.z);
            walkRemaining = 1.5f;
        }

        private void Update()
        {
            if (walkRemaining <= 0f) return;
            transform.position = Vector3.MoveTowards(transform.position, walkDestination, 1.7f * Time.deltaTime);
            if (Vector3.Distance(transform.position, walkDestination) < 0.03f) walkRemaining = 0f;
        }

        public void MarkPlaced(Vector3 position)
        {
            placed = true;
            transform.position = position;
        }

        public void MarkUnavailable()
        {
            unavailable = true;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                var color = renderer.color;
                color.a = 0.25f;
                renderer.color = color;
            }
        }
    }
}
