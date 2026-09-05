using UnityEngine;

namespace LetGo
{
    public enum LearnerState { Waiting, Walking, NeedsBridge, NeedsComfort, Returning, Arrived }

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
        public void Start() => Running = true;

        public void Tick(float dt, string bridge, bool held, float playerX, bool light, bool toy)
        {
            if (held) Running = true;
            if (!Running || State == LearnerState.Arrived) return;
            var comfortable = held || light || toy || passedDark;
            if (X >= 18f && X < 21.5f && !comfortable)
            {
                if (State != LearnerState.Returning) Retreats++;
                State = LearnerState.Returning;
                X = Mathf.MoveTowards(X, 17.8f, dt * 1.7f);
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
                X = 17.8f;
                State = LearnerState.NeedsComfort;
                return;
            }
            if (next >= 18f && X < 21.5f)
            {
                Support = held ? "hand" : toy ? "toy" : "lamp";
                if (next >= 21.5f) { passedDark = true; Independent = !held; }
            }
            X = next;
            State = X >= GoalX - 0.01f ? LearnerState.Arrived : LearnerState.Walking;
        }
    }
}
