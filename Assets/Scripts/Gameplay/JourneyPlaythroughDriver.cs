#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LetGo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public sealed partial class JourneyPlaythroughDriver : MonoBehaviour
{
    private Keyboard keyboard;
    private readonly List<string> results = new();
    private string phase;
    private bool finished;
    private int repairRuns;
    private float started;
    private float heartbeatAt;
    private float previousMaximumDelta;
    private InputSettings.BackgroundBehavior previousBackground;
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    private PlayerController2D Player => StorySceneDirector.Instance == null || StorySceneDirector.Instance.Player == null
        ? null : StorySceneDirector.Instance.Player.GetComponent<PlayerController2D>();
    private HandConnection Hand => Player?.GetComponent<HandConnection>();

    public void Begin()
    {
        Application.runInBackground = true;
        previousBackground = InputSystem.settings.backgroundBehavior;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        // Isolated batch QA must keep receiving its virtual keyboard when the user
        // works in another window. Restore these settings after the run.
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        previousMaximumDelta = Time.maximumDeltaTime;
        // Rendering captures can stall the editor. Bound catch-up so scripted key edges are
        // not delivered after the player has already stepped past a narrow pickup region.
        Time.maximumDeltaTime = 1f / 30f;
        Screen.SetResolution(1280, 720, false);
        keyboard = InputSystem.AddDevice<Keyboard>();
        started = Time.realtimeSinceStartup;
        StartCoroutine(Run());
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup > heartbeatAt)
        {
            heartbeatAt = Time.realtimeSinceStartup + 3f;
            File.WriteAllText("Logs/JourneyPlaythroughProgress.txt", phase + "\n" +
                (Player != null ? Player.transform.position + " objectives=" + StorySceneDirector.Instance.CompletedObjectives : SceneManager.GetActiveScene().name));
        }
        if (!finished && Time.realtimeSinceStartup - started > 600f) Finish("FAIL: timed out during " + phase);
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(1.4f);
        if (SessionState.GetBool("LetGo.QA.FinalAssets", false))
        {
            SessionState.EraseBool("LetGo.QA.FinalAssets");
            yield return ReviewDeliveredVoice();
            if (finished) yield break;
            yield return ReviewKindergartenGrowth();
            if (finished) yield break;
            yield return ReviewStageGrowth();
            if (finished) yield break;
            yield return ReviewStageSpatialJourney();
            if (finished) yield break;
            yield return ReviewFinalMemoryPresentation();
            if (finished) yield break;
            yield return ReviewReportedPresentation();
            if (finished) yield break;
            yield return ReviewEndingPainting();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for delivered recordings, learned play, independent stage voices, presentation and the complete ending film.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.StageSpatial", false))
        {
            SessionState.EraseBool("LetGo.QA.StageSpatial");
            yield return ReviewDeliveredVoice();
            if (finished) yield break;
            yield return ReviewStageSpatialJourney();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for the open stage, microphone, bow and exit wing.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.DeliveredUI", false))
        {
            SessionState.EraseBool("LetGo.QA.DeliveredUI");
            yield return ReviewInlineKeys();
            if (finished) yield break;
            yield return ReviewTitleMenu();
            if (finished) yield break;
            yield return ReviewPauseMenu();
            if (finished) yield break;
            yield return ReviewFinalMemoryPresentation();
            if (finished) yield break;
            yield return ReviewReportedPresentation();
            if (finished) yield break;
            yield return ReviewEndingPainting();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for delivered UI, narration entry, stage coverage, grounded memories and the final painting.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.GrowthRelease", false))
        {
            SessionState.EraseBool("LetGo.QA.GrowthRelease");
            yield return ReviewInlineKeys();
            if (finished) yield break;
            yield return ReviewTitleMenu();
            if (finished) yield break;
            yield return ReviewPauseMenu();
            if (finished) yield break;
            yield return ReviewKindergartenGrowth();
            if (finished) yield break;
            yield return ReviewStageGrowth();
            if (finished) yield break;
            SceneManager.LoadScene("02_Interlude_Firsts");
            yield return Keys(1.2f);
            yield return PlayFirsts();
            if (finished) yield break;
            SceneManager.LoadScene("04_Interlude_Growing");
            yield return Keys(1.2f);
            yield return PlayRepair(true);
            if (finished) yield break;
            SceneManager.LoadScene("04_Interlude_Growing");
            yield return Keys(1.2f);
            yield return PlayRepair(false);
            if (finished) yield break;
            yield return ReviewReportedPresentation();
            if (finished) yield break;
            yield return ReviewEndingPainting();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for the title, growth decisions, free paper bridges, reported placement and farewell depth.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.SceneStaging", false))
        {
            SessionState.EraseBool("LetGo.QA.SceneStaging");
            yield return ReviewContinuousKindergarten();
            if (finished) yield break;
            yield return ReviewSceneStaging();
            if (finished) yield break;
            if (SessionState.GetBool("LetGo.QA.StagingOnly", false))
            {
                SessionState.EraseBool("LetGo.QA.StagingOnly");
                Finish("ALL PLAYTHROUGH CHECKS PASSED for the final authored scene presentation.");
                yield break;
            }
            yield return ReviewLearnedHabits();
            if (finished) yield break;
            yield return ReviewEndingBoundaries();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for staging, visible learning trials, reteaching, music and the final farewell.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.LearnedHabits", false))
        {
            SessionState.EraseBool("LetGo.QA.LearnedHabits");
            yield return ReviewLearnedHabits();
            if (finished) yield break;
            yield return ReviewEndingBoundaries();
            if (finished) yield break;
            if (!SessionState.GetBool("LetGo.QA.FullAfterHabits", false))
            {
                Finish("ALL PLAYTHROUGH CHECKS PASSED for demonstrated habits, meaningful reteaching, music, and the authored farewell.");
                yield break;
            }
            SessionState.EraseBool("LetGo.QA.FullAfterHabits");
            JourneyChoices.Reset();
            SceneManager.LoadScene("00_Prologue");
            yield return Keys(1.4f);
        }
        if (SessionState.GetBool("LetGo.QA.HandPassages", false))
        {
            SessionState.EraseBool("LetGo.QA.HandPassages");
            yield return ReviewContinuousKindergarten();
            if (finished) yield break;
            yield return ReviewPassages();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for continuous kindergarten, hand presentation and reversible passages.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.DeliveredArt", false))
        {
            SessionState.EraseBool("LetGo.QA.DeliveredArt");
            yield return ReviewContinuousKindergarten();
            if (finished) yield break;
            SceneManager.LoadScene("05_Research");
            yield return Keys(1.4f);
            yield return WorkshopHeld();
            if (finished) yield break;
            yield return ExploreAlternative();
            if (finished) yield break;
            yield return ReviewPassages();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for delivered art, hand presentation, workshop routes and reversible passages.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.ContinuousKindergarten", false))
        {
            SessionState.EraseBool("LetGo.QA.ContinuousKindergarten");
            yield return ReviewContinuousKindergarten();
            if (finished) yield break;
            if (!SessionState.GetBool("LetGo.QA.FullAfterKindergarten", false))
            {
                Finish("ALL PLAYTHROUGH CHECKS PASSED for the continuous kindergarten opening and return to the backpack.");
                yield break;
            }
            SessionState.EraseBool("LetGo.QA.FullAfterKindergarten");
            JourneyChoices.Reset();
            SceneManager.LoadScene("00_Prologue");
            yield return Keys(1.4f);
        }
        if (SessionState.GetBool("LetGo.QA.PassagesOnly", false))
        {
            SessionState.EraseBool("LetGo.QA.PassagesOnly");
            yield return ReviewPassages();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for the entrance and reversible cinematic passages.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.TransitionsOnly", false))
        {
            SessionState.EraseBool("LetGo.QA.TransitionsOnly");
            SceneManager.LoadScene("02_Interlude_Firsts");
            yield return Keys(1.2f);
            yield return PlayFirsts();
            if (finished) yield break;
            SceneManager.LoadScene("04_Interlude_Growing");
            yield return Keys(1.2f);
            yield return PlayRepair(true);
            if (finished) yield break;
            yield return WorkshopHeld();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for playable transitions and research guidance.");
            yield break;
        }
        if (SessionState.GetBool("LetGo.QA.WorkshopOnly", false))
        {
            SessionState.EraseBool("LetGo.QA.WorkshopOnly");
            JourneyChoices.Reset();
            SceneManager.LoadScene("05_Research");
            yield return Keys(1.4f);
            phase = "workshop crate and held crossing";
            yield return WorkshopHeld();
            if (finished) yield break;
            yield return ExploreAlternative();
            if (finished) yield break;
            Finish("ALL PLAYTHROUGH CHECKS PASSED for workshop fixtures using real keyboard events and Unity physics.");
            yield break;
        }
        phase = "prologue door";
        if (FindAnyObjectByType<MainMenuController>()?.Visible == true)
        {
            yield return Keys(0.15f, Key.Enter);
            yield return Keys(0.8f);
        }
        yield return UseDoor("Meeting Door", "01_Kindergarten");
        if (finished) yield break;
        phase = "kindergarten parent release";
        var escort = FindAnyObjectByType<ParentEscort>();
        var parent = EmotionalJourney.Target("parent");
        var childStart = Player.transform.position.x;
        yield return Keys(2.5f, Key.E);
        if (!Check(escort != null && escort.IsLeading && Player.transform.position.x > childStart + 0.8f &&
            parent.transform.position.x > Player.transform.position.x,
            "holding E alone lets the parent lead the child toward school")) yield break;
        Capture("01-parent-leads");
        yield return Keys(0.2f);
        var stoppedParent = parent.transform.position.x;
        var stoppedChild = Player.transform.position.x;
        yield return Keys(0.5f);
        if (!Check(Mathf.Abs(parent.transform.position.x - stoppedParent) < 0.02f &&
            Mathf.Abs(Player.transform.position.x - stoppedChild) < 0.02f,
            "releasing the hand stops both people without requiring a movement key")) yield break;
        yield return Keys(0.4f, Key.A);
        if (!Check(Player.transform.position.x < stoppedChild - 0.5f,
            "after release the child's movement belongs to the player again")) yield break;
        yield return Keys(0.4f, Key.D);
        yield return Keys(0.2f, Key.E);
        if (!Check(escort.ReturnsToHand == 1, "the child can reach back and receive help again")) yield break;
        for (var i = 0; i < 60 && !escort.WaitingAtDoor; i++) yield return Keys(0.2f, Key.E);
        if (!Check(escort.WaitingAtDoor && StorySceneDirector.Instance.CompletedObjectives == 0,
            "the parent stops at the doorway and waits for the child's own step")) yield break;
        Capture("01-parent-waits");
        yield return Keys(0.15f);
        yield return Walk(-1.25f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1, "after parental help the child crosses the doorway with an empty hand")) yield break;
        yield return Keys(0.25f, Key.E);
        yield return Walk(1.8f, true);
        yield return Keys(0.25f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 2, "bag stays on the chair")) yield break;
        phase = "kindergarten toy and obstacles";
        // The chair and blocks are real obstacles; cross them with actual jump input.
        yield return Keys(0.42f, Key.D, Key.Space);
        yield return Walk(4.8f);
        yield return Keys(0.6f);
        yield return Keys(0.55f, Key.D, Key.Space);
        yield return Walk(8f);
        yield return Keys(0.2f, Key.E);
        yield return Walk(9.3f, true);
        yield return Keys(0.2f);
        var toyPlay = FindAnyObjectByType<ComfortToyPlay>();
        if (!Check(toyPlay.Exchanges == 1 && StorySceneDirector.Instance.CompletedObjectives == 2,
            "the child rolls the first toy back instead of completing a delivery")) yield break;
        Capture("01-toy-return");
        yield return Keys(0.8f, Key.E);
        yield return Keys(0.25f);
        if (!Check(toyPlay.Exchanges == 2 && StorySceneDirector.Instance.CompletedObjectives == 3,
            "catching and rolling back turns the toy into a shared game")) yield break;
        phase = "kindergarten companion pace";
        yield return Keys(1.1f, Key.E);
        yield return Keys(1.4f, Key.E, Key.D);
        var child = EmotionalJourney.Target("crying-child");
        if (!Check(child.IsWaiting, "running ahead makes the child stop")) yield break;
        yield return Walk(child.transform.position.x + 1.3f, true);
        yield return Keys(1f, Key.E);
        if (!Check(!child.IsWaiting, "returning and waiting reassures the child")) yield break;
        var teacherX = GameObject.Find("Teacher Safe Area").transform.position.x;
        for (var i = 0; i < 60 && child.transform.position.x < teacherX - 1.2f; i++)
        {
            yield return Keys(0.14f, Key.E, Key.D);
            yield return Keys(0.23f, Key.E);
        }
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete, "child is released into the safe area")) yield break;
        var childX = child.transform.position.x;
        yield return Keys(0.8f);
        if (!Check(child.transform.position.x > childX, "child takes independent steps after release")) yield break;

        phase = "reconsider the childhood toy";
        yield return Walk(child.transform.position.x);
        yield return Keys(0.3f, Key.E);
        var childhoodToy = EmotionalJourney.Target("comfort-toy");
        if (!Check(Hand.CurrentTarget == childhoodToy && !childhoodToy.IsPlaced, "the toy can be reclaimed after the child is safe")) yield break;
        yield return Keys(0.3f);
        if (!Check(childhoodToy.IsPlaced && childhoodToy.AllowReclaim && StorySceneDirector.Instance.CompletedObjectives == 4,
            "changing one's mind returns the toy without duplicating progress or locking the choice")) yield break;

        phase = "stage first breath";
        yield return UseDoor("Classroom Exit", "02_Interlude_Firsts");
        if (finished) yield break;
        yield return PlayFirsts();
        if (finished) yield break;
        yield return Walk(0f);
        yield return Keys(0.3f, Key.E);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 0, "an unfinished first breath does not skip the opening")) yield break;
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1, "first stage phrase requires a full breath and release")) yield break;
        yield return Keys(0.25f);
        if (!Check(Player.GetComponent<CharacterAnimationDriver>().StageGesturesPlayed > 0 &&
            Player.CharacterRenderer.sprite.name.StartsWith("char_teen_stage"), "releasing a phrase actually displays the supplied stage gesture sprite")) yield break;
        Capture("03-stage-open-arms");
        phase = "stage elevated route";
        yield return Walk(3.2f);
        yield return Keys(0.45f, Key.D, Key.Space);
        yield return Keys(0.7f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1, "entering a stage route cannot complete its retired trigger")) yield break;
        yield return Note(false, false);
        var performance = FindAnyObjectByType<StagePerformance>();
        var stageFeedback = FindAnyObjectByType<StageFeedbackPresentation>();
        if (!Check(stageFeedback != null && stageFeedback.PlayedNotes == 1 && !stageFeedback.LastNoteLong &&
            stageFeedback.IsPlayingNote && stageFeedback.PlayedAudienceAnswers > 0,
            "the short note and its audience answer reach the actual audio presentation")) yield break;
        Capture("03-stage-short-answer");
        if (!Check(performance.ComposedBeats == 1 && StorySceneDirector.Instance.CompletedObjectives == 1,
            "one short note starts a phrase without completing it")) yield break;
        yield return Note(true, true);
        if (!Check(stageFeedback.PlayedNotes == 2 && stageFeedback.LastNoteLong && stageFeedback.IsPlayingNote,
            "a long note plays the distinct sustained response")) yield break;
        Capture("03-stage-long-answer");
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 2 && JourneyChoices.StageStyle.Contains("观众"),
            "high route supports composing an actual short-long phrase")) yield break;
        yield return Walk(10f);
        yield return Note(true, false);
        if (!Check(performance.RepriseRetries == 1 && StorySceneDirector.Instance.CompletedObjectives == 2,
            "a different rhythm retries the phrase without losing the performance")) yield break;
        yield return Note(false, false);
        yield return Note(true, true);
        if (!Check(performance.ReadyForCurtain && !StorySceneDirector.Instance.ObjectivesComplete,
            "finishing one's phrase leaves the decision to take a bow with the player")) yield break;
        yield return Keys(0.2f, Key.F);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete && JourneyChoices.HasStageRhythm &&
            !JourneyChoices.FirstBeatLong && JourneyChoices.SecondBeatLong, "the reprise remembers the player's own phrase")) yield break;

        phase = "stage lower route and alternate rhythm";
        yield return UseDoor("Back Curtain", "04_Interlude_Growing");
        if (finished) yield break;
        yield return PlayRepair(repairRuns++ == 0);
        if (finished) yield break;
        SceneManager.LoadScene("03_Stage");
        yield return Keys(1.2f);
        yield return Walk(0f);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.2f);
        yield return Walk(5f);
        yield return Note(true, true);
        yield return Note(false, false);
        yield return Keys(0.2f, Key.Q);
        yield return Keys(0.2f);
        yield return Note(false, false);
        yield return Walk(0f);
        yield return Note(true, false);
        performance = FindAnyObjectByType<StagePerformance>();
        if (!Check(performance.Revisions == 1 && performance.ComposedBeats == 2 &&
            performance.AddressedListener(0) == 0 && performance.AddressedListener(1) == 0 &&
            !JourneyChoices.FirstBeatLong && JourneyChoices.SecondBeatLong,
            "rewriting can address the same audience twice and change position between notes")) yield break;
        stageFeedback = FindAnyObjectByType<StageFeedbackPresentation>();
        var answersBefore = stageFeedback.PlayedAudienceAnswers;
        var notesBefore = stageFeedback.PlayedNotes;
        yield return Keys(0.2f, Key.F);
        if (!Check(performance.AudienceLeading, "F hands the player's rhythm to the audience")) yield break;
        yield return Keys(6.3f);
        if (!Check(performance.ReadyForCurtain && !StorySceneDirector.Instance.ObjectivesComplete,
            "listening to the audience is a complete alternate performance and still waits for a bow")) yield break;
        if (!Check(stageFeedback.PlayedNotes == notesBefore + 2 && stageFeedback.PlayedAudienceAnswers == answersBefore + 2,
            "the audience-led phrase actually produces both remembered notes and responses")) yield break;
        yield return Keys(0.2f, Key.F);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete && JourneyChoices.StageStyle.Contains("幕布") &&
            !JourneyChoices.FirstBeatLong && !JourneyChoices.SecondBeatLong && JourneyChoices.StageRelay.Outcome == StageRelayOutcome.Accepted,
            "accepting the audience preserves the version they actually finished")) yield break;

        phase = "workshop crate and held crossing";
        yield return UseDoor("Back Curtain", "04_Interlude_Growing");
        if (finished) yield break;
        yield return PlayRepair(repairRuns++ == 0);
        if (finished) yield break;
        yield return WorkshopHeld();
        if (finished) yield break;
        phase = "final farewell";
        yield return Walk(EmotionalJourney.Target("young-presenter").transform.position.x - 1f);
        yield return Keys(0.3f, Key.E);
        yield return Keys(0.25f);
        var ending = FindAnyObjectByType<ReleaseEndingGoal>();
        if (!Check(!ending.IsDeparting, "an early hand release does not skip reassurance")) yield break;
        yield return Keys(3.8f, Key.E);
        if (!Check(ending.IsReadyToLeave && ending.IsHeldBack && !ending.IsDeparting && Player.ControlsEnabled,
            "a ready recipient steps forward but the held hand prevents departure")) yield break;
        Capture("04-held-goodbye");
        yield return Keys(0.25f);
        var recipient = EmotionalJourney.Target("young-presenter");
        var initialX = recipient.transform.position.x;
        for (var i = 0; i < 50 && recipient.transform.position.x <= initialX; i++) yield return Keys(0.1f);
        if (!Check(recipient.transform.position.x > initialX, "recipient walks away after release")) yield break;
        yield return Walk(recipient.transform.position.x - 0.6f);
        yield return Keys(0.3f, Key.E);
        var pausedX = recipient.transform.position.x;
        yield return Keys(0.8f, Key.E);
        if (!Check(ending.TimesReheld == 1 && ending.IsHeldBack && Mathf.Abs(recipient.transform.position.x - pausedX) < 0.01f,
            "reaching back pauses departure without resetting the goodbye")) yield break;
        yield return Walk(recipient.transform.position.x + 2.25f, true);
        var waitingX = Player.transform.position.x;
        for (var i = 0; i < 100 && !ending.HasEnteredDoor; i++) yield return Keys(0.1f);
        if (!Check(ending.HasEnteredDoor, "the second release lets the recipient finish entering the door")) yield break;
        if (!Check(Player.ControlsEnabled, "the ending waits for the player's own last step")) yield break;
        if (!Check(ending.OnwardPositionX >= waitingX + 0.75f, "walking ahead before goodbye cannot skip the final step with empty hands")) yield break;
        yield return Walk(ending.OnwardPositionX + 0.2f);
        for (var i = 0; i < 30 && Player.ControlsEnabled; i++) yield return Keys(0.1f);
        if (!Check(!Player.ControlsEnabled, "walking onward triggers the final fade")) yield break;
        for (var i = 0; i < 100 && !StorySceneDirector.Instance.ReplayAvailable; i++) yield return Keys(0.1f);
        yield return Keys(0.2f, Key.Enter);
        yield return Keys(1f);
        if (!Check(SceneManager.GetActiveScene().name == "00_Prologue" && !JourneyChoices.HasStageRhythm &&
            string.IsNullOrEmpty(JourneyChoices.TestedResearchModel), "replay starts a fresh journey without inherited experiment or rhythm state")) yield break;
        yield return ExploreAlternative();
        if (finished) yield break;
        phase = "firsts home branch";
        SceneManager.LoadScene("02_Interlude_Firsts");
        yield return Keys(1.2f);
        yield return PlayFirsts(2);
        if (finished) yield break;
        Finish("ALL PLAYTHROUGH CHECKS PASSED using real keyboard events and Unity physics.");
    }

    private IEnumerator PlayFirsts(int stop = 0)
    {
        phase = "firsts playable interlude";
        var moment = FindAnyObjectByType<InterludeController>();
        if (!Check(moment != null && moment.HasIllustration, "the first interlude displays existing montage artwork")) yield break;
        if (!CheckInterludeText()) yield break;
        if (stop == 2)
        {
            var waitForHome = Time.time + 10f;
            while (moment.Firsts.Position < 0.60f && Time.time < waitForHome) yield return null;
        }
        Capture(stop == 2 ? "02-bus-home" : "02-bus-ride");
        yield return Keys(0.15f, Key.F);
        yield return Keys(0.15f);
        var until = Time.time + 12f;
        while (moment.Firsts.Phase == FirstsPhase.Riding && Time.time < until) yield return null;
        if (!Check(moment.Firsts.Phase == FirstsPhase.Walking, "ringing the bell actually stops the bus")) yield break;
        if (stop == 2 && !Check(moment.Firsts.ExitStop == 2, "choosing the home stop reaches the tired-parent branch")) yield break;
        Capture(stop == 2 ? "02-home-stop" : "02-friend-stop");
        until = Time.time + 7f;
        while (moment.Firsts.Phase == FirstsPhase.Walking && Time.time < until)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(moment.Firsts.Position < 0.55f ? Key.D : Key.A));
            yield return null;
        }
        yield return Keys(0.2f);
        if (!Check(moment.Firsts.Phase == FirstsPhase.RaisingHand, "the player walks from the chosen stop to school")) yield break;
        yield return Keys(0.6f, Key.E);
        var handBar = GameObject.Find("Raised hand")?.GetComponent<UnityEngine.UI.Image>();
        var schoolLabel = GameObject.Find("Stop name 1")?.GetComponent<UnityEngine.UI.Text>();
        if (!Check(handBar != null && schoolLabel != null &&
            Mathf.Abs(handBar.rectTransform.anchoredPosition.x - schoolLabel.rectTransform.anchoredPosition.x) < 0.01f &&
            handBar.rectTransform.pivot.x == 0.5f && Mathf.Abs(moment.Firsts.Position - FirstsJourney.Stops[1]) < 0.001f,
            "the raised-hand line expands equally from the exact school node")) yield break;
        Capture("02-raising-hand");
        yield return Keys(0.8f, Key.E);
        yield return Keys(0.3f);
        if (!Check(moment.Ready && JourneyChoices.BusExitStop >= 0, "raising and releasing the hand completes the first transition and remembers the stop")) yield break;
        Capture("02-restored-firsts");
        yield return Keys(0.3f);
        yield return Keys(0.15f, Key.F);
        yield return Keys(1.4f);
        Check(SceneManager.GetActiveScene().name == "03_Stage", "the restored firsts interlude leads into the stage");
    }

    private IEnumerator PlayRepair(bool send)
    {
        phase = send ? "an independently supported paper bridge" : "holding a paper route for someone else";
        var moment = FindAnyObjectByType<InterludeController>();
        if (!Check(moment != null && moment.HasIllustration && moment.Bridge != null,
            "the growing interlude presents a playable paper construction")) yield break;
        if (!CheckInterludeText()) yield break;
        var bridge = moment.Bridge;
        Capture("04-bridge-before");
        yield return Keys(0.15f, Key.F);
        yield return Keys(1.4f);
        if (!Check(bridge.Phase == PaperBridgePhase.Blocked && bridge.Failure != PaperBridgeFailure.None,
            "trying unsupported paper stops at a visible failure instead of advancing the story")) yield break;
        Capture("04-bridge-failure");
        yield return Keys(0.15f, Key.Q);
        yield return Keys(0.15f);
        if (send)
        {
            yield return Keys(0.15f, Key.Space);
            yield return Keys(0.15f);
            yield return MoveBridgePaper(bridge, 0.20f);
            yield return Keys(0.15f, Key.D);
            yield return Keys(0.15f);
            yield return Keys(0.15f, Key.D);
            yield return Keys(0.15f);
            yield return Keys(0.15f, Key.Space);
            yield return Keys(0.15f);
            yield return MoveBridgePaper(bridge, 0.80f);
            yield return Keys(0.15f, Key.F);
            yield return Keys(8f);
        }
        else
        {
            yield return Keys(0.15f, Key.D);
            yield return Keys(0.15f);
            yield return Keys(0.15f, Key.E, Key.F);
            yield return Keys(3f, Key.E);
            Capture("04-bridge-held-in-progress");
            yield return Keys(5f, Key.E);
        }
        if (!Check(bridge.Ready && bridge.Independent == send && JourneyChoices.PaperBridgeIndependent == send &&
            JourneyChoices.PaperBridgeAttempts >= 2,
            "the actual trial records whether the route stood independently or needed the player's hand")) yield break;
        Capture(send ? "04-bridge-independent" : "04-bridge-held-complete");
        yield return Keys(0.3f);
        if (!send && !Check(!bridge.IsStable(1) && bridge.Ready,
            "letting go after arrival collapses the supported paper without undoing the child's crossing")) yield break;
        yield return Keys(0.3f);
        yield return Keys(0.15f, Key.F);
        yield return Keys(1.4f);
        var workshop = FindAnyObjectByType<ResearchExpedition>();
        if (!Check(SceneManager.GetActiveScene().name == "05_Research" && workshop != null,
            "the folded-paper trial leads into the real workshop")) yield break;
        Check(Mathf.Abs(workshop.Plank.transform.position.x - (send ? 2.7f : -2f)) < 0.1f,
            "an independent paper bridge brings the reusable folding plank to the workbench in the next scene");
    }

    private IEnumerator MoveBridgePaper(PaperBridgeJourney bridge, float target)
    {
        var end = Time.time + 3f;
        while (Mathf.Abs(bridge.Positions[bridge.Selected] - target) > 0.004f && Time.time < end)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E,
                bridge.Positions[bridge.Selected] < target ? Key.D : Key.A));
            yield return null;
        }
        yield return Keys(0.15f);
    }
    private bool CheckInterludeText()
    {
        foreach (var name in new[] { "Moment title", "Moment state", "Moment controls" })
        {
            var label = GameObject.Find(name)?.GetComponent<UnityEngine.UI.Text>();
            var corners = new Vector3[4];
            if (label != null) label.rectTransform.GetWorldCorners(corners);
            if (!Check(label != null && !string.IsNullOrEmpty(label.text) && corners[0].x >= -1f &&
                corners[2].x <= Screen.width + 1f && corners[0].y >= -1f && corners[2].y <= Screen.height + 1f,
                name + " stays inside the visible game view")) return false;
        }
        return true;
    }

    private IEnumerator MovePaper(InterludeController moment, float destination, bool whole)
    {
        var until = Time.time + 5f;
        while (Time.time < until)
        {
            var x = whole ? moment.Repair.LetterPosition : moment.Repair.Pieces[moment.Repair.Selected];
            if (Mathf.Abs(x - destination) < 0.015f) break;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E, x < destination ? Key.D : Key.A));
            yield return null;
        }
        yield return Keys(0.1f, Key.E);
    }
    private IEnumerator WorkshopHeld()
    {
        var research = FindAnyObjectByType<ResearchExpedition>();
        var guide = FindAnyObjectByType<ResearchGuide>();
        var audioLibrary = Resources.Load<SceneAudioLibrary>("SceneAudioLibrary");
        var audio = FindAnyObjectByType<SceneAudio>();
        var deliveredAudio = audioLibrary != null && audio != null;
        foreach (var name in new[] { "footstepOne", "footstepTwo", "interact", "itemMove", "objectiveLight", "doorOpen",
            "handRelease", "heartbeat", "calmBreath", "applause", "paperRustle", "finalLight" })
        {
            var expected = audioLibrary == null ? null : typeof(SceneAudioLibrary).GetField(name).GetValue(audioLibrary) as AudioClip;
            var actual = audio == null ? null : typeof(SceneAudio).GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(audio) as AudioClip;
            deliveredAudio &= expected != null && expected.length > 0f && expected == actual &&
                AssetDatabase.GetAssetPath(expected).StartsWith("Assets/Audio/SFX/SFX/", StringComparison.Ordinal);
        }
        if (!Check(deliveredAudio, "all twelve delivered SFX are loaded and bound in the running scene")) yield break;
        if (!Check(audio.AmbienceClip != null && audio.AmbienceClip.name == "amb_indoor_loop" && audio.IsAmbiencePlaying,
            "the newly delivered indoor ambience is bound and actually looping")) yield break;
        var bridgeArt = research.Plank.GetComponent<SpriteRenderer>();
        var windowArt = GameObject.Find("Draft Window Handle")?.GetComponent<SpriteRenderer>();
        if (!Check(bridgeArt.sprite.name == "Research Bridge Closed" && bridgeArt.sprite.texture.name == "prop_research_bridge" &&
            bridgeArt.bounds.size.x > 1.4f && Mathf.Abs(bridgeArt.bounds.min.y + 0.2f) < 0.02f,
            "the delivered bridge shows only its closed flat surface, resting on the archive")) yield break;
        if (!Check(windowArt != null && windowArt.sprite.name == "Research Window Pane" &&
            windowArt.sprite.texture.name == "workshop-window" && windowArt.bounds.size.y > 1.4f,
            "the delivered window replaces the old signs using the complete cropped pane")) yield break;
        if (!CheckLearnerPose(research, "idle")) yield break;
        Capture("05-lab-entry");
        if (!Check(guide != null && guide.GoalText.Contains("实验小人") && guide.SituationText.Contains("压力板"), "research shows its goal and the first obstacle immediately on entry")) yield break;
        yield return Keys(0.15f, Key.H);
        yield return Keys(0.15f);
        if (!Check(guide.HelpVisible, "H reveals concrete alternate approaches")) yield break;
        yield return Keys(0.15f, Key.H);
        yield return Keys(0.15f);
        if (!Check(!guide.HelpVisible, "H can dismiss the extra guidance")) yield break;
        if (!Check(!research.ShowingLampRange && GameObject.Find("Desk Lamp") == null && GameObject.Find("Pinned Draft") == null &&
            GameObject.Find("Research Desk") == null && GameObject.Find("Research Notes") == null,
            "the workshop removes misleading old props and does not show an idle lamp range")) yield break;
        if (!Check(research != null && research.Toy == null, "leaving the childhood toy leaves the workshop without that tool")) yield break;
        yield return Walk(3.1f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Crate, "pick up the reusable crate")) yield break;
        yield return Walk(5.3f, true);
        yield return Keys(0.3f, Key.E);
        yield return Keys(0.3f);
        if (!Check(research.GateOpen, "a crate holds the pressure gate open")) yield break;
        yield return Keys(0.5f, Key.D, Key.Space);
        yield return Walk(10.5f);
        yield return Keys(0.2f, Key.F);
        if (!Check(research.GateLatched, "the inside latch permanently opens the return route")) yield break;
        Capture("05-shutter-latched");
        yield return Keys(0.2f);
        yield return Walk(11.6f);
        yield return Keys(0.2f, Key.F);
        if (!CheckLearnerPose(research, "walk")) yield break;
        yield return Keys(2f);
        if (!Check(research.State == LearnerState.NeedsBridge, "trying before preparation exposes the missing crossing")) yield break;
        if (!Check(guide.Step == ResearchGuideStep.Bridge && guide.SituationText.Contains("落脚点") && guide.GoalText.Contains("缺口"), "the visible goal explains why the learner stopped at the gap")) yield break;
        Capture("05-guide-gap");
        yield return Carry(research.Crate, 16f);
        if (finished) yield break;
        yield return Keys(2f);
        if (!Check(research.State == LearnerState.NeedsComfort, "the same crate solves a second problem but cannot solve darkness")) yield break;
        if (!CheckLearnerPose(research, "fear")) yield break;
        Capture("05-model-afraid");
        if (!Check(guide.Step == ResearchGuideStep.Comfort && guide.SituationText.Contains("暗处"), "the guide distinguishes fear of darkness from a missing crossing")) yield break;
        yield return Keys(0.5f, Key.D, Key.Space);
        yield return Walk(research.Learner.transform.position.x - 0.4f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Learner, "take the learner's hand at the dark threshold")) yield break;
        yield return Walk(24f, true);
        yield return Keys(1.2f, Key.E);
        yield return Keys(0.3f);
        if (!Check(research.ShortcutOpen && !JourneyChoices.LearnedIndependentDeparture && JourneyChoices.CrossingTool == "crate", "accompanying the learner remembers a distinct solution")) yield break;
        if (!CheckLearnerPose(research, "idle")) yield break;
        Capture("03-workshop-arrival");
        yield return ReturnReport(research);
        if (finished) yield break;
    }

    private IEnumerator Note(bool sustained, bool right)
    {
        yield return Keys(0.4f, Key.E, right ? Key.D : Key.A);
        if (sustained) yield return Keys(0.6f, Key.E, right ? Key.D : Key.A);
        if (phase == "stage elevated route") Capture("02-stage-aim");
        yield return Keys(0.2f);
    }

    private IEnumerator UseDoor(string name, string nextScene)
    {
        var door = GameObject.Find(name);
        if (!Check(door != null, "exit exists: " + name)) yield break;
        yield return Walk(door.transform.position.x - 0.5f);
        yield return Keys(0.2f, Key.E);
        yield return Keys(1.4f);
        Check(SceneManager.GetActiveScene().name == nextScene, name + " loads " + nextScene + " through actual interaction and fade");
    }

    private static void Capture(string name)
    {
        Directory.CreateDirectory("Logs/GameplayCaptures");
        ScreenCapture.CaptureScreenshot("Logs/GameplayCaptures/" + name + ".png");
    }

    private IEnumerator Carry(HoldTarget item, float dropX)
    {
        var side = Player.transform.position.x > item.transform.position.x ? 1f : -1f;
        var reach = item.TargetId == "workshop-crate" ? 1.05f : 0.7f;
        yield return Walk(item.transform.position.x + side * reach);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == item, "pick up " + item.TargetId)) yield break;
        if (item.TargetId == "workshop-lamp")
        {
            var coverageObject = GameObject.Find("Portable lamp coverage");
            var coverage = coverageObject == null ? null : coverageObject.GetComponent<LineRenderer>();
            var tether = GameObject.Find("Hand Connection Light")?.GetComponent<LineRenderer>();
            if (!Check(tether != null && !tether.enabled, "carrying a lamp does not draw a second hand-to-object rope")) yield break;
            var glow = GameObject.Find("Workshop Lamp Glow")?.GetComponent<SpriteRenderer>();
            if (!Check(coverage != null && !coverage.enabled && glow != null &&
                glow.enabled && glow.color.a > 0f && !FindAnyObjectByType<ResearchExpedition>().ShowingLampRange,
                "carrying the lamp shows actual soft light with no developer range diagram")) yield break;
            Capture("05-lamp-placement");
        }
        yield return Walk(dropX - item.CarryOffset.x, true);
        yield return Keys(0.3f, Key.E);
        yield return Keys(0.3f);
        Check(Mathf.Abs(item.transform.position.x - dropX) < 0.3f, "reuse " + item.TargetId + " at " + dropX);
    }

    private IEnumerator ReturnReport(ResearchExpedition research)
    {
        // ResearchGuide publishes the arrival message in LateUpdate, after the learner ticks.
        yield return new WaitForEndOfFrame();
        if (!Check(FindAnyObjectByType<ResearchGuide>().GoalText.Contains("报告"), "arrival visibly replaces the crossing goal with returning the report")) yield break;
        yield return Walk(22.6f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Report, "the report only exists after a successful experiment")) yield break;
        yield return Keys(0.2f, Key.E, Key.F);
        if (!Check(Player.transform.position.x < 1f && Hand.CurrentTarget == research.Report, "the unlocked return shortcut carries the held report")) yield break;
        yield return Keys(0.3f, Key.E);
        yield return Keys(0.3f);
        if (!Check(research.ReportReturned && StorySceneDirector.Instance.ObjectivesComplete, "returning to the workbench completes the loop")) yield break;
        yield return Keys(0.2f, Key.Q);
        if (!Check(research.ReportReturned && StorySceneDirector.Instance.ObjectivesComplete && research.State == LearnerState.Arrived,
            "submitting the report commits the experiment and disables recall")) yield break;
        yield return Keys(0.2f);
        yield return Walk(1f);
        yield return Keys(0.2f, Key.F);
        yield return Keys(1.4f);
        Check(SceneManager.GetActiveScene().name == "06_FinalWalk", "the completed workshop opens the final chapter");
    }

    private IEnumerator ExploreAlternative()
    {
        phase = "workshop archive, plank, wind and independent crossing";
        // A separate chapter fixture represents bringing the childhood toy; the main run left it.
        JourneyChoices.TakeChildhoodToy(true);
        SceneManager.LoadScene("05_Research");
        yield return Keys(1.4f);
        var research = FindAnyObjectByType<ResearchExpedition>();
        if (!Check(research.Toy != null, "bringing the childhood toy adds a usable workshop object")) yield break;
        yield return Walk(research.Toy.transform.position.x - 0.7f);
        yield return Keys(0.7f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Toy, "the remembered toy can be wound with the actual hold input")) yield break;
        yield return Keys(0.3f);
        if (!Check(research.ToySoundRemaining > 10f && !research.Toy.IsHeld, "the toy continues providing support after being set down")) yield break;
        yield return Carry(research.Crate, -2f);
        if (finished) yield break;
        yield return Keys(0.31f, Key.D, Key.Space);
        yield return Keys(0.65f);
        if (!Check(Player.transform.position.y > -1.2f, "the crate is a physical step to the archive")) yield break;
        yield return Keys(0.1f, Key.Space);
        yield return Keys(0.8f);
        if (!Check(Player.transform.position.y > 0f, "a second jump reaches the optional archive")) yield break;
        Capture("05-archive-top");
        yield return Walk(-2.7f);
        yield return Keys(0.2f, Key.F);
        if (!Check(research.CluesFound == 1, "exploration discovers the tool and window clue")) yield break;
        yield return Walk(-2f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Plank, "the archive supplies an alternate crossing tool")) yield break;
        yield return Walk(8.2f, true);
        yield return Keys(0.8f, Key.D, Key.E);
        if (!Check(Player.transform.position.x < 8.5f && !research.GateOpen, "walking past the plate cannot bypass the closed physical gate")) yield break;
        yield return Walk(5.7f, true);
        yield return Keys(0.3f, Key.E);
        yield return Keys(0.3f);
        yield return Walk(10.5f);
        yield return Keys(0.2f, Key.F);
        yield return Keys(0.2f);
        if (!Check(research.GateLatched, "the plank also operates the pressure gate")) yield break;
        yield return Carry(research.Plank, 16f);
        if (finished) yield break;
        yield return Walk(11.6f);
        yield return Keys(0.2f, Key.F);
        yield return Keys(2.8f);
        yield return Walk(21f);
        // Open the window before bringing the light: a working carried lamp is also
        // a valid solution and could otherwise let the learner finish on the way.
        if (!research.WindActive) { yield return Keys(0.2f, Key.F); yield return Keys(0.2f); }
        yield return Carry(research.Lamp, 20f);
        if (finished) yield break;
        yield return Walk(21f);
        yield return Keys(1f);
        if (!Check(!research.LampWorking && !research.ShortcutOpen, "an open window disables the lamp in the dark area")) yield break;
        if (!Check(FindAnyObjectByType<ResearchGuide>().GoalText.Contains("风"), "the visible goal identifies wind as the cause of the failed lamp")) yield break;
        yield return Keys(0.2f, Key.F);
        yield return Keys(3.5f);
        if (!Check(research.ShortcutOpen && JourneyChoices.LearnedIndependentDeparture && JourneyChoices.TestedResearchModel == "lamp" && JourneyChoices.CrossingTool == "plank", "closing the window lets a prepared environment support an independent crossing")) yield break;
        yield return Walk(22.6f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Report, "a successful report can be inspected before committing the result")) yield break;
        yield return Walk(20f, true);
        yield return Keys(0.2f);
        yield return Keys(0.2f, Key.Q);
        if (!Check(!research.ShortcutOpen && research.State == LearnerState.Recalling && research.Rehearsals == 1,
            "the player can recall a successful experiment before submitting the report")) yield break;
        if (!CheckLearnerPose(research, "walk")) yield break;
        Capture("05-model-recalling");
        yield return Keys(3.5f);
        yield return Carry(research.Lamp, 11f);
        if (finished) yield break;
        yield return Walk(11.6f);
        yield return Keys(0.2f, Key.F);
        yield return Keys(5.2f);
        if (!Check(research.ShortcutOpen && JourneyChoices.TestedResearchModel == "learned" && JourneyChoices.LearnedIndependentDeparture,
            "a rehearsed learner crosses without the removed lamp because previous help changed what it knows")) yield break;
        if (!Check(Mathf.Abs(research.Report.transform.position.x - 22f) < 0.01f,
            "retrying replaces the moved draft with a new report at the completed experiment")) yield break;
        Capture("05-learned-independence");
        yield return ReturnReport(research);
        if (finished) yield break;
        yield return Walk(EmotionalJourney.Target("young-presenter").transform.position.x - 1.4f);
        yield return Keys(0.4f);
        var ending = FindAnyObjectByType<ReleaseEndingGoal>();
        Check(ending.InitiatesOwnDeparture && ending.IsDeparting && Hand.CurrentTarget == null, "the previous independent solution makes the next person initiate goodbye without being prompted by a hand hold");
    }

    private IEnumerator Walk(float x, bool hold = false)
    {
        var timeout = Time.time + Mathf.Max(18f, Mathf.Abs(x - Player.transform.position.x) / 2.5f + 5f);
        var direction = Mathf.Sign(x - Player.transform.position.x);
        while (Mathf.Abs(Player.transform.position.x - x) > 0.12f &&
            Mathf.Sign(x - Player.transform.position.x) == direction && Time.time < timeout && Player.ControlsEnabled)
        {
            var key = Player.transform.position.x < x ? Key.D : Key.A;
            InputSystem.QueueStateEvent(keyboard, hold ? new KeyboardState(key, Key.E) : new KeyboardState(key));
            yield return null;
        }
        InputSystem.QueueStateEvent(keyboard, hold ? new KeyboardState(Key.E) : new KeyboardState());
        yield return null;
    }

    private IEnumerator Keys(float seconds, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        // Let the next input update consume the state before starting its dwell time.
        yield return null;
        yield return new WaitForSeconds(seconds);
    }
    private IEnumerator ReviewLearnedHabits()
    {
        JourneyChoices.Reset();
        phase = "learning from a real carried-light demonstration";
        SceneManager.LoadScene("05_Research");
        yield return Keys(1.4f);
        var research = FindAnyObjectByType<ResearchExpedition>();
        var audio = FindAnyObjectByType<SceneAudio>();
        if (!Check(audio.MusicClip != null && audio.MusicClip.name.Contains("v3_剩余关卡") && audio.IsMusicPlaying,
            "the delivered journey music plays as a loop in the research scene")) yield break;
        yield return Walk(3.1f);
        yield return Keys(0.2f, Key.E);
        yield return Walk(5.3f, true);
        yield return Keys(0.25f);
        yield return Keys(0.5f, Key.D, Key.Space);
        yield return Walk(10.5f);
        yield return Keys(0.2f, Key.F);
        yield return Walk(21f);
        if (research.WindActive) yield return Keys(0.2f, Key.F);
        yield return Walk(13.2f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Lamp, "the player can choose to demonstrate with a carried lamp")) yield break;
        yield return Keys(0.2f, Key.E, Key.H);
        var lampGlow = GameObject.Find("Workshop Lamp Glow")?.GetComponent<SpriteRenderer>();
        if (!Check(lampGlow != null && lampGlow.enabled && lampGlow.color.a > 0f && ResearchDiagramsHidden(),
            "carrying a lamp with hints open keeps soft light visible and every research diagram hidden")) yield break;
        Capture("05-lamp-without-diagrams");
        yield return Keys(0.2f, Key.E);
        yield return Keys(0.2f, Key.E, Key.H);
        yield return Keys(0.2f, Key.E);
        yield return Keys(0.2f, Key.E, Key.T);
        if (!Check(research.IsDemonstrating, "T starts an observed demonstration while carrying a tool")) yield break;
        yield return Walk(14.9f, true);
        yield return Keys(0.62f, Key.D, Key.Space, Key.E);
        yield return Walk(18.6f, true);
        yield return Keys(0.4f, Key.E);
        yield return Keys(0.2f, Key.E, Key.T);
        if (!Check(research.HasDemonstration && research.Demonstrations == 1 && !research.IsDemonstrating,
            "a real demonstration records a lesson for the learner's next turn")) yield break;
        yield return Keys(0.8f, Key.E);
        if (!Check(research.State == LearnerState.Waiting && Mathf.Abs(research.Learner.transform.position.x - 12f) < 0.05f,
            "finishing far from the learner never starts its first imitation outside the player's view")) yield break;
        var recordedJump = false;
        var recordedHelp = false;
        foreach (var action in research.DemonstratedActions)
        {
            recordedJump |= action.CrossesGap;
            recordedHelp |= action.Kind == LearnedHabitKind.SeekHelp && action.Help == "lamp";
        }
        if (!Check(recordedJump && recordedHelp, "actual takeoff, landing and carried light become separate learned actions")) yield break;
        yield return Walk(10f, true);
        yield return Keys(0.2f);
        yield return Walk(12f);
        yield return Keys(0.2f, Key.F);
        for (var i = 0; i < 100 && research.State != LearnerState.NeedsComfort; i++) yield return Keys(0.1f);
        if (!Check(research.State == LearnerState.NeedsComfort && !research.ShortcutOpen && research.Learner.transform.position.x > 17f,
            "the learned jump solves the empty gap, but removing the demonstrated lamp exposes dependence on light")) yield break;
        yield return Walk(17f);
        Capture("05-learned-lamp-dependence");
        yield return Keys(0.2f, Key.Q);
        for (var i = 0; i < 80 && research.State != LearnerState.Waiting; i++) yield return Keys(0.1f);
        yield return Walk(12f);
        yield return Keys(0.2f, Key.T);
        if (!Check(research.IsDemonstrating, "recall returns the learner so its previous lesson can be rewritten")) yield break;
        yield return Walk(14.9f);
        yield return Keys(0.62f, Key.D, Key.Space);
        yield return Walk(18.4f);
        yield return Keys(0.25f);
        yield return Keys(1.8f, Key.E);
        if (!Check(Hand.IsSelfAnchoring && Hand.SelfChargeNormalized > 0.98f,
            "the replacement demonstration includes a deliberate completed calming breath")) yield break;
        yield return Keys(0.2f);
        yield return Keys(0.2f, Key.T);
        yield return Walk(12f);
        yield return Keys(0.2f, Key.F);
        var sawJump = false;
        var sawPause = false;
        for (var i = 0; i < 130 && !research.ShortcutOpen; i++)
        {
            yield return Keys(0.1f);
            if (research.LearnerHeight > 0.12f && !sawJump)
            {
                sawJump = true;
                var viewport = Camera.main.WorldToViewportPoint(research.Learner.transform.position);
                if (!Check(viewport.x > 0.03f && viewport.x < 0.97f && viewport.y > 0f && viewport.y < 1f,
                    "the learned jump is actually inside the game camera when the player starts the trial beside the learner")) yield break;
                Capture("05-learned-jump");
                yield return Walk(16.3f);
            }
            if (research.State == LearnerState.Pausing && !sawPause)
            {
                sawPause = true;
                Capture("05-learned-breath");
            }
        }
        if (!Check(sawJump && sawPause && research.ShortcutOpen && research.Demonstrations == 2 &&
            JourneyChoices.CrossingTool == "jump" && JourneyChoices.TestedResearchModel == "breath" &&
            JourneyChoices.LearnedHabit.Kind == LearnedHabitKind.Jump,
            "rewriting the lesson creates a real jump-and-breath route with no bridge, lamp or hand")) yield break;
        yield return ReturnReport(research);
        if (finished) yield break;
        phase = "the next generation repeats the taught jump";
        var recipient = EmotionalJourney.Target("young-presenter");
        var ending = FindAnyObjectByType<ReleaseEndingGoal>();
        yield return Walk(recipient.transform.position.x - 1.8f);
        if (!Check(ending.ReplayedHabitKind == LearnedHabitKind.Jump,
            "the final person receives the habit actually used in the committed successful trial")) yield break;
        yield return Keys(1f);
        Capture("06-learned-habit-echo");
        for (var i = 0; i < 100 && !ending.HasEnteredDoor; i++) yield return Keys(0.1f);
        var farewell = recipient.GetComponent<FinalFarewellPresentation>();
        if (!Check(ending.HasReplayedHabit && ending.HasEnteredDoor && farewell.DoorEntryComplete && !farewell.CharacterRenderer.enabled,
            "the person repeats the learned habit, walks through the door and stays hidden")) yield break;
        yield return Keys(0.5f);
        if (!Check(!farewell.CharacterRenderer.enabled && Player.ControlsEnabled,
            "scene presentation cannot resurrect the departed person and the last step stays with the player")) yield break;
        Capture("06-doorway-empty");
        yield return Walk(ending.OnwardPositionX + 0.2f);
        for (var i = 0; i < 100 && !StorySceneDirector.Instance.ReplayAvailable; i++) yield return Keys(0.1f);
        var artwork = GameObject.Find("Delivered Ending Art")?.GetComponent<UnityEngine.UI.Image>();
        var replay = GameObject.Find("Closing Replay")?.GetComponent<UnityEngine.UI.Text>();
        if (!Check(artwork != null && artwork.sprite != null && artwork.sprite.name.Contains("结束界面") &&
            artwork.preserveAspect && artwork.color.a > 0.99f && replay != null && replay.color.a > 0.99f &&
            GameObject.Find("Closing Title") == null && GameObject.Find("Closing Thanks") == null,
            "the final frame preserves the delivered ending painting with only a delayed quiet replay hint")) yield break;
        Capture("06-delivered-ending-painting");
        yield return Keys(0.2f, Key.Enter);
        yield return Keys(1.4f);
        if (!Check(SceneManager.GetActiveScene().name == "00_Prologue" && JourneyChoices.LearnedHabit.Kind == LearnedHabitKind.None,
            "replay clears the taught habit and starts a new journey")) yield break;
        SceneManager.LoadScene("01_Kindergarten");
        yield return Keys(1.2f);
        audio = FindAnyObjectByType<SceneAudio>();
        var prompt = GameObject.Find("Prompt")?.GetComponent<UnityEngine.UI.Text>();
        if (!Check(audio.MusicClip != null && audio.MusicClip.name.Contains("v2_幼儿园关卡") && audio.IsMusicPlaying &&
            prompt != null && prompt.fontSize == 24 && prompt.font == StoryTypography.PromptFont,
            "kindergarten selects its own delivered music and shares the refined Chinese prompt style")) yield break;
        Capture("01-refined-prompt-and-music");
    }

    private IEnumerator ReviewEndingBoundaries()
    {
        phase = "reholding a taught hop at the doorway";
        JourneyChoices.Reset();
        JourneyChoices.RememberWorkshop("hand", "jump", false);
        JourneyChoices.RememberLearnedHabit(new LearnedHabitSnapshot(LearnedHabitKind.Jump, 0f, 15f, "", 1));
        SceneManager.LoadScene("06_FinalWalk");
        yield return Keys(1.4f);
        var recipient = EmotionalJourney.Target("young-presenter");
        var ending = FindAnyObjectByType<ReleaseEndingGoal>();
        var farewell = recipient.GetComponent<FinalFarewellPresentation>();
        var door = GameObject.Find("Unknown Door").transform;
        yield return Walk(recipient.transform.position.x - 0.8f);
        yield return Keys(1.8f, Key.E);
        yield return Walk(door.position.x + 0.8f, true);
        yield return Keys(0.2f, Key.E);
        yield return Keys(1.05f);
        if (!Check(ending.IsReplayingHabit && !ending.IsEnteringDoor && !ending.HasEnteredDoor &&
            farewell.CharacterRenderer.bounds.min.y > -2.62f,
            "a taught hop at the doorway remains visible and cannot be cut short by entering")) yield break;
        yield return Keys(0.8f, Key.E);
        if (!Check(Hand.CurrentTarget == recipient && !ending.HasEnteredDoor && ending.TimesReheld > 0 &&
            Mathf.Abs(farewell.CharacterRenderer.bounds.min.y + 2.72f) < 0.02f,
            "reaching back during a hop lets it land naturally and pauses the next step")) yield break;
        Capture("06-reheld-habit-at-door");
        for (var i = 0; i < 80 && !ending.IsEnteringDoor; i++) yield return Keys(0.05f);
        yield return Keys(0.15f);
        Capture("06-door-entry-opening");
        yield return Keys(0.23f);
        Capture("06-door-entry-depth");
        for (var i = 0; i < 40 && !ending.HasEnteredDoor; i++) yield return Keys(0.1f);
        if (!Check(ending.HasReplayedHabit && ending.HasEnteredDoor && farewell.DoorEntryComplete &&
            !farewell.CharacterRenderer.enabled && Player.ControlsEnabled && ending.OnwardPositionX > Player.transform.position.x + 0.7f,
            "releasing again completes the remembered action and doorway exit, with a fresh final step ahead")) yield break;
    }

    private IEnumerator ReviewContinuousKindergarten()
    {
        JourneyChoices.Reset();
        phase = "continuous kindergarten opening";
        SceneManager.LoadScene("01_Kindergarten");
        yield return Keys(1.4f);
        var bag = EmotionalJourney.Target("home-bag");
        var wall = GameObject.Find("Classroom Wall");
        var backdropStates = new Dictionary<SpriteRenderer, Color>();
        foreach (var renderer in wall.GetComponentsInChildren<SpriteRenderer>())
            if (renderer.enabled) backdropStates.Add(renderer, renderer.color);
        if (!Check(VisibleObject(bag.gameObject) && VisibleObject(GameObject.Find("Cubby")) &&
            !VisibleObject(GameObject.Find("Integrated Classroom Entrance")) && PassageAlpha() == 0f,
            "the opening starts with a stable classroom, visible backpack and cubby, and no transition overlay")) yield break;
        Capture("01-continuous-start");
        var escort = FindAnyObjectByType<ParentEscort>();
        yield return Keys(0.3f, Key.E);
        yield return new WaitForEndOfFrame();
        if (!CheckHandThread("near the parent")) yield break;
        Capture("01-hand-thread-near");
        for (var i = 0; i < 80 && !escort.WaitingAtDoor; i++) yield return Keys(0.2f, Key.E);
        if (!Check(escort.WaitingAtDoor && Player.ControlsEnabled, "parental guidance still works in the continuous classroom")) yield break;
        yield return Walk(EmotionalJourney.Target("parent").transform.position.x + 2.05f, true);
        yield return new WaitForEndOfFrame();
        if (!CheckHandThread("when walking away from the waiting parent")) yield break;
        Capture("01-hand-thread-stretched");
        yield return Keys(0.2f);
        yield return new WaitForEndOfFrame();
        if (!Check(!FindAnyObjectByType<HandTetherPresentation>().IsVisible, "releasing the parent hides the hand thread")) yield break;
        yield return Walk(-1.25f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == bag && FindAnyObjectByType<ReleaseGate>().HasEntered,
            "the backpack can be picked up after the child's own first step")) yield break;
        yield return new WaitForEndOfFrame();
        if (!Check(!FindAnyObjectByType<HandTetherPresentation>().IsVisible, "carrying the backpack never shows the hand thread")) yield break;
        yield return Walk(-4.2f, true);
        yield return Keys(0.8f, Key.E);
        var unchanged = true;
        foreach (var state in backdropStates) unchanged &= state.Key.enabled && state.Key.color == state.Value;
        if (!Check(Hand.CurrentTarget == bag && VisibleObject(bag.gameObject) && unchanged && PassageAlpha() == 0f &&
            !VisibleObject(GameObject.Find("Integrated Classroom Entrance")),
            "carrying the backpack back across the old threshold never fades or replaces the scene")) yield break;
        if (!Check(Mathf.Abs(Camera.main.transform.position.x - Player.transform.position.x) < 0.6f,
            "the camera continues following the child when returning toward the parents")) yield break;
        Capture("01-continuous-return-with-bag");
        yield return Keys(0.3f);
        if (!Check(Hand.CurrentTarget == null && VisibleObject(bag.gameObject),
            "putting the backpack down on the return route leaves it visible")) yield break;
        yield return Keys(0.2f, Key.E);
        yield return Walk(1.8f, true);
        yield return Keys(0.3f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 2 && VisibleObject(bag.gameObject) && PassageAlpha() == 0f,
            "returning with the backpack still completes the chair task without a camera transition")) yield break;
        Capture("01-continuous-bag-placed");
    }

    private IEnumerator ReviewSceneStaging()
    {
        phase = "stage backstage support and curtains";
        SceneManager.LoadScene("03_Stage");
        yield return Keys(1.4f);
        var parent = EmotionalJourney.Target("stage-parent");
        if (!Check(parent != null && VisibleObject(parent.gameObject), "the parent is visible and reachable in the stage waiting area")) yield break;
        if (!Check(FindAnyObjectByType<PromptKeyIcon>()?.IsVisible == true,
            "the newly delivered E icon appears beside the actual backstage hand prompt")) yield break;
        var fold = GameObject.Find("Stage Left Foreground Fold")?.GetComponent<SpriteRenderer>();
        if (!Check(VisibleObject(GameObject.Find("Curtain Left")) && VisibleObject(GameObject.Find("Curtain Right")) &&
            fold != null && fold.enabled && fold.sortingOrder > Player.CharacterRenderer.sortingOrder && fold.bounds.max.x < -1f,
            "both authored curtains are restored, with a foreground wing that ends before the first speaking mark")) yield break;
        Capture("03-backstage-arrival");
        yield return Keys(0.3f, Key.E);
        if (!Check(Hand.CurrentTarget == parent, "the player can take the parent's hand before going on stage")) yield break;
        yield return Walk(-3.6f, true);
        yield return Keys(0.3f, Key.E);
        if (!Check(parent.transform.position.x <= -4.49f, "the parent stays at the backstage boundary when the player steps toward the lights")) yield break;
        Capture("03-backstage-parent-waits");
        yield return Keys(0.2f);
        yield return Walk(0f);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.4f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1 &&
            Player.GetComponent<CharacterAnimationDriver>().StageGesturesPlayed > 0,
            "after leaving the parent, a full breath plays the supplied stage gesture and opens the performance")) yield break;
        Capture("03-curtain-to-first-phrase");
        yield return Walk(5f);
        yield return Note(false, false);
        yield return Note(true, true);
        yield return Keys(0.2f, Key.F);
        yield return Keys(6.3f);
        yield return Keys(0.2f, Key.F);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete, "the restored curtains leave audience response and the player's bow playable")) yield break;
        yield return Walk(15f);
        Capture("03-curtain-exit-approach");
        yield return UseDoor("Back Curtain", "04_Interlude_Growing");
        if (finished) yield break;
        phase = "research removes retired character and keeps learning readable";
        SceneManager.LoadScene("05_Research");
        yield return Keys(1.4f);
        if (!Check(GameObject.Find("Mentor") == null && FindAnyObjectByType<ResearchExpedition>()?.Learner != null,
            "the retired noninteractive mentor is absent while the actual learner remains available")) yield break;
        var wall = GameObject.Find("Research Background").GetComponent<SpriteRenderer>();
        var extension = GameObject.Find("Mentor Hallway").GetComponent<SpriteRenderer>();
        if (!Check(extension.sprite == wall.sprite && extension.flipX != wall.flipX &&
            Mathf.Abs(extension.bounds.min.y - wall.bounds.min.y) < 0.01f &&
            Mathf.Abs(extension.bounds.max.x - wall.bounds.min.x) < 0.1f,
            "the entrance extends the same painted wall with a continuous floor instead of an unrelated office panel")) yield break;
        Capture("05-workshop-clear-arrival");
        if (!Check(FindAnyObjectByType<PromptKeyIcon>()?.IsVisible == false,
            "the E icon stays hidden when there is no nearby E action prompt")) yield break;
        yield return Walk(3.1f);
        var research = FindAnyObjectByType<ResearchExpedition>();
        var crateArt = research.Crate.transform.Find("Presentation Visual")?.GetComponent<SpriteRenderer>();
        if (!Check(crateArt != null && Mathf.Abs(crateArt.bounds.min.y + 2.72f) < 0.04f &&
            crateArt.sprite.rect.width < crateArt.sprite.texture.width * 0.9f && ResearchDiagramsHidden(),
            "the crate's visible image is cropped to its body and rests on the floor without interaction diagrams")) yield break;
        Capture("05-crate-on-floor");
        yield return Keys(0.2f, Key.E);
        yield return Walk(4.5f, true);
        yield return Keys(0.25f);
        if (!Check(Mathf.Abs(crateArt.bounds.min.y + 2.72f) < 0.04f,
            "putting the crate down returns the actual visible base to the floor")) yield break;
        Capture("05-crate-after-drop");
        var prompt = GameObject.Find("Prompt")?.GetComponent<UnityEngine.UI.Text>();
        if (!Check(prompt != null && prompt.font == StoryTypography.PromptFont && prompt.fontSize == 24 &&
            prompt.rectTransform.anchorMin == Vector2.zero && prompt.rectTransform.pivot == Vector2.zero,
            "the shared action text uses the readable left margin instead of covering the character")) yield break;
    }

    private static bool ResearchDiagramsHidden()
    {
        foreach (var name in new[] { "Plate to shutter circuit", "Learner crossing gap", "Draft through the window",
            "Workshop route sketch", "Portable lamp coverage", "Familiar toy sound range", "Nearest usable tool" })
        {
            var line = GameObject.Find(name)?.GetComponent<LineRenderer>();
            if (line != null && line.enabled && line.startWidth > 0f) return false;
        }
        foreach (var name in new[] { "Current destination", "Nearby reusable object" })
            if (GameObject.Find(name) != null) return false;
        return true;
    }

    private bool CheckHandThread(string moment)
    {
        var presentation = FindAnyObjectByType<HandTetherPresentation>();
        var line = presentation == null ? null : presentation.GetComponent<LineRenderer>();
        return Check(Hand.CurrentTarget != null && presentation != null && presentation.IsVisible &&
            line.positionCount == 16 && line.startWidth <= 0.0281f && line.endWidth <= 0.0221f &&
            Vector3.Distance(line.GetPosition(0), presentation.StartPoint) < 0.001f &&
            Vector3.Distance(line.GetPosition(15), presentation.EndPoint) < 0.001f &&
            presentation.StartPoint.y > Player.CharacterRenderer.bounds.min.y &&
            presentation.StartPoint.y < Player.CharacterRenderer.bounds.max.y,
            "thin curved hand thread follows visible hands " + moment);
    }

    private static bool VisibleObject(GameObject root)
    {
        if (root == null) return false;
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.color.a > 0.99f) return true;
        return false;
    }

    private IEnumerator ReviewPassages()
    {
        JourneyChoices.Reset();
        phase = "kindergarten entrance continuity";
        SceneManager.LoadScene("01_Kindergarten");
        yield return Keys(1.4f);
        Capture("01-entrance-wide");
        var escort = FindAnyObjectByType<ParentEscort>();
        for (var i = 0; i < 80 && !escort.WaitingAtDoor; i++) yield return Keys(0.2f, Key.E);
        if (!Check(escort.WaitingAtDoor && Player.ControlsEnabled, "the opening still lets the parent lead to the doorway")) yield break;
        yield return Keys(0.2f);
        yield return Walk(-1.8f);
        yield return Keys(1f);
        if (!Check(Player.ControlsEnabled && PassageVisible(), "stopping at the kindergarten threshold keeps the scene visible and controllable")) yield break;
        Capture("01-entrance-threshold");
        yield return Walk(-3.2f);
        yield return Keys(0.5f);
        if (!Check(PassageVisible(), "returning toward the parent does not trap the opening in a blackout")) yield break;
        Capture("01-entrance-return");
        yield return Walk(-0.8f);
        yield return Keys(0.6f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1 && PassageVisible(), "entering the classroom preserves the release objective and visibility")) yield break;
        Capture("01-entrance-inside");

        phase = "final memory continuity";
        SceneManager.LoadScene("06_FinalWalk");
        yield return Keys(1.4f);
        var clearDistance = Mathf.Max(14f, Camera.main.orthographicSize * Camera.main.aspect * 2f + 1.9f);
        foreach (var age in new[] { "char_teen", "char_adult" })
        {
            FinalAgeTransition transition = null;
            foreach (var candidate in FindObjectsByType<FinalAgeTransition>())
                if (candidate.ArtSlotId == age) transition = candidate;
            if (!Check(transition != null, "the memory walk contains the " + age + " transition")) yield break;
            var boundary = transition.transform.position.x;
            var earlierAge = age == "char_teen" ? "char_child" : "char_teen";
            yield return Walk(boundary - clearDistance);
            yield return Keys(0.2f);
            if (!Check(Player.CharacterRenderer.sprite.name.StartsWith(earlierAge) && PassageVisible(),
                "approaching " + age + " begins visibly in " + earlierAge)) yield break;
            Capture("06-before-" + age);
            var bodySize = Player.GetComponent<Collider2D>().bounds.size;
            yield return Walk(boundary - 0.5f);
            yield return Keys(1f);
            if (!Check(PassageAlpha() >= 0.99f && Player.ControlsEnabled && Player.CharacterRenderer.sprite.name.StartsWith(earlierAge),
                "the player enters full black before changing from " + earlierAge)) yield break;
            Capture("06-black-before-" + age);
            yield return Walk(boundary + 0.5f);
            yield return Keys(0.2f);
            if (!Check(PassageAlpha() >= 0.99f && transition.IsCurrentAge && Player.CharacterRenderer.sprite.name.StartsWith(age) &&
                Vector3.Distance(bodySize, Player.GetComponent<Collider2D>().bounds.size) < 0.01f,
                "the " + age + " change occurs inside full black without changing collision dimensions")) yield break;
            yield return Walk(boundary + clearDistance);
            yield return Keys(0.2f);
            if (!Check(PassageVisible() && Player.CharacterRenderer.sprite.name.StartsWith(age),
                "walking out of the cinematic passage reveals " + age)) yield break;
            Capture("06-emerged-" + age);
            yield return Keys(0.1f);
            if (!CheckMemoryBackdrop(age == "char_teen" ? "Stage Memory" : "Research Memory")) yield break;
            yield return Walk(boundary - 0.5f);
            yield return Keys(0.2f);
            if (!Check(PassageAlpha() >= 0.99f && !transition.IsCurrentAge && Player.CharacterRenderer.sprite.name.StartsWith(earlierAge),
                "walking back restores " + earlierAge + " while still fully black")) yield break;
            yield return Walk(boundary - clearDistance);
            yield return Keys(0.2f);
            if (!Check(PassageVisible() && Player.CharacterRenderer.sprite.name.StartsWith(earlierAge),
                "leaving the passage backwards reveals the previous age again")) yield break;
            Capture("06-returned-" + earlierAge);
            yield return Keys(0.1f);
            if (!CheckMemoryBackdrop(earlierAge == "char_child" ? "Kindergarten Memory" : "Stage Memory")) yield break;
            yield return Walk(boundary + clearDistance);
            yield return Keys(0.2f);
            if (!Check(transition.Applications >= 2 && transition.IsCurrentAge && Player.ControlsEnabled,
                "repeating the same crossing restores " + age + " without a one-shot trigger")) yield break;
        }
        yield return Walk(FinalPassageLayout.Centers[2]);
        yield return Keys(1f);
        if (!Check(Player.ControlsEnabled && PassageAlpha() >= 0.99f, "the last cinematic passage also keeps movement available inside full black")) yield break;
        Capture("06-final-threshold");
        yield return Walk(FinalPassageLayout.Centers[2] + clearDistance);
        yield return Keys(0.2f);
        if (!Check(PassageVisible(), "continued walking reveals the last room instead of ending the scene automatically")) yield break;
        if (!CheckMemoryBackdrop("Unknown Wall")) yield break;
        Capture("06-final-emerged");
        phase = "farewell after spaced memory rooms";
        var recipient = EmotionalJourney.Target("young-presenter");
        var ending = FindAnyObjectByType<ReleaseEndingGoal>();
        yield return Walk(recipient.transform.position.x - 0.8f);
        yield return Keys(1.8f, Key.E);
        if (!Check(ending.IsReadyToLeave && Hand.CurrentTarget == recipient,
            "the relocated final recipient still receives the player's reassurance")) yield break;
        yield return new WaitForEndOfFrame();
        if (!CheckHandThread("between the adult and final recipient")) yield break;
        Capture("06-hand-thread-reassurance");
        for (var i = 0; i < 100 && !ending.HasEnteredDoor; i++) yield return Keys(0.1f);
        if (!Check(ending.HasEnteredDoor && Player.ControlsEnabled,
            "the relocated goodbye reaches its actual door and waits for the player's last step")) yield break;
        yield return Walk(ending.OnwardPositionX + 0.2f);
        for (var i = 0; i < 30 && Player.ControlsEnabled; i++) yield return Keys(0.1f);
        if (!Check(!Player.ControlsEnabled,
            "the expanded memory floor and camera still allow the player to complete the ending")) yield break;
    }

    private bool CheckMemoryBackdrop(string expectedRoot)
    {
        var view = Camera.main;
        SpriteRenderer backdrop = null;
        var adjacentHidden = true;
        var halfHeight = view.orthographicSize;
        var halfWidth = halfHeight * view.aspect;
        var center = view.transform.position;
        foreach (var rootName in new[] { "Kindergarten Memory", "Stage Memory", "Research Memory", "Unknown Wall" })
        {
            var root = GameObject.Find(rootName);
            if (root == null) continue;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.color.a <= 0f) continue;
                if (rootName == expectedRoot) backdrop = renderer;
                else adjacentHidden &= renderer.bounds.max.x <= center.x - halfWidth || renderer.bounds.min.x >= center.x + halfWidth;
            }
        }
        var covers = false;
        if (backdrop != null && view != null)
        {
            var bounds = backdrop.bounds;
            // The authored panoramas intentionally have narrow letterboxing. Check their
            // playable picture and floor, rather than requiring a stretched full-height image.
            covers = bounds.min.x <= center.x - halfWidth + 0.02f && bounds.max.x >= center.x + halfWidth - 0.02f &&
                bounds.size.y >= halfHeight * 1.6f && bounds.min.y <= Player.GetComponent<Collider2D>().bounds.min.y + 0.2f;
        }
        return Check(adjacentHidden && covers && backdrop != null && backdrop.enabled,
            "the complete " + expectedRoot + " fills the playable frame, without adjacent-room seams" +
            " (bounds=" + (backdrop == null ? "missing" : backdrop.bounds.ToString()) + ", camera=" + center + ", adjacent=" + adjacentHidden + ")");
    }

    private bool PassageVisible()
    {
        return PassageAlpha() < 0.4f;
    }

    private float PassageAlpha()
    {
        if (SceneManager.GetActiveScene().name == "06_FinalWalk")
        {
            var view = Camera.main;
            var halfHeight = view.orthographicSize;
            var halfWidth = halfHeight * view.aspect;
            var center = view.transform.position;
            var overlap = false;
            foreach (var renderer in FindObjectsByType<SpriteRenderer>())
            {
                if (!renderer.name.StartsWith("Memory Black Passage ") || !renderer.enabled) continue;
                var bounds = renderer.bounds;
                if (renderer.sortingOrder <= Player.CharacterRenderer.sortingOrder || renderer.color.a < 0.999f) return -1f;
                if (bounds.min.x <= center.x - halfWidth && bounds.max.x >= center.x + halfWidth &&
                    bounds.min.y <= center.y - halfHeight && bounds.max.y >= center.y + halfHeight) return 1f;
                overlap |= bounds.min.x < center.x + halfWidth && bounds.max.x > center.x - halfWidth;
            }
            return overlap ? 0.5f : 0f;
        }
        var overlay = FindAnyObjectByType<PassageBlackoutOverlay>();
        if (overlay == null || !overlay.enabled) return 0f;
        var alpha = typeof(PassageBlackoutOverlay).GetProperty("CurrentAlpha");
        return alpha == null ? -1f : (float)alpha.GetValue(overlay);
    }

    private bool CheckLearnerPose(ResearchExpedition research, string pose)
    {
        SpriteRenderer visible = null;
        var count = 0;
        foreach (var renderer in research.Learner.GetComponentsInChildren<SpriteRenderer>())
            if (renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.color.a > 0f)
            { visible = renderer; count++; }
        var sprite = visible == null ? null : visible.sprite;
        var valid = count == 1 && sprite != null &&
            sprite.name == "prop_research_model_" + pose &&
            AssetDatabase.GetAssetPath(sprite.texture) == "Assets/Sprites/prop_research_model.png" &&
            sprite.rect.width < sprite.texture.width * 0.5f &&
            Mathf.Abs(visible.bounds.min.y + 2.72f) < 0.08f;
        return Check(valid, "delivered learner shows one grounded " + pose + " pose during " + research.State +
            " (sprite=" + sprite?.name + ", bottom=" + (visible == null ? "missing" : visible.bounds.min.y.ToString("F2")) + ")");
    }
    private bool Check(bool condition, string message)
    {
        results.Add((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition)
        {
            var stage = FindAnyObjectByType<StagePerformance>();
            var research = FindAnyObjectByType<ResearchExpedition>();
            results.Add("State: player=" + Player?.transform.position + " held=" + Hand?.CurrentTarget +
                " lastBreath=" + Hand?.LastSelfReleaseDuration + " objectives=" + StorySceneDirector.Instance.CompletedObjectives +
                " keyboard=" + Keyboard.current?.deviceId + "/" + keyboard?.deviceId + " E=" + GameInput.InteractHeld +
                (stage != null ? " composed=" + stage.ComposedBeats + " reprise=" + stage.ReprisedBeats + " aim=" + stage.VoiceAim : "") +
                (research != null ? " trial=" + research.State + " gate=" + research.GateOpen + " figure=" + research.Learner.transform.position : ""));
        }
        File.WriteAllLines("Logs/JourneyPlaythroughChecks.txt", results);
        if (!condition) Finish("Stopped during " + phase);
        return condition;
    }
    private void Finish(string result)
    {
        if (finished) return;
        finished = true;
        results.Add(result);
        File.WriteAllLines("Logs/JourneyPlaythroughChecks.txt", results);
        Debug.Log("[LetGo] " + string.Join("\n", results));
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        EditorApplication.isPlaying = false;
    }
    private void OnDestroy()
    {
        Time.maximumDeltaTime = previousMaximumDelta;
        InputSystem.settings.backgroundBehavior = previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
    }
}

#endif
