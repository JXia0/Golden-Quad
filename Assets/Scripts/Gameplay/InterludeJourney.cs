using UnityEngine;

namespace LetGo
{
    public enum FirstsPhase { Riding, Walking, RaisingHand, Ready }

    public sealed class FirstsJourney
    {
        public static readonly float[] Stops = { 0.25f, 0.55f, 0.85f };
        public FirstsPhase Phase { get; private set; }
        public float Position { get; private set; } = 0.02f;
        public int RequestedStop { get; private set; } = -1;
        public int ExitStop { get; private set; } = -1;
        public float RaisedHand { get; private set; }
        public void Bell()
        {
            if (Phase != FirstsPhase.Riding) return;
            if (RequestedStop >= 0) { RequestedStop = -1; return; }
            RequestedStop = 0;
            for (var i = 0; i < Stops.Length; i++)
                if (Stops[i] > Position + 0.005f) { RequestedStop = i; break; }
        }
        public void Tick(float dt, float move, bool hold, bool release)
        {
            if (Phase == FirstsPhase.Riding)
            {
                var next = Position + dt * 0.1f;
                if (RequestedStop >= 0 && ((Position <= Stops[RequestedStop] && next >= Stops[RequestedStop]) ||
                    (next >= 1f && next - 1f >= Stops[RequestedStop])))
                {
                    Position = Stops[RequestedStop]; ExitStop = RequestedStop;
                    Phase = FirstsPhase.Walking;
                }
                else Position = Mathf.Repeat(next, 1f);
            }
            else if (Phase == FirstsPhase.Walking)
            {
                Position = Mathf.Clamp01(Position + move * dt * 0.2f);
                if (Mathf.Abs(Position - Stops[1]) < 0.025f)
                {
                    Position = Stops[1];
                    Phase = FirstsPhase.RaisingHand;
                }
            }
            else if (Phase == FirstsPhase.RaisingHand)
            {
                if (release && RaisedHand >= 0.98f) Phase = FirstsPhase.Ready;
                else RaisedHand = Mathf.Clamp01(RaisedHand + dt * (hold ? 0.85f : -0.7f));
            }
        }
    }

    public sealed class RepairJourney
    {
        public readonly float[] Pieces = { 0.78f, 0.12f, 0.43f };
        public readonly bool[] Placed = new bool[3];
        public int Selected { get; private set; }
        public bool Holding { get; private set; }
        public int RepairedCount { get; private set; }
        public float LetterPosition { get; private set; } = 0.5f;
        public string Destination { get; private set; }
        public bool Ready => !string.IsNullOrEmpty(Destination);
        public static float Target(int index) => 0.25f + index * 0.25f;
        public void Tick(float dt, float move, int select, bool press, bool held, bool release)
        {
            if (Ready) return;
            if (!Holding && select != 0 && RepairedCount < 3) Selected = (Selected + select + 3) % 3;
            if (press && (RepairedCount == 3 || !Placed[Selected])) Holding = true;
            if (Holding && held)
            {
                if (RepairedCount == 3) LetterPosition = Mathf.Clamp01(LetterPosition + move * dt * 0.35f);
                else Pieces[Selected] = Mathf.Clamp01(Pieces[Selected] + move * dt * 0.35f);
            }
            if (!Holding || !release) return;
            Holding = false;
            if (RepairedCount == 3)
            {
                if (Mathf.Abs(LetterPosition - 0.2f) < 0.12f) Destination = "home";
                else if (Mathf.Abs(LetterPosition - 0.8f) < 0.12f) Destination = "notebook";
            }
            else if (Mathf.Abs(Pieces[Selected] - Target(Selected)) < 0.065f)
            {
                Pieces[Selected] = Target(Selected);
                Placed[Selected] = true;
                RepairedCount++;
            }
        }
    }
}
