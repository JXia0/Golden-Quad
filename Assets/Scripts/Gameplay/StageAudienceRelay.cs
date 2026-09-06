namespace LetGo
{
    public enum StageRelayPhase { Idle, FirstVoice, OfferedVoice, Answering, Complete }
    public enum StageEncorePhase { None, WaitingForSpace, FirstVoice, SecondVoice, Complete }

    /// <summary>A listener offers a variation. Leaving space, giving a cue and singing along are different responses.</summary>
    public sealed class StageAudienceRelay
    {
        public StageRelayPhase Phase { get; private set; }
        public StageRelayOutcome Outcome { get; private set; }
        public int FirstListener { get; private set; }
        public int Receiver => 1 - FirstListener;
        public bool FirstLong { get; private set; }
        public bool ProposedLong { get; private set; }
        public bool SecondLong { get; private set; }
        public int PlayerResponses { get; private set; }
        public float PhaseSeconds { get; private set; }
        public float ResponseWindow => 3.2f;
        public StageEncorePhase EncorePhase { get; private set; }
        public float EncorePhaseSeconds { get; private set; }
        public float EncoreSpacingSeconds { get; private set; }
        public int EncoreNotesSung { get; private set; }
        public int EncoreLeader => Receiver;
        public bool EncoreFirstLong => SecondLong;
        public bool EncoreSecondLong => FirstLong;
        public bool AudienceContinuedWithoutPlayer => EncorePhase == StageEncorePhase.Complete;
        public bool IsRunning => Phase != StageRelayPhase.Idle && Phase != StageRelayPhase.Complete;
        public StageRelaySnapshot Snapshot => new StageRelaySnapshot(Outcome, FirstLong, SecondLong,
            FirstListener, Receiver, PlayerResponses);

        public void Begin(bool firstLong, bool originalSecondLong, int firstListener, float phraseSpacingSeconds = 1.3f)
        {
            CancelEncore();
            FirstLong = firstLong;
            FirstListener = firstListener == 0 ? 0 : 1;
            // The answer is a deliberate variation of the player's phrase, never a random failure.
            ProposedLong = !originalSecondLong;
            SecondLong = ProposedLong;
            PlayerResponses = 0;
            Outcome = StageRelayOutcome.None;
            // Keep the player's phrasing, while leaving enough time to hear each complete note.
            EncoreSpacingSeconds = phraseSpacingSeconds < 0.85f ? 0.85f :
                phraseSpacingSeconds > 2.4f ? 2.4f : phraseSpacingSeconds;
            Enter(StageRelayPhase.FirstVoice);
        }

        public void TickEncore(float seconds, float playerOffsetX, bool playerSinging)
        {
            if (Phase != StageRelayPhase.Complete || seconds <= 0f ||
                EncorePhase == StageEncorePhase.None || EncorePhase == StageEncorePhase.Complete) return;
            if (playerSinging) { CancelEncore(); return; }
            EncorePhaseSeconds += seconds;
            switch (EncorePhase)
            {
                case StageEncorePhase.WaitingForSpace:
                    if (playerOffsetX >= 1.6f || playerOffsetX <= -1.6f)
                    {
                        EncoreNotesSung = 1;
                        EnterEncore(StageEncorePhase.FirstVoice);
                    }
                    break;
                case StageEncorePhase.FirstVoice:
                    if (EncorePhaseSeconds >= EncoreSpacingSeconds)
                    {
                        EncoreNotesSung = 2;
                        EnterEncore(StageEncorePhase.SecondVoice);
                    }
                    break;
                case StageEncorePhase.SecondVoice:
                    if (EncorePhaseSeconds >= NoteSeconds(EncoreSecondLong) + 0.55f)
                        EnterEncore(StageEncorePhase.Complete);
                    break;
            }
        }

        public void CancelEncore()
        {
            EncoreNotesSung = 0;
            EnterEncore(StageEncorePhase.None);
        }

        public void Tick(float seconds, float playerOffsetX, bool preparingResponse)
        {
            if (!IsRunning || seconds <= 0f) return;
            PhaseSeconds += seconds;
            switch (Phase)
            {
                case StageRelayPhase.FirstVoice:
                    if (PhaseSeconds >= NoteSeconds(FirstLong) + 0.55f)
                        Enter(StageRelayPhase.OfferedVoice);
                    break;
                case StageRelayPhase.OfferedVoice:
                    // Moving toward the waiting voice is also encouragement; the player need not sing for it.
                    var towardReceiver = playerOffsetX * (Receiver == 0 ? -1f : 1f);
                    if (!preparingResponse && PhaseSeconds >= 0.85f && towardReceiver >= 1.05f)
                        Answer(StageRelayOutcome.Encouraged, ProposedLong);
                    else if (!preparingResponse && PhaseSeconds >= ResponseWindow)
                        Answer(StageRelayOutcome.Accepted, ProposedLong);
                    break;
                case StageRelayPhase.Answering:
                    if (PhaseSeconds >= NoteSeconds(SecondLong) + 0.55f)
                        Enter(StageRelayPhase.Complete);
                    break;
            }
        }

        public bool Respond(float heldSeconds, int aimedListener, bool closeToReceiver)
        {
            if (Phase != StageRelayPhase.OfferedVoice || heldSeconds < 0.12f ||
                (aimedListener != Receiver && !closeToReceiver)) return false;
            PlayerResponses++;
            var shared = heldSeconds >= 0.7f;
            Answer(shared ? StageRelayOutcome.Shared : StageRelayOutcome.Encouraged,
                shared || ProposedLong);
            return true;
        }

        public void Cancel()
        {
            CancelEncore();
            Outcome = StageRelayOutcome.None;
            Enter(StageRelayPhase.Idle);
        }

        private void Answer(StageRelayOutcome outcome, bool secondLong)
        {
            Outcome = outcome;
            SecondLong = secondLong;
            Enter(StageRelayPhase.Answering);
        }

        private void Enter(StageRelayPhase phase)
        {
            Phase = phase;
            PhaseSeconds = 0f;
            if (phase == StageRelayPhase.Complete) EnterEncore(StageEncorePhase.WaitingForSpace);
        }

        private void EnterEncore(StageEncorePhase phase)
        {
            EncorePhase = phase;
            EncorePhaseSeconds = 0f;
        }

        private static float NoteSeconds(bool isLong) => isLong ? 0.85f : 0.45f;
    }
}
