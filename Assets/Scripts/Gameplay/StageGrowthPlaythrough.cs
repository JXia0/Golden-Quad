#if UNITY_EDITOR
using System.Collections;
using LetGo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed partial class JourneyPlaythroughDriver
{
    private IEnumerator ReviewStageSpatialJourney()
    {
        phase = "the performer enters an open stage and reaches the microphone";
        SceneManager.LoadScene("03_Stage");
        yield return Keys(1.4f);
        Capture("03-final-stage-backstage-entry");
        yield return Keys(0.3f);
        yield return Walk(0f);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1,
            "the revised open stage keeps the first real breath playable")) yield break;
        yield return SaveMemoryReviewCapture("03-final-stage-first-breath");
        if (finished) yield break;
        yield return Walk(5f);
        yield return Note(false, false);
        yield return Note(true, true);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 2,
            "the performer composes the phrase on the visible main stage")) yield break;
        var microphone = GameObject.Find("Stage Microphone")?.GetComponent<SpriteRenderer>();
        var mark = GameObject.Find("Steady Route");
        if (!Check(microphone != null && microphone.enabled && mark != null &&
            Mathf.Abs(microphone.bounds.center.x - mark.transform.position.x) < 1.2f &&
            Mathf.Abs(microphone.bounds.min.y + 2.72f) < 0.08f,
            "the microphone stands on the floor beside the actual composing position")) yield break;
        var openStage = true;
        var stageView = Camera.main;
        if (!Check(stageView != null && stageView.orthographic,
            "the curtain review reads the actual orthographic game camera")) yield break;
        var cameraTop = stageView.transform.position.y + stageView.orthographicSize;
        var wingsReachAboveView = true;
        var hemsMeetFloor = true;
        foreach (var name in new[] { "Curtain Left", "Curtain Right" })
        {
            var curtain = GameObject.Find(name);
            if (curtain == null) { openStage = false; continue; }
            var visibleCloth = 0;
            foreach (var cloth in curtain.GetComponentsInChildren<SpriteRenderer>(true))
                if (cloth.enabled && cloth.gameObject.activeInHierarchy)
                {
                    visibleCloth++;
                    // Leave the final x=10 cue and its entire interaction radius open.
                    openStage &= cloth.bounds.max.x < -1.5f || cloth.bounds.min.x > 11.8f;
                    wingsReachAboveView &= cloth.sprite != null && cloth.bounds.max.y > cameraTop + 0.05f;
                    hemsMeetFloor &= Mathf.Abs(cloth.bounds.min.y + 2.72f) <= 0.1f;
                }
            openStage &= visibleCloth > 0;
        }
        if (!Check(openStage, "the painted side curtains leave all performance positions and the microphone in an open auditorium view")) yield break;
        if (!Check(wingsReachAboveView && hemsMeetFloor,
            "the visible painted curtain tops extend above the actual camera while their hems meet the stage floor")) yield break;
        yield return SaveMemoryReviewCapture("03-final-stage-microphone-performance");
        if (finished) yield break;
        yield return Walk(10f);
        var performance = FindAnyObjectByType<StagePerformance>();
        var feedback = FindAnyObjectByType<StageFeedbackPresentation>();
        if (!Check(performance != null && feedback != null,
            "the final speaking position has its real audience relay and sound presentation")) yield break;
        yield return Keys(0.2f, Key.F);
        yield return Keys(0.2f);
        var handoffDeadline = Time.time + 8f;
        while (!performance.ReadyForCurtain && Time.time < handoffDeadline) yield return Keys(0.1f);
        if (!Check(performance.ReadyForCurtain && !performance.AudienceLeading &&
            performance.FinishedRelay.Outcome == StageRelayOutcome.Accepted &&
            StorySceneDirector.Instance.CompletedObjectives == 2,
            "the final speaking position accepts the audience's own phrase and leaves the bow immediately available")) yield break;
        var audienceEncoreNotes = 0;
        var firstEncoreSinger = -1;
        performance.NoteReleased += (beat, isLong) =>
        {
            if (!performance.LastNoteWasAudience) return;
            if (audienceEncoreNotes == 0) firstEncoreSinger = performance.LastSingingListener;
            audienceEncoreNotes++;
        };
        var playedBeforeEncore = feedback.PlayedNotes;
        var finishedPhrase = performance.FinishedRelay;
        yield return Keys(0.2f, Key.F);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 3 && performance.ReadyForCurtain &&
            performance.Relay.EncorePhase == StageEncorePhase.WaitingForSpace && performance.IndependentAudienceNotes == 0,
            "F bows before any encore, completes all stage objectives and preserves the room's opportunity to continue")) yield break;
        yield return SaveMemoryReviewCapture("03-final-stage-bow-before-wing");
        if (finished) yield break;
        phase = "the audience continues after the performer bows and steps away";
        yield return Walk(performance.RelayOrigin.x + 1.85f);
        if (!Check(Player.transform.position.x - performance.RelayOrigin.x >= 1.7f &&
            StorySceneDirector.Instance.CompletedObjectives == 3,
            "the performer can leave the singing spot after the bow without another stage objective")) yield break;
        Capture("03-final-stage-independent-encore-start");
        var encoreDeadline = Time.time + 5f;
        while (!performance.AudienceContinuedWithoutPlayer && Time.time < encoreDeadline) yield return Keys(0.1f);
        if (!Check(performance.AudienceContinuedWithoutPlayer && performance.IndependentAudienceNotes == 2 &&
            audienceEncoreNotes == 2 && feedback.PlayedNotes == playedBeforeEncore + 2 &&
            firstEncoreSinger == finishedPhrase.SecondListener &&
            StorySceneDirector.Instance.CompletedObjectives == 3 &&
            performance.FinishedRelay.Outcome == finishedPhrase.Outcome,
            "after the bow, the former listener leads two actual audible audience notes while all three objectives stay complete")) yield break;
        yield return SaveMemoryReviewCapture("03-final-stage-independent-encore-finished");
        if (finished) yield break;
        yield return Walk(15.8f);
        yield return SaveMemoryReviewCapture("03-final-stage-enter-exit-wing");
        if (finished) yield break;
        yield return UseDoor("Back Curtain", "04_Interlude_Growing");
    }

    private IEnumerator ReviewStageGrowth()
    {
        JourneyChoices.Reset();
        phase = "stage growth accepts another voice";
        yield return ComposeStageGrowthPhrase();
        if (finished) yield break;
        var performance = FindAnyObjectByType<StagePerformance>();
        var feedback = FindAnyObjectByType<StageFeedbackPresentation>();
        if (!Check(performance != null && feedback != null, "the authored stage loads its playable performance and sound presentation")) yield break;

        var preparedListener = -1;
        var proposedLong = true;
        var offeredPosition = Vector3.zero;
        var audienceNotes = 0;
        var playerNotes = 0;
        var playedBefore = feedback.PlayedNotes;
        performance.AudiencePreparing += (listener, isLong, position) =>
        {
            preparedListener = listener;
            proposedLong = isLong;
            offeredPosition = position;
        };
        performance.NoteReleased += (beat, isLong) =>
        {
            if (performance.LastNoteWasAudience) audienceNotes++;
            else playerNotes++;
        };
        yield return Keys(0.15f, Key.F);
        yield return WaitForStageGrowthOffer(performance);
        if (finished) yield break;
        if (!Check(performance.AudienceLeading && preparedListener == 1 && !proposedLong &&
            audienceNotes == 1 && !performance.ReadyForCurtain,
            "after the first audience note, the other bank offers a short variation of the player's long ending")) yield break;
        Capture("03-growth-other-voice-waits");

        var origin = performance.RelayOrigin;
        var audience = GameObject.Find("Right audience answer")?.GetComponent<SpriteRenderer>();
        if (!Check(audience != null, "the responding audience bank has an actual scene presentation")) yield break;
        var audienceX = audience.transform.position.x;
        var offeredX = offeredPosition.x;
        // Move away from the waiting right bank, leaving it space instead of triggering encouragement.
        yield return Walk(origin.x - 0.65f);
        yield return Keys(0.15f);
        if (!Check(Vector3.Distance(performance.RelayOrigin, origin) < 0.001f &&
            Mathf.Abs(audience.transform.position.x - audienceX) < 0.01f &&
            Mathf.Abs(offeredPosition.x - offeredX) < 0.001f &&
            Player.transform.position.x < origin.x - 0.4f,
            "walking away moves the performer while the offered voice and audience bank stay in their world positions")) yield break;
        Capture("03-growth-listen-without-leading");
        yield return Keys(6.3f);
        if (!Check(performance.ReadyForCurtain && !StorySceneDirector.Instance.ObjectivesComplete &&
            performance.FinishedRelay.Outcome == StageRelayOutcome.Accepted &&
            JourneyChoices.StageRelay.Outcome == StageRelayOutcome.Accepted &&
            JourneyChoices.HasStageRhythm && !JourneyChoices.FirstBeatLong && !JourneyChoices.SecondBeatLong,
            "listening accepts the actually sung short-short variation and still leaves the bow to the player")) yield break;
        if (!Check(audienceNotes == 2 && playerNotes == 0 && feedback.PlayedNotes == playedBefore + 2 &&
            JourneyChoices.StageRelay.SecondListener == 1,
            "both accepted notes are produced by the audience without inventing a player response")) yield break;
        var acceptedSecondLong = JourneyChoices.SecondBeatLong;
        Capture("03-growth-accepted-ending");
        yield return Keys(0.15f, Key.F);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete, "an explicit bow completes the accepted audience performance")) yield break;

        phase = "stage growth sustains a shared voice";
        yield return ComposeStageGrowthPhrase();
        if (finished) yield break;
        performance = FindAnyObjectByType<StagePerformance>();
        audienceNotes = playerNotes = 0;
        performance.NoteReleased += (beat, isLong) =>
        {
            if (performance.LastNoteWasAudience) audienceNotes++;
            else playerNotes++;
        };
        yield return Keys(0.15f, Key.F);
        yield return WaitForStageGrowthOffer(performance);
        if (finished) yield break;
        yield return Keys(1f, Key.E, Key.D);
        if (!Check(Hand.IsSelfAnchoring && performance.Relay.Phase == StageRelayPhase.OfferedVoice,
            "holding a supportive breath keeps the waiting listener's response window open")) yield break;
        Capture("03-growth-shared-breath");
        yield return Keys(0.2f);
        if (!Check(performance.Relay.Outcome == StageRelayOutcome.Shared && performance.Relay.SecondLong &&
            performance.Relay.PlayerResponses == 1 && audienceNotes == 2 && playerNotes == 1,
            "releasing a sustained breath toward the waiting bank produces a joint long answer and an actual player voice")) yield break;
        Capture("03-growth-joint-answer");
        yield return Keys(1.6f);
        if (!Check(performance.ReadyForCurtain && !StorySceneDirector.Instance.ObjectivesComplete &&
            JourneyChoices.StageRelay.Outcome == StageRelayOutcome.Shared && JourneyChoices.SecondBeatLong &&
            JourneyChoices.SecondBeatLong != acceptedSecondLong,
            "singing with the listener preserves a different final rhythm from letting its short variation stand")) yield break;
        yield return Keys(0.15f, Key.F);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete, "the shared performance also waits for the player's explicit bow")) yield break;

        phase = "stage growth takes the lead back";
        yield return ComposeStageGrowthPhrase();
        if (finished) yield break;
        performance = FindAnyObjectByType<StagePerformance>();
        yield return Keys(0.15f, Key.F);
        yield return WaitForStageGrowthOffer(performance);
        if (finished) yield break;
        yield return Keys(0.15f, Key.F);
        yield return Keys(0.2f);
        if (!Check(!performance.AudienceLeading && !performance.ReadyForCurtain &&
            performance.LeadTakebacks == 1 && performance.Relay.Phase == StageRelayPhase.Idle,
            "pressing F during the offered response takes the lead back and cancels the unfinished audience take")) yield break;
        yield return Keys(6.3f);
        if (!Check(!performance.ReadyForCurtain && StorySceneDirector.Instance.CompletedObjectives == 2,
            "an abandoned audience timer cannot finish the performance after the player takes over")) yield break;
        yield return Note(false, false);
        yield return Note(true, true);
        if (!Check(performance.ReadyForCurtain && !StorySceneDirector.Instance.ObjectivesComplete &&
            JourneyChoices.StageRelay.Outcome == StageRelayOutcome.Solo &&
            !JourneyChoices.FirstBeatLong && JourneyChoices.SecondBeatLong &&
            performance.ReprisedBeats == 2,
            "after taking the lead back, actually singing both original notes preserves the player's short-long phrase")) yield break;
        Capture("03-growth-takeback-solo");
        yield return Keys(0.15f, Key.F);
        yield return Keys(0.2f);
        Check(StorySceneDirector.Instance.ObjectivesComplete, "all three ways to finish the stage remain playable through an explicit bow");
    }

    private IEnumerator ComposeStageGrowthPhrase()
    {
        SceneManager.LoadScene("03_Stage");
        yield return Keys(1.4f);
        yield return Walk(0f);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1,
            "the stage growth trial opens with a real full breath and release")) yield break;
        yield return Walk(5f);
        yield return Note(false, false);
        yield return Note(true, true);
        var performance = FindAnyObjectByType<StagePerformance>();
        Check(StorySceneDirector.Instance.CompletedObjectives == 2 && performance != null &&
            performance.ComposedBeats == 2 && !performance.FirstBeatLong && performance.SecondBeatLong &&
            performance.AddressedListener(0) == 0 && performance.AddressedListener(1) == 1,
            "real E and direction inputs compose the same short-left long-right phrase before each decision");
    }

    private IEnumerator WaitForStageGrowthOffer(StagePerformance performance)
    {
        var deadline = Time.time + 3f;
        while (performance != null && performance.Relay.Phase != StageRelayPhase.OfferedVoice && Time.time < deadline)
            yield return Keys(0.06f);
        Check(performance != null && performance.Relay.Phase == StageRelayPhase.OfferedVoice,
            "the audience handoff reaches its observable response window");
    }
}
#endif
