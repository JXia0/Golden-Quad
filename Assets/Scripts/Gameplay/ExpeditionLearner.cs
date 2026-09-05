using UnityEngine;

namespace LetGo
{
    public enum LearnerState { Waiting, Walking, NeedsBridge, NeedsComfort, Returning, Arrived, Recalling }

    public sealed class ExpeditionLearner
    {
        public const float GapX = 16f;
        public const float GoalX = 23f;
        public float X { get; private set; } = 12f;
        public LearnerState State { get; private set; }
        public bool Running { get; private set; }
        public string Crossing { get; private set; } = "";
        public string Support { get; private set; } = "";
        public bool Independent { get; private set; }
        public int Retreats { get; private set; }
        private bool passedDark;
        private float practicedUntil = 17.8f;
        public float LearnedUntil { get; private set; } = 17.8f;
        public int Rehearsals { get; private set; }
        public void Recall()
        {
            if (State == LearnerState.Recalling || X <= 12.01f) return;
            LearnedUntil = Mathf.Max(LearnedUntil, practicedUntil);
            Rehearsals++;
            passedDark = Independent = false;
            Support = Crossing = "";
            Running = true;
            State = LearnerState.Recalling;
        }
        public void Start() => Running = true;

        public void Tick(float dt, string bridge, bool held, float playerX, bool light, bool toy)
        {
            if (State == LearnerState.Recalling)
            {
                X = Mathf.MoveTowards(X, 12f, dt * 3.5f);
                if (X <= 12.01f) { X = 12f; State = LearnerState.Waiting; Running = false; }
                return;
            }
            if (held) Running = true;
            if (!Running || State == LearnerState.Arrived) return;
            var comfortable = held || light || toy || passedDark || LearnedUntil >= 21.5f || X < LearnedUntil - 0.01f;
            if (X >= 18f && X < 21.5f && !comfortable)
            {
                var safeEdge = Mathf.Max(17.8f, LearnedUntil);
                if (X > safeEdge + 0.01f)
                {
                    if (State != LearnerState.Returning) Retreats++;
                    State = LearnerState.Returning;
                    X = Mathf.MoveTowards(X, safeEdge, dt * 1.7f);
                }
                else { X = safeEdge; State = LearnerState.NeedsComfort; }
                return;
            }
            var destination = held ? Mathf.Clamp(playerX - 0.8f, X, GoalX) : GoalX;
            var next = Mathf.MoveTowards(X, destination, dt * 2.3f);
            if (X < GapX + 0.65f && next >= GapX - 0.65f)
            {
                if (string.IsNullOrEmpty(bridge))
                {
                    X = Mathf.Min(next, Mathf.Max(X, GapX - 0.65f));
                    State = LearnerState.NeedsBridge;
                    return;
                }
                Crossing = bridge;
            }
            // Stop at a stable threshold instead of stepping forward and snapping
            // back on alternating frames while support is still missing.
            if (next >= 17.8f && X < 21.5f && !comfortable)
            {
                X = Mathf.Max(17.8f, LearnedUntil);
                State = LearnerState.NeedsComfort;
                return;
            }
            if (next >= 18f && X < 21.5f)
            {
                Support = held ? "hand" : toy ? "toy" : light ? "lamp" : "learned";
                if (held || light || toy) practicedUntil = Mathf.Max(practicedUntil, Mathf.Min(next, 21.5f));
                if (next >= 21.5f) { passedDark = true; Independent = !held; }
            }
            X = next;
            State = X >= GoalX - 0.01f ? LearnerState.Arrived : LearnerState.Walking;
        }
    }
}
