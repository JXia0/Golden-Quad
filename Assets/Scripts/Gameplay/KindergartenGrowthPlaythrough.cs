#if UNITY_EDITOR
using System.Collections;
using LetGo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed partial class JourneyPlaythroughDriver
{
    private IEnumerator ReviewKindergartenGrowth()
    {
        yield return ReviewKindergartenImitation();
        if (finished) yield break;
        JourneyChoices.Reset();
        phase = "kindergarten shared play: leave room for the child";
        SceneManager.LoadScene("01_Kindergarten");
        yield return Keys(1.4f);
        var escort = FindAnyObjectByType<ParentEscort>();
        var sharedPlay = FindAnyObjectByType<ComfortToyPlay>();
        var chapter = FindAnyObjectByType<KindergartenChapter>();
        var child = EmotionalJourney.Target("crying-child");
        var toy = EmotionalJourney.Target("comfort-toy");
        var teacher = GameObject.Find("Teacher Safe Area")?.GetComponent<HoldSocket>();
        if (!Check(escort != null && sharedPlay != null && chapter != null && child != null && toy != null && teacher != null,
            "the saved kindergarten installs shared play and its original three classroom tasks")) yield break;
        if (!Check(!JourneyChoices.KindergartenGaveSpace && !sharedPlay.CanWalkTogether,
            "a fresh playthrough has not learned independent play before the child's actions")) yield break;

        for (var i = 0; i < 80 && !escort.WaitingAtDoor; i++) yield return Keys(0.2f, Key.E);
        if (!Check(escort.WaitingAtDoor, "the offered parental hand still leads to the classroom before shared play")) yield break;
        yield return Keys(0.2f);
        yield return Walk(-1.25f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == EmotionalJourney.Target("home-bag"), "the backpack is picked up with the original hand interaction")) yield break;
        yield return Walk(1.8f, true);
        yield return Keys(0.25f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 2, "the independent-play route preserves entrance and backpack progression")) yield break;

        // Cross the same saved chair and blocks as the original route, using real jump edges.
        yield return Keys(0.42f, Key.D, Key.Space);
        yield return Walk(4.8f);
        yield return Keys(0.6f);
        yield return Keys(0.55f, Key.D, Key.Space);
        yield return Walk(8f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == toy, "the player physically picks up the shared toy")) yield break;
        yield return Walk(9.3f, true);
        yield return Keys(0.2f);
        if (!Check(sharedPlay.Exchanges == 1 && StorySceneDirector.Instance.CompletedObjectives == 2,
            "the first roll comes back and leaves a real choice to catch or wait")) yield break;
        Capture("01-growth-return-or-wait");

        var beforeRetrieval = child.transform.position.x;
        var sawTentativeStep = false;
        var stayedEmptyHanded = true;
        for (var i = 0; i < 75 && !sharedPlay.CanWalkTogether; i++)
        {
            // A copied forceful roll can settle closer to the player than the old targeted return.
            // Give the real stopped toy room instead of changing the child's retrieval threshold.
            if (!sharedPlay.IsRolling && !sharedPlay.ChildWinding && !sharedPlay.ChildTrying &&
                Mathf.Abs(Player.transform.position.x - toy.transform.position.x) <= 0.35f)
                yield return Walk(toy.transform.position.x - 0.8f);
            yield return Keys(0.08f);
            stayedEmptyHanded &= Hand.CurrentTarget == null && !child.IsHeld;
            if (!sawTentativeStep && sharedPlay.ChildTrying && Mathf.Abs(child.transform.position.x - beforeRetrieval) > 0.03f)
            {
                sawTentativeStep = true;
                if (!Check(!JourneyChoices.KindergartenGaveSpace && StorySceneDirector.Instance.CompletedObjectives == 2,
                    "the tentative first step is visible before the game awards an independent retrieval")) yield break;
                Capture("01-growth-child-first-step");
            }
        }
        if (!Check(sawTentativeStep && stayedEmptyHanded && Mathf.Abs(child.transform.position.x - beforeRetrieval) > 0.3f,
            "waiting empty-handed lets the child actually cross the space to the returned toy")) yield break;
        if (!Check(sharedPlay.IndependentRetrievals == 1 && sharedPlay.Exchanges == 2 && toy.IsPlaced &&
            StorySceneDirector.Instance.CompletedObjectives == 3 && JourneyChoices.KindergartenGaveSpace,
            "one independent retrieval replaces the second exchange and records the player's way of helping")) yield break;
        Capture("01-growth-child-has-toy");

        phase = "kindergarten shared play: rush ahead, return, and walk together";
        var beforeWalk = child.transform.position.x;
        // Starting here keeps the child below the teacher while the faster player opens a gap.
        yield return Walk(20f);
        yield return Keys(0.2f);
        var stoppedAt = child.transform.position.x;
        if (!Check(Hand.CurrentTarget == null && !child.IsHeld && stoppedAt > beforeWalk + 1f &&
            Player.transform.position.x - stoppedAt > 4.7f && !chapter.FollowingWithoutHand && !teacher.Completed,
            "the child starts following without a hand, then stops when the player runs too far ahead")) yield break;
        yield return Keys(0.6f);
        if (!Check(Mathf.Abs(child.transform.position.x - stoppedAt) < 0.03f && !teacher.Completed,
            "remaining far away does not silently carry the waiting child into the teacher's safe area")) yield break;
        // Bring the waiting child into the camera's edge without yet re-entering following range.
        yield return Walk(stoppedAt + 5f);
        yield return Keys(0.5f);
        Capture("01-growth-waiting-for-you");

        yield return Walk(stoppedAt + 2.7f);
        yield return Keys(0.03f, Key.F);
        yield return Keys(0.02f);
        if (!Check(sharedPlay.IsPracticing && !teacher.Completed &&
            StorySceneDirector.Instance.CompletedObjectives == 3,
            "the child can play again from the later position reached during the independent walk")) yield break;
        var lateChildX = child.transform.position.x;
        for (var i = 0; i < 50 && sharedPlay.ChildWinding; i++) yield return Keys(0.02f);
        yield return Keys(0.04f);
        if (!Check(sharedPlay.IsRolling && toy.transform.position.x > lateChildX + 0.2f &&
            toy.transform.position.x <= lateChildX + 2.05f,
            "the late return rolls toward the nearby player without snapping back to the child's old play area")) yield break;
        Capture("01-growth-later-position-play");
        yield return Walk(lateChildX + 5.9f);
        if (!Check(!sharedPlay.IsPracticing && toy.IsPlaced && sharedPlay.CanWalkTogether &&
            StorySceneDirector.Instance.CompletedObjectives == 3 && JourneyChoices.KindergartenGaveSpace,
            "leaving late play restores the toy and the earned independent walk without blocking the teacher")) yield break;
        yield return Walk(child.transform.position.x + 2.7f);
        for (var i = 0; i < 60 && !teacher.Completed; i++) yield return Keys(0.1f);
        if (!Check(Hand.CurrentTarget == null && !child.IsHeld && child.transform.position.x > stoppedAt + 0.5f &&
            teacher.Completed && StorySceneDirector.Instance.ObjectivesComplete,
            "returning within sight lets the child finish the walk and enter the teacher's socket without a hand release")) yield break;
        if (!Check(sharedPlay.IndependentRetrievals == 1 && JourneyChoices.KindergartenGaveSpace,
            "recovering from a rushed walk keeps the actual lesson and does not add another play errand")) yield break;
        Capture("01-growth-arrive-together");

        phase = "kindergarten shared play: carry the decision into the next scene";
        yield return UseDoor("Classroom Exit", "02_Interlude_Firsts");
        if (finished) yield break;
        if (!Check(JourneyChoices.KindergartenGaveSpace,
            "the way the player made room for the child survives the actual classroom exit")) yield break;
    }

    private IEnumerator ReviewKindergartenImitation()
    {
        JourneyChoices.Reset();
        phase = "kindergarten imitation: actual forceful demonstration and catch";
        SceneManager.LoadScene("01_Kindergarten");
        yield return Keys(1.4f);
        var escort = FindAnyObjectByType<ParentEscort>();
        var sharedPlay = FindAnyObjectByType<ComfortToyPlay>();
        var child = EmotionalJourney.Target("crying-child");
        var toy = EmotionalJourney.Target("comfort-toy");
        var teacher = GameObject.Find("Teacher Safe Area")?.GetComponent<HoldSocket>();
        if (!Check(escort != null && sharedPlay != null && child != null && toy != null && teacher != null,
            "imitation uses the saved kindergarten's real toy, child and teacher")) yield break;
        for (var i = 0; i < 80 && !escort.WaitingAtDoor; i++) yield return Keys(0.2f, Key.E);
        if (!Check(escort.WaitingAtDoor, "imitation begins after the parent reaches the classroom door")) yield break;
        yield return Keys(0.2f);
        yield return Walk(-1.25f);
        yield return Keys(0.2f, Key.E);
        yield return Walk(1.8f, true);
        yield return Keys(0.25f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 2,
            "the imitation route earns the entrance and backpack objectives")) yield break;
        yield return Keys(0.42f, Key.D, Key.Space);
        yield return Walk(4.8f);
        yield return Keys(0.6f);
        yield return Keys(0.55f, Key.D, Key.Space);
        yield return Walk(8f);
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == toy, "imitation begins with a real toy pickup")) yield break;
        yield return Walk(9.3f, true);
        yield return Keys(0.85f, Key.E);
        var demonstratedForce = KindergartenPlayLesson.RollSpeed(toy.HeldDuration);
        yield return Keys(0.02f);
        for (var i = 0; i < 100 && sharedPlay.Exchanges == 0; i++) yield return Keys(0.02f);
        if (!Check(sharedPlay.Exchanges == 1 && sharedPlay.ChildWinding &&
            Mathf.Abs(sharedPlay.CopiedRollSpeed - demonstratedForce) < 0.01f && sharedPlay.CopiedWaitSeconds >= 0.5f &&
            StorySceneDirector.Instance.CompletedObjectives == 2,
            "the child receives the real roll, copies its full force, and waits after the player's still demonstration")) yield break;
        var strongWait = sharedPlay.CopiedWaitSeconds;
        var windingAt = toy.transform.position;
        yield return Keys(0.15f);
        if (!Check(sharedPlay.ChildWinding && Vector3.Distance(toy.transform.position, windingAt) < 0.03f,
            "the copied pause holds the actual toy still before the child's return")) yield break;
        Capture("01-imitation-copied-pause");
        for (var i = 0; i < 50 && sharedPlay.ChildWinding; i++) yield return Keys(0.02f);
        var strongAt = toy.transform.position.x;
        var strongStartedAt = Time.time;
        yield return Keys(0.14f);
        var strongMotion = Mathf.Abs(toy.transform.position.x - strongAt) / (Time.time - strongStartedAt);
        if (!Check(sharedPlay.IsRolling && strongMotion > 3f,
            "the child's forceful return moves the actual toy quickly instead of auto-targeting the player's hand")) yield break;
        Capture("01-imitation-forceful-return");
        yield return CatchKindergartenReturn(toy);
        if (finished) yield break;
        yield return Walk(child.transform.position.x - 0.95f, true);
        yield return Keys(0.04f, Key.D, Key.E);
        yield return Keys(0.8f, Key.E);
        yield return Keys(0.02f);
        for (var i = 0; i < 100 && sharedPlay.Exchanges < 2; i++) yield return Keys(0.02f);
        var socket = child.GetComponent<HoldSocket>();
        if (!Check(sharedPlay.Exchanges == 2 && toy.IsPlaced && socket.Completed && socket.Occupant == toy &&
            StorySceneDirector.Instance.CompletedObjectives == 3 && !JourneyChoices.KindergartenGaveSpace,
            "moving to catch and rolling back still completes the original two-exchange path exactly once")) yield break;

        phase = "kindergarten imitation: optional gentler re-demonstration";
        yield return Walk(child.transform.position.x - 1.05f);
        yield return Keys(0.025f, Key.D);
        yield return Keys(0.04f);
        var returns = 0;
        System.Action countReturn = () => returns++;
        sharedPlay.ChildReturnedToy += countReturn;
        yield return Keys(0.03f, Key.F);
        yield return Keys(0.02f);
        if (!Check(sharedPlay.IsPracticing && !toy.IsPlaced && socket.Completed &&
            StorySceneDirector.Instance.CompletedObjectives == 3 && sharedPlay.Exchanges == 2,
            "F opens optional play with the earned socket and objective preserved while the toy is in motion")) yield break;
        yield return CatchKindergartenReturn(toy);
        if (finished) yield break;
        // Catching can involve following the return leftward. Turn through real movement input
        // while retaining E, then steady the toy before demonstrating toward the child.
        var towardChild = child.transform.position.x >= Player.transform.position.x ? Key.D : Key.A;
        yield return Keys(0.025f, towardChild, Key.E);
        yield return Keys(0.06f, Key.E);
        if (!Check(Hand.CurrentTarget == toy &&
            Player.FacingDirection * (child.transform.position.x - Player.transform.position.x) > 0f,
            "after following the toy the player turns toward the child with real movement input before the gentle release")) yield break;
        var previousReturns = returns;
        yield return Keys(0.02f);
        // HandConnection also ticks HeldDuration on the actual release frame. Measure the
        // completed hold after that input is consumed, not one frame before it ends.
        var gentleForce = KindergartenPlayLesson.RollSpeed(toy.HeldDuration);
        for (var i = 0; i < 120 && returns == previousReturns; i++) yield return Keys(0.02f);
        if (!Check(returns == previousReturns + 1 && sharedPlay.ChildWinding &&
            Mathf.Abs(sharedPlay.CopiedRollSpeed - gentleForce) < 0.01f && gentleForce < demonstratedForce * 0.65f &&
            sharedPlay.CopiedWaitSeconds < strongWait && sharedPlay.CopiedWaitSeconds > 0.1f,
            "a real brief pickup and gentle release replace both force and timing in the child's next return" +
            " (returns=" + returns + "/" + (previousReturns + 1) + ", force=" + sharedPlay.CopiedRollSpeed.ToString("F3") +
            "/" + gentleForce.ToString("F3") + ", wait=" + sharedPlay.CopiedWaitSeconds.ToString("F3") +
            "/" + strongWait.ToString("F3") + ", winding=" + sharedPlay.ChildWinding +
            ", facing=" + Player.FacingDirection + ", toyX=" + toy.transform.position.x.ToString("F2") +
            ", childX=" + child.transform.position.x.ToString("F2") + ")")) yield break;
        Capture("01-imitation-gentle-demonstration");
        for (var i = 0; i < 50 && sharedPlay.ChildWinding; i++) yield return Keys(0.02f);
        var gentleAt = toy.transform.position.x;
        var gentleStartedAt = Time.time;
        yield return Keys(0.14f);
        var gentleMotion = Mathf.Abs(toy.transform.position.x - gentleAt) / (Time.time - gentleStartedAt);
        if (!Check(sharedPlay.IsRolling && gentleMotion > 0.5f && gentleMotion < strongMotion - 0.8f &&
            StorySceneDirector.Instance.CompletedObjectives == 3 && !JourneyChoices.KindergartenGaveSpace,
            "the corrected return is visibly slower, without extra objectives or an unearned independent retrieval")) yield break;
        Capture("01-imitation-gentle-return");
        sharedPlay.ChildReturnedToy -= countReturn;

        phase = "kindergarten imitation: leave optional play and finish the classroom";
        yield return Walk(child.transform.position.x + 6.2f);
        if (!Check(!sharedPlay.IsPracticing && toy.IsPlaced && socket.Completed && socket.Occupant == toy &&
            sharedPlay.Exchanges == 2 && StorySceneDirector.Instance.CompletedObjectives == 3,
            "walking away ends optional play and restores toy placement without clearing or duplicating earned progress")) yield break;
        yield return Walk(child.transform.position.x + 1.3f);
        yield return Keys(0.9f, Key.E);
        if (!Check(Hand.CurrentTarget == child, "the child's hand remains available after optional play ends")) yield break;
        for (var i = 0; i < 80 && child.transform.position.x < teacher.transform.position.x - 1.2f; i++)
        {
            yield return Keys(0.14f, Key.E, Key.D);
            yield return Keys(0.23f, Key.E);
        }
        yield return Keys(0.2f);
        if (!Check(teacher.Completed && StorySceneDirector.Instance.ObjectivesComplete && toy.AllowReclaim,
            "after re-demonstrating and walking away the original teacher delivery still finishes and permits toy reclaim")) yield break;
        Capture("01-imitation-continue-to-teacher");
    }

    private IEnumerator CatchKindergartenReturn(HoldTarget toy)
    {
        var until = Time.time + 8f;
        while (Hand.CurrentTarget != toy && Time.time < until)
        {
            if (HoldTarget.FindNearest(Player.transform.position) == toy)
                yield return Keys(0.12f, Key.E);
            else
                yield return Keys(0.04f, Player.transform.position.x < toy.transform.position.x ? Key.D : Key.A);
        }
        Check(Hand.CurrentTarget == toy, "the player follows the real returning toy and catches it with an E press");
    }
}
#endif
