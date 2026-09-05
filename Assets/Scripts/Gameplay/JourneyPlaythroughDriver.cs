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

public sealed class JourneyPlaythroughDriver : MonoBehaviour
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
        yield return Keys(3f);
        if (!Check(performance.ReadyForCurtain && !StorySceneDirector.Instance.ObjectivesComplete,
            "listening to the audience is a complete alternate performance and still waits for a bow")) yield break;
        if (!Check(stageFeedback.PlayedNotes == notesBefore + 2 && stageFeedback.PlayedAudienceAnswers == answersBefore + 2,
            "the audience-led phrase actually produces both remembered notes and responses")) yield break;
        yield return Keys(0.2f, Key.F);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete && JourneyChoices.StageStyle.Contains("幕布") &&
            !JourneyChoices.FirstBeatLong && JourneyChoices.SecondBeatLong, "the final revised phrase is retained after the audience-led performance")) yield break;

        phase = "workshop crate and held crossing";
        yield return UseDoor("Back Curtain", "04_Interlude_Growing");
        if (finished) yield break;
        yield return PlayRepair(repairRuns++ == 0);
        if (finished) yield break;
        yield return WorkshopHeld();
        if (finished) yield break;
        phase = "final farewell";
        yield return Walk(36f);
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
        yield return Keys(0.2f);
        if (!Check(recipient.transform.position.x > initialX, "recipient walks away after release")) yield break;
        yield return Walk(recipient.transform.position.x - 0.6f);
        yield return Keys(0.3f, Key.E);
        var pausedX = recipient.transform.position.x;
        yield return Keys(0.8f, Key.E);
        if (!Check(ending.TimesReheld == 1 && ending.IsHeldBack && Mathf.Abs(recipient.transform.position.x - pausedX) < 0.01f,
            "reaching back pauses departure without resetting the goodbye")) yield break;
        yield return Walk(recipient.transform.position.x + 2.25f, true);
        var waitingX = Player.transform.position.x;
        yield return Keys(3f);
        if (!Check(ending.HasEnteredDoor, "the second release lets the recipient finish entering the door")) yield break;
        if (!Check(Player.ControlsEnabled, "the ending waits for the player's own last step")) yield break;
        if (!Check(ending.OnwardPositionX >= waitingX + 0.75f, "walking ahead before goodbye cannot skip the final step with empty hands")) yield break;
        yield return Walk(ending.OnwardPositionX + 0.2f);
        yield return Keys(0.5f);
        if (!Check(!Player.ControlsEnabled, "walking onward triggers the final fade")) yield break;
        yield return Keys(3.5f);
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
        phase = "growing playable interlude";
        var moment = FindAnyObjectByType<InterludeController>();
        if (!Check(moment != null && moment.HasIllustration, "the second interlude displays the rejected-work artwork")) yield break;
        if (!CheckInterludeText()) yield break;
        Capture("04-paper-before");
        for (var i = 0; i < 3; i++)
        {
            if (i > 0) { yield return Keys(0.12f, Key.D); yield return Keys(0.12f); }
            yield return Keys(0.12f, Key.E);
            if (!Check(moment.Repair.Holding && moment.Repair.Selected == i, "pick up paper fragment " + i)) yield break;
            if (i == 1) Capture("04-paper-in-hand");
            yield return MovePaper(moment, RepairJourney.Target(i), false);
            yield return Keys(0.15f);
            if (!Check(moment.Repair.RepairedCount == i + 1, "align and release paper fragment " + i)) yield break;
        }
        if (!Check(!moment.Ready, "repairing the page still leaves its destination to the player")) yield break;
        yield return Keys(0.12f, Key.E);
        yield return MovePaper(moment, send ? 0.2f : 0.8f, true);
        yield return Keys(0.3f);
        if (!Check(moment.Ready && JourneyChoices.RepairedNoteDestination == (send ? "home" : "notebook"), "the final placement remembers whether the page was sent or kept")) yield break;
        Capture(send ? "04-restored-sent" : "04-restored-kept");
        yield return Keys(0.3f);
        yield return Keys(0.15f, Key.F);
        yield return Keys(1.4f);
        Check(SceneManager.GetActiveScene().name == "05_Research", "the restored growing interlude leads into research");
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
        yield return Keys(2f);
        if (!Check(research.State == LearnerState.NeedsBridge, "trying before preparation exposes the missing crossing")) yield break;
        if (!Check(guide.Step == ResearchGuideStep.Bridge && guide.SituationText.Contains("落脚点") && guide.GoalText.Contains("缺口"), "the visible goal explains why the learner stopped at the gap")) yield break;
        Capture("05-guide-gap");
        yield return Carry(research.Crate, 16f);
        if (finished) yield break;
        yield return Keys(2f);
        if (!Check(research.State == LearnerState.NeedsComfort, "the same crate solves a second problem but cannot solve darkness")) yield break;
        if (!Check(guide.Step == ResearchGuideStep.Comfort && guide.SituationText.Contains("暗处"), "the guide distinguishes fear of darkness from a missing crossing")) yield break;
        yield return Keys(0.5f, Key.D, Key.Space);
        yield return Walk(research.Learner.transform.position.x - 0.4f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Learner, "take the learner's hand at the dark threshold")) yield break;
        yield return Walk(24f, true);
        yield return Keys(1.2f, Key.E);
        yield return Keys(0.3f);
        if (!Check(research.ShortcutOpen && !JourneyChoices.LearnedIndependentDeparture && JourneyChoices.CrossingTool == "crate", "accompanying the learner remembers a distinct solution")) yield break;
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
            if (!Check(coverage != null && coverage.gameObject.activeInHierarchy && coverage.startWidth > 0f && coverage.startColor.a > 0f && FindAnyObjectByType<ResearchExpedition>().ShowingLampRange && coverage.positionCount == 4,
                "carrying the lamp shows a floor-level reach marker instead of a screen-sized circle")) yield break;
            Capture("05-lamp-placement");
        }
        yield return Walk(dropX - item.CarryOffset.x, true);
        yield return Keys(0.3f, Key.E);
        yield return Keys(0.3f);
        Check(Mathf.Abs(item.transform.position.x - dropX) < 0.3f, "reuse " + item.TargetId + " at " + dropX);
    }

    private IEnumerator ReturnReport(ResearchExpedition research)
    {
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
        yield return Walk(35.6f);
        yield return Keys(0.4f);
        var ending = FindAnyObjectByType<ReleaseEndingGoal>();
        Check(ending.InitiatesOwnDeparture && ending.IsDeparting && Hand.CurrentTarget == null, "the previous independent solution makes the next person initiate goodbye without being prompted by a hand hold");
    }

    private IEnumerator Walk(float x, bool hold = false)
    {
        var timeout = Time.time + 18f;
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
