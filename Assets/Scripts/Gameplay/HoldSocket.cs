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

        public static bool TryPlace(HoldTarget target)
        {
            HoldSocket best = null;
            var bestDistance = float.MaxValue;
            foreach (var socket in ActiveSockets)
            {
                if (socket == null || !socket.CanAccept(target)) continue;
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
            if (completed) return false;
            var director = StorySceneDirector.Instance;
            if (director != null && director.CompletedObjectives < prerequisiteCount) return false;
            if (!string.IsNullOrEmpty(expectedTargetId)) return target.TargetId == expectedTargetId;
            return !string.IsNullOrEmpty(expectedGroup) && target.SelectionGroup == expectedGroup;
        }

        private void Accept(HoldTarget target)
        {
            completed = true;
            target.MarkPlaced(transform.position);
            if (!string.IsNullOrEmpty(target.SelectionGroup))
            {
                foreach (var other in Object.FindObjectsByType<HoldTarget>())
                    if (other != target && other.SelectionGroup == target.SelectionGroup) other.MarkUnavailable();
            }
            JourneyChoices.Record(target.ChoiceCategory, target.ChoiceValue, false);
            StorySceneDirector.Instance?.CompleteObjective(completionLine);
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(1f, 0.72f, 0.25f);
        }
    }
}
