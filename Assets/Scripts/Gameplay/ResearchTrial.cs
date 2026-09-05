using UnityEngine;

namespace LetGo
{
    public enum ResearchTrialState { NotStarted, Approaching, NeedsCompany, NeedsSpace, Continuing, Arrived }

    /// <summary>A reversible experiment: one model needs company, the other room to try.</summary>
    public sealed class ResearchTrial
    {
        private float startX, endX, pauseX, settling;
        private bool company;
        private readonly bool[] crossed = new bool[2];
        public ResearchTrialState State { get; private set; }
        public float PositionX { get; private set; }
        public float Progress01 => Mathf.InverseLerp(startX, endX, PositionX);
        public float Response01 => Mathf.Clamp01(settling / 0.8f);
        public int Revision { get; private set; }
        public bool WaitingForBridge { get; private set; }
        public float RequiredBridgeX { get; private set; }
        public float FirstGapX => Mathf.Lerp(startX, endX, 0.22f);
        public float SecondGapX => Mathf.Lerp(startX, endX, 0.77f);

        public void Begin(float start, float end, bool needsCompany)
        {
            startX = start;
            endX = end;
            pauseX = Mathf.Lerp(start, end, 0.45f);
            PositionX = start;
            company = needsCompany;
            settling = 0f;
            crossed[0] = crossed[1] = false;
            WaitingForBridge = false;
            Revision++;
            State = ResearchTrialState.Approaching;
        }

        public void Tick(float seconds, float playerX, bool standingStill, bool handsFree, float bridgeX, bool bridgePlaced)
        {
            if (State == ResearchTrialState.NotStarted || State == ResearchTrialState.Arrived) return;
            if (State == ResearchTrialState.Approaching || State == ResearchTrialState.Continuing)
            {
                var destination = State == ResearchTrialState.Approaching ? pauseX : endX;
                var next = Mathf.MoveTowards(PositionX, destination, 1.6f * seconds);
                WaitingForBridge = false;
                for (var i = 0; i < 2; i++)
                {
                    var gap = i == 0 ? FirstGapX : SecondGapX;
                    if (crossed[i] || next < gap - 0.45f) continue;
                    if (!bridgePlaced || Mathf.Abs(bridgeX - gap) > 0.75f)
                    {
                        WaitingForBridge = true;
                        RequiredBridgeX = gap;
                        next = Mathf.Min(next, Mathf.Max(PositionX, gap - 0.45f));
                        break;
                    }
                    if (next >= gap + 0.45f) crossed[i] = true;
                }
                PositionX = next;
                if (Mathf.Abs(PositionX - destination) < 0.001f)
                    State = State == ResearchTrialState.Continuing ? ResearchTrialState.Arrived :
                        company ? ResearchTrialState.NeedsCompany : ResearchTrialState.NeedsSpace;
                return;
            }
            var responding = handsFree && (company ? standingStill && Mathf.Abs(playerX - PositionX) <= 1.7f :
                playerX >= PositionX + 2.2f);
            settling = responding ? settling + seconds : 0f;
            if (settling >= 0.8f) State = ResearchTrialState.Continuing;
        }
    }
}
