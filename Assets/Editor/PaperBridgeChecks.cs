using System;
using System.Collections.Generic;
using LetGo;

/// <summary>Behavioural checks for construction, real traversal, retained attempts and releasing support.</summary>
public static class PaperBridgeChecks
{
    public static List<string> RunChecks()
    {
        var results = new List<string>();
        var bridge = new PaperBridgeJourney();
        Run(bridge, 10f);
        Require(bridge.Phase == PaperBridgePhase.Editing && !bridge.Ready && bridge.AttemptCount == 0,
            "time spent arranging paper cannot manufacture a crossing");
        Start(bridge);
        Run(bridge, 2f);
        Require(bridge.Phase == PaperBridgePhase.Blocked && bridge.Failure == PaperBridgeFailure.Unsupported &&
            bridge.FailureX > PaperBridgeJourney.LeftBank && bridge.FailureX < 0.13f && !bridge.Ready,
            "unsupported long paper must fail where the walker first leaves the bank");
        Run(bridge, 20f);
        Require(!bridge.Ready && bridge.Completion == PaperBridgeCompletion.None,
            "waiting at a failed construction cannot silently finish it");
        results.Add("PASS: arranging and waiting cannot grant success; unsupported paper stops the real walk at the bank.");

        bridge = Layout(0.20f, 0.50f, 0.80f, true);
        Start(bridge);
        Run(bridge, 8f);
        Require(bridge.IsStable(0) && bridge.IsStable(1) && bridge.IsStable(2) &&
            bridge.Phase == PaperBridgePhase.Blocked && bridge.Failure == PaperBridgeFailure.Gap &&
            bridge.FailureX > 0.33f && bridge.FailureX < 0.36f,
            "three standing feet still need a continuous path between them");
        results.Add("PASS: stable folded feet with an actual missing span fail at that gap, rather than count as a bridge.");

        foreach (var positions in new[] { new[] { 0.20f, 0.50f, 0.80f }, new[] { 0.22f, 0.49f, 0.78f }, new[] { 0.17f, 0.51f, 0.83f } })
        {
            bridge = Layout(positions[0], positions[1], positions[2]);
            Start(bridge);
            Run(bridge, 1f);
            Require(!bridge.Ready && bridge.WalkerX > PaperBridgeJourney.StartX && bridge.WalkerX < PaperBridgeJourney.FinishX,
                "a valid structure still requires the walker to traverse it");
            Run(bridge, 7f);
            Require(bridge.Ready && bridge.Independent && bridge.WalkerX == PaperBridgeJourney.FinishX &&
                Same(bridge.Positions, positions), "different free layouts must work without snapping to preset slots");
        }
        results.Add("PASS: three distinct free layouts remain where placed and become independent only after a full crossing.");

        bridge = new PaperBridgeJourney();
        SelectMiddle(bridge);
        Start(bridge, true);
        Run(bridge, 3f, true);
        Require(bridge.Phase == PaperBridgePhase.Walking && bridge.WalkerX > 0.40f,
            "holding E when F starts the trial must support the selected middle paper immediately");
        var beforeRelease = bridge.WalkerX;
        bridge.Tick(0.02f, 0f, 0, false, false, false, false);
        Require(bridge.Phase == PaperBridgePhase.Blocked && bridge.Failure == PaperBridgeFailure.Unsupported &&
            Math.Abs(bridge.WalkerX - beforeRelease) < 0.0001f && !bridge.Ready && !bridge.IsStable(1),
            "releasing the necessary support must stop the walker on that same frame");
        results.Add("PASS: E held at trial start supports the bridge; releasing it mid-crossing immediately exposes the dependency.");

        bridge = new PaperBridgeJourney();
        SelectMiddle(bridge);
        Start(bridge, true);
        for (var i = 0; i < 400 && bridge.WalkerX < 0.91f; i++) bridge.Tick(0.02f, 0f, 0, true, false, false, false);
        Require(bridge.Phase == PaperBridgePhase.Walking && bridge.WalkerX >= 0.91f && !bridge.Ready,
            "the walker reaches the far bank before the complete state");
        Run(bridge, 1f);
        Require(bridge.Ready && bridge.Completion == PaperBridgeCompletion.Held && !bridge.Independent && !bridge.IsStable(1),
            "releasing on the far bank preserves the help actually used, rather than inventing independence");
        var arrival = bridge.WalkerX;
        Run(bridge, 4f);
        Require(bridge.Ready && bridge.WalkerX == arrival && bridge.Completion == PaperBridgeCompletion.Held,
            "a bridge collapsing after arrival must not erase the person's completed crossing");
        results.Add("PASS: support used during the crossing is remembered after release; later collapse cannot undo arrival.");

        bridge = Layout(0.22f, 0.49f, 0.78f, true);
        Start(bridge);
        Run(bridge, 8f);
        Require(bridge.Phase == PaperBridgePhase.Blocked, "the retry fixture must first encounter a real gap");
        var savedPositions = (float[])bridge.Positions.Clone();
        var savedFolds = (bool[])bridge.Folded.Clone();
        bridge.Tick(0.02f, 0f, 0, false, false, false, true);
        Require(bridge.Phase == PaperBridgePhase.Editing && bridge.WalkerX == PaperBridgeJourney.StartX &&
            bridge.Failure == PaperBridgeFailure.None && bridge.AttemptCount == 1 && Same(bridge.Positions, savedPositions) &&
            Same(bridge.Folded, savedFolds), "Q must return the walker without resetting paper, folds or trial history");
        bridge.Tick(0.02f, 0f, 1, false, true, false, false);
        Require(!bridge.Folded[1], "the middle piece can be unfolded to fix the retained layout");
        Start(bridge);
        Run(bridge, 8f);
        Require(bridge.Ready && bridge.Independent && bridge.AttemptCount == 2 && Same(bridge.Positions, savedPositions),
            "repairing a failed layout must produce a fresh, successful second trial");
        results.Add("PASS: Q retains the construction and attempt history; one actual edit changes the next result.");

        bridge = Layout(0.20f, 0.50f, 0.80f);
        Start(bridge);
        savedPositions = (float[])bridge.Positions.Clone();
        savedFolds = (bool[])bridge.Folded.Clone();
        var selected = bridge.Selected;
        bridge.Tick(0.1f, 1f, 1, true, true, true, false);
        Require(bridge.Phase == PaperBridgePhase.Walking && bridge.Selected == selected && bridge.AttemptCount == 1 &&
            Same(bridge.Positions, savedPositions) && Same(bridge.Folded, savedFolds),
            "move, select, fold and repeated F inputs cannot rewrite or restart a trial while the walker is using it");
        Run(bridge, 8f, true);
        Require(bridge.Ready && bridge.Independent,
            "holding an already self-supporting bridge is not the same as the walker needing that hand");
        results.Add("PASS: a running trial cannot be secretly edited; unnecessary E input does not overwrite earned independence.");

        foreach (var dt in new[] { 0.016f, 0.02f, 0.1f })
        {
            bridge = Layout(0.215f, 0.498f, 0.781f, true);
            Start(bridge);
            Run(bridge, 8f, false, dt);
            Require(bridge.Phase == PaperBridgePhase.Blocked && bridge.Failure == PaperBridgeFailure.Gap &&
                bridge.FailureX > 0.355f && bridge.FailureX < 0.357f && !bridge.Ready,
                "a remaining narrow gap must block at the same seam at every tested frame rate");
            bridge = Layout(0.215f, 0.496f, 0.777f, true);
            Start(bridge);
            Run(bridge, 8f, false, dt);
            Require(bridge.Ready && bridge.Independent,
                "the intentional endpoint allowance must still accept forgiving near joins at every frame rate");
        }
        results.Add("PASS: continuous coverage catches a formerly skipped narrow gap at every frame rate while preserving forgiving joins.");
        return results;
    }

    private static PaperBridgeJourney Layout(float left, float middle, float right, bool foldMiddle = false)
    {
        var bridge = new PaperBridgeJourney();
        bridge.Positions[0] = left;
        bridge.Positions[1] = middle;
        bridge.Positions[2] = right;
        bridge.Folded[0] = bridge.Folded[2] = true;
        bridge.Folded[1] = foldMiddle;
        return bridge;
    }

    private static void Start(PaperBridgeJourney bridge, bool hold = false) =>
        bridge.Tick(0f, 0f, 0, hold, false, true, false);

    private static void SelectMiddle(PaperBridgeJourney bridge) => bridge.Tick(0f, 0f, 1, false, false, false, false);

    private static void Run(PaperBridgeJourney bridge, float seconds, bool hold = false, float dt = 0.02f)
    {
        for (var elapsed = 0f; elapsed < seconds; elapsed += dt) bridge.Tick(dt, 0f, 0, hold, false, false, false);
    }

    private static bool Same<T>(T[] current, T[] expected)
    {
        if (current.Length != expected.Length) return false;
        for (var i = 0; i < current.Length; i++) if (!EqualityComparer<T>.Default.Equals(current[i], expected[i])) return false;
        return true;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Paper bridge: " + message);
    }
}
