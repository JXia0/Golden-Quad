using System.Collections.Generic;
using UnityEngine;

namespace LetGo
{
    public sealed class HoldSocket : MonoBehaviour
    {
        private static readonly List<HoldSocket> ActiveSockets = new();

        [SerializeField] private string expectedTargetId;
        [SerializeField] private string expectedGroup;
        [SerializeField] private float radius = 1.8f;
        [SerializeField] private int prerequisiteCount;
        [TextArea] [SerializeField] private string completionLine;
        private bool completed;
        private HoldTarget occupant;
        public bool Replaceable { get; set; }
        public bool Locked { get; set; }
        public bool CompletesObjective { get; set; } = true;
        public HoldTarget Occupant => occupant;
        public bool Completed => completed;
        public System.Action<HoldTarget> Placed;
        public System.Func<HoldTarget, bool> Acceptance;

        public void Configure(string targetId, string group, float acceptRadius, int prerequisite, string line)
        {
            expectedTargetId = targetId;
            expectedGroup = group;
            radius = acceptRadius;
            prerequisiteCount = prerequisite;
            completionLine = line;
        }

        private void OnEnable()
        {
            if (!ActiveSockets.Contains(this)) ActiveSockets.Add(this);
        }

        private void OnDisable() => ActiveSockets.Remove(this);

        public static void Reclaim(HoldTarget target)
        {
            foreach (var socket in ActiveSockets)
                if (socket != null && socket.occupant == target) socket.occupant = null;
        }

        public static bool TryPlace(HoldTarget target)
        {
            HoldSocket best = null;
            var bestDistance = float.MaxValue;
            foreach (var socket in ActiveSockets)
            {
                if (socket == null || !socket.CanAccept(target)) continue;
                if (socket.gameObject.scene != target.gameObject.scene) continue;
                var distance = Vector2.Distance(target.transform.position, socket.transform.position);
                if (distance > socket.radius || distance >= bestDistance) continue;
                best = socket;
                bestDistance = distance;
            }
            if (best == null) return false;
            best.Accept(target);
            return true;
        }

        private bool CanAccept(HoldTarget target)
        {
            if (Acceptance != null && !Acceptance(target)) return false;
            if (Locked || (completed && !Replaceable) || target == occupant) return false;
            var director = StorySceneDirector.Instance;
            if (director != null && director.CompletedObjectives < prerequisiteCount) return false;
            if (!string.IsNullOrEmpty(expectedTargetId)) return target.TargetId == expectedTargetId;
            return !string.IsNullOrEmpty(expectedGroup) && target.SelectionGroup == expectedGroup;
        }

        private void Accept(HoldTarget target)
        {
            var firstPlacement = !completed;
            if (occupant != null && Replaceable) occupant.ReturnToRest();
            completed = true;
            occupant = target;
            target.MarkPlaced(transform.position);
            if (!Replaceable && !string.IsNullOrEmpty(target.SelectionGroup))
            {
                foreach (var other in Object.FindObjectsByType<HoldTarget>())
                    if (other != target && other.SelectionGroup == target.SelectionGroup) other.MarkUnavailable();
            }
            JourneyChoices.Record(target.ChoiceCategory, target.ChoiceValue, false);
            if (firstPlacement && CompletesObjective) StorySceneDirector.Instance?.CompleteObjective(completionLine);
            else SceneAudio.Instance?.PlayItemMove();
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(1f, 0.72f, 0.25f);
            Placed?.Invoke(target);
        }
    }
}
