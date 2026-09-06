using UnityEngine;

namespace LetGo
{
    public enum PaperBridgePhase { Editing, Walking, Blocked, Complete }
    public enum PaperBridgeFailure { None, Gap, Unsupported }
    public enum PaperBridgeCompletion { None, Independent, Held }

    /// <summary>A small paper construction: folded feet stand on the desk; flat paper needs both ends supported.</summary>
    public sealed class PaperBridgeJourney
    {
        public const float LeftBank = 0.10f;
        public const float RightBank = 0.90f;
        public const float StartX = 0.025f;
        public const float FinishX = 0.975f;
        public const float FlatWidth = 0.42f;
        public const float FoldedWidth = 0.27f;
        private const float JoinTolerance = 0.006f;
        private readonly bool[] stable = new bool[3];
        private readonly bool[] unassistedStable = new bool[3];
        private bool neededHand;

        public readonly float[] Positions = { 0.27f, 0.50f, 0.73f };
        public readonly bool[] Folded = new bool[3];
        public PaperBridgePhase Phase { get; private set; }
        public PaperBridgeFailure Failure { get; private set; }
        public PaperBridgeCompletion Completion { get; private set; }
        public int Selected { get; private set; }
        public int AttemptCount { get; private set; }
        public bool Holding { get; private set; }
        public float WalkerX { get; private set; } = StartX;
        public float FailureX { get; private set; } = StartX;
        public bool Ready => Phase == PaperBridgePhase.Complete;
        public bool Independent => Completion == PaperBridgeCompletion.Independent;
        public bool IsStable(int index) => index >= 0 && index < stable.Length && stable[index];
        public bool IsStableWithoutHand(int index) => index >= 0 && index < unassistedStable.Length && unassistedStable[index];
        public float Width(int index) => Folded[index] ? FoldedWidth : FlatWidth;
        public float LeftEdge(int index) => Positions[index] - Width(index) * 0.5f;
        public float RightEdge(int index) => Positions[index] + Width(index) * 0.5f;

        public PaperBridgeJourney() => RecalculateSupport();

        public void Tick(float dt, float move, int select, bool hold, bool foldPressed, bool trialPressed, bool recallPressed)
        {
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            Holding = hold;
            if (recallPressed)
            {
                Recall();
                RecalculateSupport();
                return;
            }

            if (Phase == PaperBridgePhase.Editing || Phase == PaperBridgePhase.Blocked)
            {
                if (!hold && select != 0) Selected = (Selected + select + Positions.Length) % Positions.Length;
                if (hold && Mathf.Abs(move) > 0.01f)
                    Positions[Selected] = Mathf.Clamp(Positions[Selected] + move * dt * 0.22f, 0.12f, 0.88f);
                if (foldPressed) Folded[Selected] = !Folded[Selected];
                if (trialPressed)
                {
                    WalkerX = StartX;
                    Failure = PaperBridgeFailure.None;
                    Completion = PaperBridgeCompletion.None;
                    neededHand = false;
                    AttemptCount++;
                    Phase = PaperBridgePhase.Walking;
                }
            }
            RecalculateSupport();
            if (Phase != PaperBridgePhase.Walking) return;

            // Connect whole support intervals. A low frame rate must not step over a narrow gap.
            var destination = Mathf.Min(FinishX, WalkerX + dt * 0.135f);
            var reachable = ContinuousReach(WalkerX, destination, stable);
            if (ContinuousReach(WalkerX, reachable, unassistedStable) < reachable) neededHand = true;
            WalkerX = reachable;
            if (reachable < destination)
            {
                FailureX = Mathf.Min(destination, reachable + 0.00001f);
                Failure = Covered(FailureX, null) ? PaperBridgeFailure.Unsupported : PaperBridgeFailure.Gap;
                Phase = PaperBridgePhase.Blocked;
                return;
            }
            if (WalkerX >= FinishX)
            {
                Completion = neededHand ? PaperBridgeCompletion.Held : PaperBridgeCompletion.Independent;
                Phase = PaperBridgePhase.Complete;
            }
        }

        private void Recall()
        {
            Phase = PaperBridgePhase.Editing;
            Failure = PaperBridgeFailure.None;
            Completion = PaperBridgeCompletion.None;
            WalkerX = StartX;
            neededHand = false;
        }

        private void RecalculateSupport()
        {
            SolveSupport(unassistedStable, false);
            SolveSupport(stable, Holding);
        }

        private void SolveSupport(bool[] result, bool withHand)
        {
            for (var i = 0; i < result.Length; i++) result[i] = Folded[i] || (withHand && i == Selected);
            // Iteration only grows from a bank, a folded foot, or a hand. Two unsupported sheets cannot support each other.
            for (var pass = 0; pass < result.Length; pass++)
            for (var i = 0; i < result.Length; i++)
                if (!result[i] && EndpointSupported(LeftEdge(i), i, result) && EndpointSupported(RightEdge(i), i, result))
                    result[i] = true;
        }

        private bool EndpointSupported(float x, int ownIndex, bool[] supported)
        {
            if (x <= LeftBank + JoinTolerance || x >= RightBank - JoinTolerance) return true;
            for (var i = 0; i < Positions.Length; i++)
                if (i != ownIndex && supported[i] && x >= LeftEdge(i) - JoinTolerance && x <= RightEdge(i) + JoinTolerance)
                    return true;
            return false;
        }

        private bool Covered(float x, bool[] supported)
        {
            if (x <= LeftBank + JoinTolerance || x >= RightBank - JoinTolerance) return true;
            for (var i = 0; i < Positions.Length; i++)
                if ((supported == null || supported[i]) && x >= LeftEdge(i) - JoinTolerance && x <= RightEdge(i) + JoinTolerance)
                    return true;
            return false;
        }

        private float ContinuousReach(float from, float destination, bool[] supported)
        {
            var reached = from;
            // The same expanded endpoints used by Covered define each interval; no extra snapping or tolerance is added here.
            for (var pass = 0; pass < Positions.Length + 2 && reached < destination; pass++)
            {
                var previous = reached;
                if (reached <= LeftBank + JoinTolerance) reached = Mathf.Min(destination, LeftBank + JoinTolerance);
                for (var i = 0; i < Positions.Length; i++)
                    if (supported[i] && LeftEdge(i) - JoinTolerance <= reached && RightEdge(i) + JoinTolerance >= reached)
                        reached = Mathf.Max(reached, Mathf.Min(destination, RightEdge(i) + JoinTolerance));
                if (reached >= RightBank - JoinTolerance) reached = destination;
                if (reached == previous) break;
            }
            return reached;
        }
    }
}
