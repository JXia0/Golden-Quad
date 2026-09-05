using System;
using System.Collections.Generic;
using LetGo;
using UnityEngine;

/// <summary>Deterministic behavioural checks for demonstration, environment changes and reteaching.</summary>
public static class LearnedHabitChecks
{
    public static List<string> RunChecks()
    {
        var results = new List<string>();
        var recording = new WorkshopDemonstration();
        recording.Begin(new Vector2(12f, -2.1f));
        recording.Sample(0.6f, new Vector2(15f, -2.1f), new Vector2(3.8f, 0f), false, "");
        recording.Sample(0.02f, new Vector2(15f, -2.1f), new Vector2(3.8f, 9.2f), true, "");
        recording.Sample(0.2f, new Vector2(15.8f, -1.1f), new Vector2(3.8f, 2f), false, "");
        recording.Sample(0.1f, new Vector2(16.2f, -0.9f), new Vector2(3.8f, 0f), false, "");
        Require(recording.Actions.Count == 0, "the apex must not be recorded as a landing");
        recording.Sample(0.2f, new Vector2(17f, -1.2f), new Vector2(3.8f, -5f), false, "");
        recording.Sample(0.2f, new Vector2(17.6f, -2.1f), new Vector2(3.8f, 0f), false, "");
        Require(recording.Actions.Count == 1 && recording.Actions[0].CrossesGap, "a real takeoff and landing span the gap");
        recording.Sample(0.1f, new Vector2(18f, -2.1f), Vector2.zero, false, "");
        for (var i = 0; i < 90; i++) recording.Sample(0.02f, new Vector2(18f, -2.1f), Vector2.zero, false, "", i >= 60);
        Require(recording.Finish(), "a route beyond the gap is a valid partial demonstration");
        Require(recording.Actions.Count == 2 && recording.Actions[1].SteadiesInDark, "a charged self-anchor is remembered with its actual pause");
        results.Add("PASS: actual input-derived takeoff, descent, landing and charged self-anchor become a usable lesson.");

        var learner = new ExpeditionLearner();
        learner.Teach(recording.Actions);
        recording.Begin(new Vector2(12f, -2.1f));
        recording.Cancel();
        Require(learner.Lesson.Count == 2, "cancelling a new take cannot erase the learned lesson");
        learner.Start();
        var sawHop = false;
        var pausedSeconds = 0f;
        for (var i = 0; i < 1000 && learner.State != LearnerState.Arrived; i++)
        {
            learner.Tick(0.02f, "", false, 12f, false, false);
            sawHop |= learner.Height > 0.2f;
            if (learner.State == LearnerState.Pausing) pausedSeconds += 0.02f;
        }
        Require(learner.State == LearnerState.Arrived && learner.Crossing == "jump" && learner.Support == "breath" && learner.Independent,
            "a demonstrated jump and self-anchor create a genuinely tool-free route");
        Require(sawHop && pausedSeconds >= 1.2f && learner.ExecutedHabit.Kind == LearnedHabitKind.Jump,
            "the learner visibly jumps, spends time pausing, and remembers an action it actually performed");
        results.Add("PASS: learned jump and self-anchor solve both obstacles without a crate, plank, lamp or hand.");

        learner = new ExpeditionLearner();
        learner.Teach(new[] { new DemonstratedAction(LearnedHabitKind.Jump, 15.1f, 0.5f, 16.1f, 0.5f) });
        learner.Start();
        Run(learner, 6f, "", false);
        Require(learner.State == LearnerState.NeedsBridge, "a short jump cannot invent a crossing");
        results.Add("PASS: a jump that does not land across the gap fails recoverably at the gap.");

        learner = new ExpeditionLearner();
        learner.Teach(new[] { new DemonstratedAction(LearnedHabitKind.Pause, 18f, 2f) });
        learner.Start();
        Run(learner, 8f, "plank", false);
        Require(learner.State == LearnerState.NeedsComfort, "ordinary idling must not manufacture a self-anchor action");
        results.Add("PASS: merely standing still is an imitated pause, not invented courage.");

        var helped = new[] { new DemonstratedAction(LearnedHabitKind.SeekHelp, 17.8f, 0f, help: "lamp") };
        learner = new ExpeditionLearner();
        learner.Teach(helped);
        learner.Start();
        Run(learner, 8f, "crate", false);
        Require(learner.State == LearnerState.NeedsComfort, "a lesson given with light leaves the learner waiting when light is absent");
        Run(learner, 8f, "crate", true);
        Require(learner.State == LearnerState.Arrived && learner.ExecutedHabit.Kind == LearnedHabitKind.SeekHelp && learner.ExecutedHabit.HelpKind == "lamp",
            "restoring real light resolves the learned dependency and records the actual source");
        learner.Recall();
        Run(learner, 5f, "crate", false);
        learner.Start();
        Run(learner, 8f, "crate", false);
        Require(learner.State == LearnerState.NeedsComfort, "ordinary recall must not silently overwrite an explicitly demonstrated dependency");
        learner.Recall();
        Run(learner, 5f, "", false);
        learner.Teach(new[] {
            new DemonstratedAction(LearnedHabitKind.Jump, 15f, 0.8f, 17f, 1f),
            new DemonstratedAction(LearnedHabitKind.Pause, 18f, 1.6f, selfSteadied: true)
        });
        learner.Start();
        Run(learner, 12f, "", false);
        Require(learner.State == LearnerState.Arrived && learner.Support == "breath" && learner.Crossing == "jump" &&
            learner.ExecutedHabit.Kind == LearnedHabitKind.Jump && learner.ExecutedHabit.Demonstrations == 2,
            "reteaching replaces the habit and the later result instead of adding a scripted objective");
        results.Add("PASS: changed light exposes dependency; restoring help recovers; reteaching changes the next attempt and ending snapshot.");

        recording.Begin(new Vector2(12f, -2.1f));
        recording.Sample(1f, new Vector2(18f, -2.1f), new Vector2(3.8f, 0f), false, "lamp");
        Require(recording.Finish() && recording.Actions.Count == 1 && recording.Actions[0].Kind == LearnedHabitKind.SeekHelp,
            "carrying a lamp into darkness records source reliance without synthesizing jumps");
        recording.Begin(new Vector2(12f, -2.1f));
        recording.Sample(0.1f, new Vector2(12f, -2.1f), Vector2.zero, false, "");
        Require(!recording.Finish(), "an empty or too-short demonstration cannot replace the lesson");
        results.Add("PASS: carried help is recorded, while an empty demonstration is rejected.");
        return results;
    }

    private static void Run(ExpeditionLearner learner, float seconds, string bridge, bool light)
    {
        for (var i = 0; i < Mathf.CeilToInt(seconds / 0.02f); i++) learner.Tick(0.02f, bridge, false, 12f, light, false);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Learned habits: " + message);
    }
}
