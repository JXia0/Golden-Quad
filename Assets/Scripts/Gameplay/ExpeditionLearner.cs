using UnityEngine;

namespace LetGo
{
    public enum LearnerState { Waiting, Walking, NeedsBridge, NeedsComfort, Returning, Arrived, Recalling, Pausing }

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
        private DemonstratedAction[] lesson = System.Array.Empty<DemonstratedAction>();
        private System.Collections.Generic.IReadOnlyList<DemonstratedAction> lessonView = System.Array.Empty<DemonstratedAction>();
        private bool[] usedActions = System.Array.Empty<bool>();
        private float pauseRemaining, jumpRemaining, jumpDuration, jumpStart, jumpEnd, jumpHeight;
        private bool pauseSteadies, steady, hasHelpHabit;
        private DemonstratedAction pausedAction;
        public int Demonstrations { get; private set; }
        public bool HasDemonstration => Demonstrations > 0;
        public float Height { get; private set; }
        public float PauseRemaining => pauseRemaining;
        public LearnedHabitSnapshot ExecutedHabit { get; private set; }
        public System.Collections.Generic.IReadOnlyList<DemonstratedAction> Lesson => lessonView;

        public void Teach(System.Collections.Generic.IReadOnlyList<DemonstratedAction> actions)
        {
            lesson = new DemonstratedAction[actions.Count];
            usedActions = new bool[actions.Count];
            lessonView = System.Array.AsReadOnly(lesson);
            hasHelpHabit = false;
            for (var i = 0; i < actions.Count; i++)
            {
                lesson[i] = actions[i];
                if (actions[i].Kind == LearnedHabitKind.SeekHelp) hasHelpHabit = true;
            }
            Demonstrations++;
            ResetHabitTrial();
        }

        private void ResetHabitTrial()
        {
            System.Array.Clear(usedActions, 0, usedActions.Length);
            pauseRemaining = jumpRemaining = Height = 0f;
            steady = pauseSteadies = false;
            ExecutedHabit = default;
        }

        public void Recall()
        {
            if (State == LearnerState.Recalling || X <= 12.01f) return;
            LearnedUntil = Mathf.Max(LearnedUntil, practicedUntil);
            Rehearsals++;
            passedDark = Independent = false;
            Support = Crossing = "";
            Running = true;
            State = LearnerState.Recalling;
            ResetHabitTrial();
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
            if (TickHabit(dt)) return;
            var comfortable = held || light || toy || passedDark || steady ||
                !hasHelpHabit && (LearnedUntil >= 21.5f || X < LearnedUntil - 0.01f);
            if (X >= 18f && X < 21.5f && !comfortable)
            {
                var safeEdge = hasHelpHabit ? 17.8f : Mathf.Max(17.8f, LearnedUntil);
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
                    if (TryLearnedJump()) return;
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
                X = hasHelpHabit ? 17.8f : Mathf.Max(17.8f, LearnedUntil);
                State = LearnerState.NeedsComfort;
                return;
            }
            if (next >= 18f && X < 21.5f)
            {
                Support = held ? "hand" : toy ? "toy" : light ? "lamp" : steady ? "breath" : "learned";
                if (hasHelpHabit && (held || light || toy))
                    Remember(LearnedHabitKind.SeekHelp, 0f, 17.8f, Support);
                if (held || light || toy) practicedUntil = Mathf.Max(practicedUntil, Mathf.Min(next, 21.5f));
                if (next >= 21.5f) { passedDark = true; Independent = !held; }
            }
            X = next;
            State = X >= GoalX - 0.01f ? LearnerState.Arrived : LearnerState.Walking;
        }

        private bool TickHabit(float dt)
        {
            if (jumpRemaining > 0f)
            {
                jumpRemaining = Mathf.Max(0f, jumpRemaining - dt);
                var t = 1f - jumpRemaining / jumpDuration;
                X = Mathf.Lerp(jumpStart, jumpEnd, t);
                Height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                State = LearnerState.Walking;
                if (jumpRemaining <= 0f) Height = 0f;
                return true;
            }
            if (pauseRemaining > 0f)
            {
                pauseRemaining = Mathf.Max(0f, pauseRemaining - dt);
                State = LearnerState.Pausing;
                if (pauseRemaining <= 0f)
                {
                    if (pauseSteadies) steady = true;
                    Remember(LearnedHabitKind.Pause, pausedAction.Seconds, pausedAction.X, "");
                }
                return true;
            }
            for (var i = 0; i < lesson.Length; i++)
            {
                var action = lesson[i];
                if (usedActions[i] || action.Kind != LearnedHabitKind.Pause) continue;
                var triggerX = action.SteadiesInDark ? Mathf.Min(action.X, 17.8f) : action.X;
                if (X < triggerX - 0.12f) continue;
                usedActions[i] = true;
                pausedAction = action;
                pauseRemaining = action.Seconds;
                pauseSteadies = action.SteadiesInDark;
                State = LearnerState.Pausing;
                return true;
            }
            return false;
        }

        private bool TryLearnedJump()
        {
            for (var i = 0; i < lesson.Length; i++)
            {
                var action = lesson[i];
                if (usedActions[i] || !action.CrossesGap) continue;
                usedActions[i] = true;
                jumpStart = X;
                jumpEnd = GapX + 0.7f;
                jumpDuration = jumpRemaining = Mathf.Clamp(action.Seconds * 0.65f, 0.5f, 0.95f);
                jumpHeight = Mathf.Clamp(action.Height * 0.4f, 0.35f, 0.7f);
                Crossing = "jump";
                State = LearnerState.Walking;
                Remember(LearnedHabitKind.Jump, 0f, action.X, "");
                return true;
            }
            return false;
        }

        private void Remember(LearnedHabitKind kind, float seconds, float x, string help)
        {
            // Show the most consequential action from this trial in the farewell.
            if ((int)kind < (int)ExecutedHabit.Kind) return;
            ExecutedHabit = new LearnedHabitSnapshot(kind, seconds, x, help, Demonstrations);
        }
    }
}
