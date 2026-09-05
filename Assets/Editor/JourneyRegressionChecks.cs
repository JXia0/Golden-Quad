using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LetGo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Non-destructive editor regression checks. Does not save or rebuild game scenes.</summary>
[InitializeOnLoad]
public static class JourneyRegressionChecks
{
    private const string RequestPath = "Logs/run-journey-checks.request";
    private const string ResultPath = "Logs/JourneyRegressionChecks.txt";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static JourneyRegressionChecks() => EditorApplication.update += CheckRequest;

    private static void CheckRequest()
    {
        if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(RequestPath);
        Run();
    }

    [MenuItem("Tools/Let Go/Validate Emotional Journey")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        for (var i = 0; i < SceneManager.sceneCount; i++)
            if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
            { Debug.LogWarning("[LetGo] Save the untitled scene before running regression checks."); return; }
        var results = new List<string>();
        var active = SceneManager.GetActiveScene();
        var previousDirector = StorySceneDirector.Instance;
        var previousAudio = SceneAudio.Instance;
        var fixture = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(fixture);
        try
        {
            SetStatic(typeof(SceneAudio), "Instance", null);
            var player = new GameObject("Regression Player", typeof(PlayerController2D), typeof(HandConnection));
            Call(player.GetComponent<PlayerController2D>(), "Awake");
            var hand = player.GetComponent<HandConnection>();
            Call(hand, "Awake");
            var director = new GameObject("Regression Director").AddComponent<StorySceneDirector>();
            var checkpoint = new GameObject("Regression Checkpoint").transform;
            director.Configure("", player.transform, checkpoint, 10, null, null, null, null, null);
            SetStatic(typeof(StorySceneDirector), "Instance", director);

            var socket = new GameObject("Draft socket").AddComponent<HoldSocket>();
            socket.transform.position = new Vector3(1000, 0);
            socket.Configure("", "draft", 1.8f, 0, "");
            socket.Replaceable = true;
            Call(socket, "OnEnable");
            var a = Target("a", HoldTargetMode.Carryable, new Vector3(1003, 0), "draft");
            var b = Target("b", HoldTargetMode.Carryable, new Vector3(1006, 0), "draft");
            a.transform.position = socket.transform.position;
            Require(HoldSocket.TryPlace(a), "first draft accepted");
            b.transform.position = socket.transform.position;
            Require(HoldSocket.TryPlace(b), "replacement accepted");
            Require(director.CompletedObjectives == 1 && socket.Occupant == b && !a.IsPlaced && a.transform.position.x == 1003,
                "revising returns the old evidence without double-counting objectives");
            results.Add("PASS: evidence replacement returns previous card and counts one objective.");
            socket.Locked = true;
            a.transform.position = socket.transform.position;
            Require(!HoldSocket.TryPlace(a), "committed draft cannot change");
            results.Add("PASS: final commitment locks further evidence replacement.");

            socket.Locked = false;
            Call(hand, "Attach", a);
            hand.CancelConnection();
            Require(hand.CurrentTarget == null && hand.LastReleasedTarget == null && socket.Occupant == b && a.transform.position.x == 1003,
                "reset does not submit or emit a release");
            results.Add("PASS: cancel/reset returns the carried card and never submits it.");
            a.transform.position = new Vector3(1020, 10);
            a.EndHold();
            Require(a.transform.position.y == 0 && a.transform.position.x == 1020, "airborne drop remains reachable");
            results.Add("PASS: dropping in mid-air puts the item back within ground-level reach.");

            var child = Target("child", HoldTargetMode.Companion, new Vector3(1100, 0));
            child.GentleCompanion = true;
            child.BeginHold(hand);
            player.transform.position = new Vector3(1102.3f, 0);
            child.TickHeld(hand, 0.1f);
            Require(child.IsWaiting && child.transform.position.x == 1100, "a stretched hand does not drag the child");
            player.transform.position = new Vector3(1101.4f, 0);
            child.TickHeld(hand, 0.4f);
            Require(child.IsWaiting, "reassurance takes time");
            child.TickHeld(hand, 0.5f);
            Require(!child.IsWaiting && child.transform.position.x > 1100, "coming back and waiting lets the child walk again");
            results.Add("PASS: child stops under tension and resumes only after nearby reassurance.");

            var parent = Target("parent-test", HoldTargetMode.Support, new Vector3(1200, 0));
            Call(hand, "Attach", parent);
            player.transform.position = new Vector3(1210, 6);
            Call(hand, "ConstrainDistance");
            Require(Mathf.Abs(player.transform.position.x - 1202.5f) < 0.01f && player.transform.position.y == 6,
                "tether must not teleport the player vertically");
            hand.CancelConnection();
            results.Add("PASS: hand distance constrains horizontal position without snapping vertical position.");

            var courage = player.AddComponent<CourageSystem>();
            Call(courage, "Awake");
            courage.Configure(0.1f, 1f);
            courage.Draining = true;
            typeof(CourageSystem).GetField("courage", Private).SetValue(courage, 0f);
            var position = player.transform.position;
            Call(courage, "Update");
            Require(player.transform.position == position, "fear cannot respawn the player");
            results.Add("PASS: zero courage does not reset the player.");
            var selfReleaseBefore = hand.LastSelfReleaseTime;
            typeof(HandConnection).GetField("selfAnchoring", Private).SetValue(hand, true);
            typeof(HandConnection).GetField("selfCharge", Private).SetValue(hand, 1.2f);
            hand.CancelConnection();
            Require(hand.LastSelfReleaseTime == selfReleaseBefore && !hand.IsSelfAnchoring,
                "reset cannot play a charged stage note");
            results.Add("PASS: cancelling a full breath emits no note or rhythm progress.");
            var entry = new GameObject("Independent school entry").AddComponent<ReleaseGate>();
            entry.Configure(hand, "parent-test", 1200f, "");
            var beforeEntry = director.CompletedObjectives;
            Call(entry, "Update");
            Call(entry, "Update");
            Require(director.CompletedObjectives == beforeEntry + 1,
                "independent entry is possible without a parental hand checklist and counts only once");
            results.Add("PASS: parental help is optional and independent entry counts once.");
            ValidateExperiments(results);
            ValidateInterludes(results);
            ValidateScenes(results);
            results.Add("ALL CHECKS PASSED. Editor logic/topology checks; manual playthrough still required.");
        }
        catch (Exception error)
        {
            results.Add("FAIL: " + error);
            Debug.LogError("[LetGo] Emotional Journey regression check failed: " + error.Message);
        }
        finally
        {
            foreach (var root in fixture.GetRootGameObjects())
            {
                foreach (var target in root.GetComponentsInChildren<HoldTarget>()) Call(target, "OnDisable");
                foreach (var socket in root.GetComponentsInChildren<HoldSocket>()) Call(socket, "OnDisable");
            }
            EditorSceneManager.CloseScene(fixture, true);
            if (active.IsValid()) SceneManager.SetActiveScene(active);
            SetStatic(typeof(StorySceneDirector), "Instance", previousDirector);
            SetStatic(typeof(SceneAudio), "Instance", previousAudio);
            Directory.CreateDirectory("Logs");
            File.WriteAllLines(ResultPath, results);
            Debug.Log("[LetGo] " + string.Join("\n", results));
        }
    }

