namespace LetGo
{
    public enum StageRelayOutcome { None, Solo, Accepted, Encouraged, Shared }

    /// <summary>The phrase that was actually finished, including who carried its second voice.</summary>
    public readonly struct StageRelaySnapshot
    {
        public readonly StageRelayOutcome Outcome;
        public readonly bool FirstLong;
        public readonly bool SecondLong;
        public readonly int FirstListener;
        public readonly int SecondListener;
        public readonly int PlayerResponses;

        public bool HasPerformance => Outcome != StageRelayOutcome.None;

        public StageRelaySnapshot(StageRelayOutcome outcome, bool firstLong, bool secondLong,
            int firstListener, int secondListener, int playerResponses)
        {
            Outcome = outcome;
            FirstLong = firstLong;
            SecondLong = secondLong;
            FirstListener = firstListener;
            SecondListener = secondListener;
            PlayerResponses = playerResponses;
        }
    }
}
