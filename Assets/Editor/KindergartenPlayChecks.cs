using System;
using System.Collections.Generic;
using LetGo;
using UnityEngine;

/// <summary>Checks that independent play is earned by a supported attempt, not by a timer alone.</summary>
public static class KindergartenPlayChecks
{
    public static List<string> RunChecks()
    {
        var results = new List<string>();
        var lesson = new KindergartenPlayLesson();
        var childX = 10f;
        for (var i = 0; i < 150; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8f, true, false);
        Require(childX == 10f && !lesson.Learned, "running past a stopped toy does not teach independent retrieval");
        for (var i = 0; i < 150; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8f, false, true);
        Require(childX == 10f && !lesson.Learned, "holding the toy or another hand cannot count as leaving space");
        results.Add("PASS: empty hands and attention matter; time alone does not complete shared play.");

        for (var i = 0; i < 24; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8f, true, true);
        Require(lesson.Trying && childX < 9.95f && !lesson.Learned, "the child visibly starts trying before a completed lesson");
        var pausedAt = childX;
        for (var i = 0; i < 50; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8f, true, false);
        Require(childX == pausedAt && lesson.Trying, "rushing away pauses an attempt without erasing its steps");
        for (var i = 0; i < 150; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8f, true, true);
        Require(lesson.Learned && lesson.IndependentRetrievals == 1 && childX <= 8.93f, "returning and waiting lets the actual retrieval finish");
        results.Add("PASS: a tentative first step pauses when rushed, resumes when supported, and records one actual retrieval.");

        lesson = new KindergartenPlayLesson();
        childX = 10f;
        for (var i = 0; i < 200; i++) lesson.TickRetrieval(0.02f, ref childX, 6f, 6f, true, true);
        Require(childX == 10f && !lesson.Trying, "an unreachable toy is recoverable rather than dragging the child across the room");
        for (var i = 0; i < 200; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 3f, true, true);
        Require(childX == 10f && !lesson.Trying, "leaving the child behind does not silently autoplay the trial");
        results.Add("PASS: unreachable throws and an absent player do not manufacture an independent success.");

        for (var i = 0; i < 24; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8f, true, true);
        lesson.CancelAttempt();
        Require(!lesson.Trying && !lesson.Learned && lesson.IndependentRetrievals == 0, "picking the toy up cancels an unfinished attempt without granting its result");
        for (var i = 0; i < 200; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8f, true, true);
        Require(lesson.Learned && lesson.IndependentRetrievals == 1, "a fresh invitation can succeed after a cancelled attempt");
        results.Add("PASS: interrupted attempts can be retried without accumulating fake progress or blocking the child.");

        lesson = new KindergartenPlayLesson();
        childX = 10f;
        for (var i = 0; i < 100; i++) lesson.TickRetrieval(0.02f, ref childX, 9.57f, 8.5f, true, true);
        Require(lesson.Learned, "a toy just outside the direct catch radius must not fall between both interaction paths");
        results.Add("PASS: a throw just short of the child's hand still has a usable independent retrieval.");

        lesson = new KindergartenPlayLesson();
        childX = 10f;
        for (var i = 0; i < 100; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8.7f, true, true);
        Require(childX == 10f && !lesson.Trying, "standing over the toy must leave no room for an independent attempt");
        for (var i = 0; i < 100; i++) lesson.TickRetrieval(0.02f, ref childX, 8.8f, 8.2f, true, true);
        Require(lesson.Learned && lesson.IndependentRetrievals == 1,
            "stepping aside while staying nearby must resolve the blocked attempt without moving the toy");
        results.Add("PASS: waiting on top of the toy cannot help; giving it room immediately makes the same reachable toy usable.");

        lesson = new KindergartenPlayLesson();
        lesson.ObserveRoll(1f, 0f);
        var fastSpeed = lesson.CopiedRollSpeed;
        var rushedWait = lesson.CopiedWaitSeconds;
        var fastTravel = Travel(fastSpeed);
        lesson.ObserveRoll(0.2f, 0.2f);
        var gentleTravel = Travel(lesson.CopiedRollSpeed);
        Require(fastTravel > gentleTravel + 4f && gentleTravel < 1.3f,
            "copying a forceful demonstration must visibly overshoot a nearby partner while a changed gentle demonstration stays reachable");
        Require(lesson.Demonstrations == 2 && lesson.CopiedRollSpeed < fastSpeed - 2f &&
            lesson.CopiedWaitSeconds > rushedWait && !lesson.Learned && lesson.IndependentRetrievals == 0,
            "the new demonstration replaces both copied force and pause without awarding an independent retrieval");
        results.Add("PASS: copied force changes actual roll distance; a gentle re-demonstration replaces the rushed motion without free progress.");

        lesson.ObserveRoll(0.55f, 0f);
        var walkingRelease = lesson.CopiedRollSpeed;
        lesson.ObserveRoll(0.55f, 0.55f);
        Require(Mathf.Abs(lesson.CopiedRollSpeed - walkingRelease) < 0.001f &&
            lesson.CopiedWaitSeconds > rushedWait + 0.3f,
            "the same-force roll after a still pause must preserve its force and copy its calmer timing separately");
        lesson.CancelAttempt();
        Require(lesson.Demonstrations == 4 && lesson.CopiedWaitSeconds > rushedWait + 0.3f,
            "catching or reclaiming the toy must not erase the example the child is learning");
        results.Add("PASS: force and demonstrated pause stay independent, and picking up the toy keeps the learned example.");
        return results;
    }

    private static float Travel(float speed)
    {
        var distance = 0f;
        for (var i = 0; i < 1000 && speed > 0.05f; i++)
        {
            distance += speed * 0.01f;
            speed = Mathf.MoveTowards(speed, 0f, KindergartenPlayLesson.RollingFriction * 0.01f);
        }
        return distance;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Kindergarten play: " + message);
    }
}