    private static void ValidateExperiments(List<string> results)
    {
        var walker = new ExpeditionLearner();
        for (var i = 0; i < 200; i++) walker.Tick(0.05f, "crate", false, 23f, true, false);
        Require(walker.X == 12f, "the learner waits for player initiative");
        walker.Start();
        for (var i = 0; i < 100; i++) walker.Tick(0.05f, "", false, 23f, true, false);
        Require(walker.State == LearnerState.NeedsBridge, "no crossing without a placed tool");
        for (var i = 0; i < 80; i++) walker.Tick(0.05f, "plank", false, 23f, false, false);
        Require(walker.State == LearnerState.NeedsComfort, "the learner stops at darkness");
        for (var i = 0; i < 30; i++)
        {
            walker.Tick(0.016f, "plank", false, 23f, false, false);
            Require(walker.State == LearnerState.NeedsComfort && Mathf.Abs(walker.X - 17.8f) < 0.01f,
                "waiting without support must not oscillate between moving and stopping");
        }
        for (var i = 0; i < 10; i++) walker.Tick(0.05f, "plank", false, 23f, true, false);
        var before = walker.X;
        walker.Tick(0.2f, "plank", false, 23f, false, false);
        Require(walker.X < before && walker.Retreats == 1, "removing support makes the learner retreat");
        for (var i = 0; i < 100; i++) walker.Tick(0.05f, "plank", false, 23f, true, false);
        Require(walker.State == LearnerState.Arrived && walker.Independent && walker.Support == "lamp" && walker.Crossing == "plank", "lamp and plank enable independent crossing");
        results.Add("PASS: missing tools stop progress; withdrawing light causes a recoverable retreat; lamp and plank allow independent crossing.");
        walker = new ExpeditionLearner();
        for (var i = 0; i < 200; i++) walker.Tick(0.05f, "crate", true, 24f, false, false);
        Require(walker.State == LearnerState.Arrived && !walker.Independent && walker.Support == "hand" && walker.Crossing == "crate", "crate and hand are a complete alternate solution");
        results.Add("PASS: crate and hand solve the same space with a different remembered outcome.");
        walker = new ExpeditionLearner(); walker.Start();
        for (var i = 0; i < 200; i++) walker.Tick(0.05f, "crate", false, 24f, false, true);
        Require(walker.State == LearnerState.Arrived && walker.Independent && walker.Support == "toy", "a remembered toy substitutes for light");
        results.Add("PASS: the childhood toy is a functional substitute for the lamp.");
    }

