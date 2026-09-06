using System;
using System.Collections.Generic;
using LetGo;

/// <summary>Behavioural checks for leaving space, offering support and taking the lead back.</summary>
public static class StageRelayChecks
{
    public static List<string> RunChecks()
    {
        var results = new List<string>();
        var relay = new StageAudienceRelay();
        relay.Begin(false, true, 0);
        Run(relay, 6.5f, 0f, false);
        Require(relay.Phase == StageRelayPhase.Complete && relay.Outcome == StageRelayOutcome.Accepted &&
            !relay.Snapshot.SecondLong && relay.Snapshot.FirstListener == 0 && relay.Snapshot.SecondListener == 1 &&
            relay.Snapshot.PlayerResponses == 0, "waiting preserves the new voice and its actual variation");
        results.Add("PASS: giving the audience space produces a new second note sung by the other bank.");

        relay.Begin(true, false, 1);
        Run(relay, 1.6f, 0f, false);
        Require(relay.Phase == StageRelayPhase.OfferedVoice && !relay.Respond(0.3f, 1, false),
            "a cue aimed at the first singer cannot silently encourage the other bank");
        Require(relay.Respond(0.3f, 0, false), "a short cue reaches the waiting listener");
        Run(relay, 1.5f, 0f, false);
        Require(relay.Outcome == StageRelayOutcome.Encouraged && relay.Phase == StageRelayPhase.Complete &&
            relay.SecondLong && relay.PlayerResponses == 1, "encouragement starts the listener's variation early");
        results.Add("PASS: aiming a short response at the waiting listener starts an independent answer; the wrong bank does not.");

        relay.Begin(false, true, 0);
        Run(relay, 1.2f, 0f, false);
        Require(relay.Respond(0.9f, 1, false), "the player can sustain the waiting listener's voice");
        Run(relay, 1.5f, 0f, false);
        Require(relay.Outcome == StageRelayOutcome.Shared && relay.SecondLong && !relay.ProposedLong &&
            relay.Phase == StageRelayPhase.Complete, "singing together changes a short proposal into a sustained note");
        results.Add("PASS: a sustained response creates a joint long note instead of the originally proposed short answer.");

        relay.Begin(false, true, 0);
        Run(relay, 2f, -1.2f, false);
        Require(relay.Phase == StageRelayPhase.OfferedVoice, "walking away must leave the listener's decision open");
        Run(relay, 0.2f, 1.2f, false);
        Require(relay.Outcome == StageRelayOutcome.Encouraged && relay.PlayerResponses == 0,
            "moving close can encourage without speaking for the listener");
        results.Add("PASS: approaching the waiting bank encourages it; walking toward the first singer does not.");

        relay.Begin(false, true, 0);
        Run(relay, 5.5f, 1.2f, true);
        Require(relay.Phase == StageRelayPhase.OfferedVoice && relay.Outcome == StageRelayOutcome.None,
            "the response window must not consume a held breath or start a spatial answer over it");
        Require(relay.Respond(1.2f, 1, false), "a held response remains valid at release");
        relay.Cancel();
        Run(relay, 8f, 0f, false);
        Require(relay.Phase == StageRelayPhase.Idle && !relay.Snapshot.HasPerformance,
            "taking the lead back cannot finish an abandoned audience take");
        relay.Begin(true, false, 1);
        Require(relay.PlayerResponses == 0 && relay.Outcome == StageRelayOutcome.None && relay.Receiver == 0,
            "a fresh handoff cannot inherit earlier support");
        results.Add("PASS: holding protects the response window; cancelling and handing off again clear abandoned outcomes.");

        relay.Begin(false, false, 0, 1.8f);
        Run(relay, 6.5f, 0f, false);
        var finished = relay.Snapshot;
        RunEncore(relay, 8f, 1.5f);
        Require(relay.Phase == StageRelayPhase.Complete && relay.EncorePhase == StageEncorePhase.WaitingForSpace &&
            relay.EncoreNotesSung == 0, "waiting at the singing spot cannot substitute for giving the room space");
        relay.TickEncore(0f, 2f, false);
        Require(relay.EncorePhase == StageEncorePhase.WaitingForSpace, "paused time cannot trigger an encore");
        relay.TickEncore(0.02f, 1.7f, false);
        Require(relay.EncorePhase == StageEncorePhase.FirstVoice && relay.EncoreNotesSung == 1 &&
            relay.EncoreLeader == finished.SecondListener && relay.EncoreFirstLong == finished.SecondLong,
            "the listener who answered must now lead with the note it actually finished");
        RunEncore(relay, 1.5f, 3f);
        Require(relay.EncorePhase == StageEncorePhase.FirstVoice,
            "the encore must keep the performed spacing rather than immediately play both notes");
        RunEncore(relay, 0.4f, 4f);
        Require(relay.EncorePhase == StageEncorePhase.SecondVoice && relay.EncoreNotesSung == 2 &&
            relay.EncoreSecondLong == finished.FirstLong, "the first bank answers its new leader while the player keeps walking");
        RunEncore(relay, 12f, 5f);
        Require(relay.AudienceContinuedWithoutPlayer && relay.EncoreNotesSung == 2 &&
            relay.Snapshot.Outcome == finished.Outcome && relay.Snapshot.PlayerResponses == finished.PlayerResponses &&
            relay.Snapshot.FirstLong == finished.FirstLong && relay.Snapshot.SecondLong == finished.SecondLong,
            "one optional continuation must not loop, add player responses or rewrite the finished performance");
        results.Add("PASS: stepping away starts one independent audience exchange; the former listener leads at the player's actual spacing without changing the completed phrase.");

        relay.Begin(true, true, 1, 20f);
        Run(relay, 6.5f, 0f, false);
        Require(relay.EncoreSpacingSeconds == 2.4f, "a long pause between composed notes remains a bounded encore");
        relay.TickEncore(0.02f, -1.7f, false);
        Require(relay.EncorePhase == StageEncorePhase.FirstVoice && relay.EncoreLeader == 0,
            "giving space on either side can let either bank begin");
        relay.TickEncore(0.02f, -1.7f, true);
        RunEncore(relay, 8f, -3f);
        Require(relay.EncorePhase == StageEncorePhase.None && !relay.AudienceContinuedWithoutPlayer,
            "singing again cancels an optional continuation instead of leaving a second voice over the player");
        relay.Begin(false, true, 0, 0.1f);
        Require(relay.EncoreSpacingSeconds == 0.85f && relay.EncoreNotesSung == 0,
            "a new handoff clears the previous encore and leaves enough time to hear each note");
        Run(relay, 6.5f, 0f, false);
        relay.CancelEncore();
        RunEncore(relay, 8f, 3f);
        Require(relay.Phase == StageRelayPhase.Complete && relay.EncorePhase == StageEncorePhase.None,
            "cancelling only the optional encore leaves the completed handoff ready to bow");
        relay.Cancel();
        RunEncore(relay, 8f, 3f);
        Require(relay.Phase == StageRelayPhase.Idle && relay.EncorePhase == StageEncorePhase.None,
            "taking back the lead cannot leave an independent audience timer alive");
        results.Add("PASS: singing, rewriting or taking the lead back cancels the optional exchange; pause, spacing limits and a fresh handoff preserve control.");
        return results;
    }

    private static void Run(StageAudienceRelay relay, float seconds, float offset, bool holding)
    {
        for (var elapsed = 0f; elapsed < seconds; elapsed += 0.02f) relay.Tick(0.02f, offset, holding);
    }

    private static void RunEncore(StageAudienceRelay relay, float seconds, float offset)
    {
        for (var elapsed = 0f; elapsed < seconds; elapsed += 0.02f) relay.TickEncore(0.02f, offset, false);
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("[LetGo Stage Relay] " + reason);
    }
}