    private static void ValidateInterludes(List<string> results)
    {
        var bus = new FirstsJourney();
        for (var i = 0; i < 500; i++) bus.Tick(0.1f, 0f, false, false);
        Require(bus.Phase == FirstsPhase.Riding, "waiting alone cannot complete a journey");
        bus = new FirstsJourney(); bus.Bell(); bus.Bell();
        Require(bus.RequestedStop == -1, "a stop request can be reconsidered");
        bus.Bell();
        for (var i = 0; i < 30; i++) bus.Tick(0.1f, 0f, false, false);
        Require(bus.ExitStop == 0 && bus.Phase == FirstsPhase.Walking, "early stop creates a walk to school");
        for (var i = 0; i < 15; i++) bus.Tick(0.1f, 1f, false, false);
        Require(bus.Phase == FirstsPhase.RaisingHand, "walking reaches the classroom");
        bus.Tick(1.3f, 0, true, false); bus.Tick(0.01f, 0, false, true);
        Require(bus.Phase == FirstsPhase.Ready, "raising then releasing a hand finishes the first transition");
        bus = new FirstsJourney(); bus.Tick(6f, 0, false, false); bus.Bell();
        for (var i = 0; i < 30; i++) bus.Tick(0.1f, 0, false, false);
        Require(bus.ExitStop == 2, "a later bell results in a different stop");
        for (var i = 0; i < 15; i++) bus.Tick(0.1f, -1f, false, false);
        Require(bus.Phase == FirstsPhase.RaisingHand, "a late stop remains recoverable by walking back");
        results.Add("PASS: the bus waits for an intentional bell, supports cancellation and different stops, and requires walking and hand release.");
        foreach (var friend in new[] { true, false })
        {
            var paper = new RepairJourney();
            paper.Tick(0f, 0, 0, true, true, false);
            paper.Tick(0f, 0, 0, false, false, true);
            Require(paper.RepairedCount == 0 && !paper.Ready, "dropping a misaligned fragment is not completion");
            for (var i = 0; i < 3; i++)
            {
                if (i > 0) paper.Tick(0, 0, 1, false, false, false);
                paper.Tick(0, 0, 0, true, true, false);
                var delta = RepairJourney.Target(i) - paper.Pieces[i];
                paper.Tick(Mathf.Abs(delta) / 0.35f, Mathf.Sign(delta), 0, false, true, false);
                paper.Tick(0, 0, 0, false, false, true);
            }
            Require(paper.RepairedCount == 3 && !paper.Ready, "repairing leaves the destination undecided");
            paper.Tick(0, 0, 0, true, true, false);
            paper.Tick(0.3f / 0.35f, friend ? -1 : 1, 0, false, true, false);
            paper.Tick(0, 0, 0, false, false, true);
            Require(paper.Ready && paper.Destination == (friend ? "home" : "notebook"), "the final placement determines where the note goes");
        }
        results.Add("PASS: fragments require alignment; the repaired page can be sent or retained through different placements.");
        var scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => Path.GetFileNameWithoutExtension(x.path)).ToArray();
        Require(scenes.SequenceEqual(new[] { "00_Prologue", "01_Kindergarten", "02_Interlude_Firsts", "03_Stage", "04_Interlude_Growing", "05_Research", "06_FinalWalk" }), "both transitions belong in the shipped scene order");
        foreach (var name in new[] { "02_Interlude_Firsts", "04_Interlude_Growing" })
        {
            var preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
            try { Require(preview.GetRootGameObjects().Any(x => x.GetComponent<InterludeController>() != null), name + " needs its playable controller"); }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        results.Add("PASS: all seven scenes are shipped in order and both interludes retain playable controllers.");
    }

    private static void ValidateScenes(List<string> results)
    {
        var expected = new Dictionary<string, string[]>
        {
            ["01_Kindergarten"] = new[] { "Oversized Chair", "Crying Child", "Teacher Safe Area", "Release Parent At Door" },
            ["03_Stage"] = new[] { "First Breath Cue", "Steady Route", "Forward Route", "Forward Platform", "Final Release Cue" },
            ["05_Research"] = new[] { "Question Board", "Evidence Board", "Conclusion Board", "Photo Evidence", "Data Evidence" },
            ["06_FinalWalk"] = new[] { "Young Presenter", "Unknown Door", "Release The Next Person" }
        };
        foreach (var pair in expected)
        {
            var preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + pair.Key + ".unity");
            try
            {
                var objects = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                foreach (var name in pair.Value) Require(objects.Count(t => t.name == name) == 1, pair.Key + ": unique anchor " + name);
                Require(objects.Any(t => t.GetComponent<HandConnection>() != null), pair.Key + ": hand connection");
                results.Add("PASS: " + pair.Key + " has all required saved-scene anchors.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
    }

    private static HoldTarget Target(string id, HoldTargetMode mode, Vector3 position, string group = "")
    {
        var go = new GameObject(id);
        go.transform.position = position;
        var target = go.AddComponent<HoldTarget>();
        target.Configure(id, mode, "", 2f, 2.5f, companionMaxX: 9999f, group: group);
        Call(target, "OnEnable");
        return target;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static void SetStatic(Type type, string property, object value) =>
        type.GetField("<" + property + ">k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
}
