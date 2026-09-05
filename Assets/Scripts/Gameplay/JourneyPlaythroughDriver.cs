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
    private float started;
    private float heartbeatAt;
    private PlayerController2D Player => StorySceneDirector.Instance.Player.GetComponent<PlayerController2D>();
    private HandConnection Hand => Player.GetComponent<HandConnection>();

    public void Begin()
    {
        Application.runInBackground = true;
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
                (StorySceneDirector.Instance != null ? Player.transform.position + " objectives=" + StorySceneDirector.Instance.CompletedObjectives : "loading"));
        }
        if (!finished && Time.realtimeSinceStartup - started > 160f) Finish("FAIL: timed out during " + phase);
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(1.4f);
        phase = "kindergarten parent release";
        yield return Keys(0.2f, Key.E);
        yield return Walk(-2.7f, true);
        yield return Keys(0.15f);
        yield return Walk(-1.25f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1, "parent gate requires a deliberate release")) yield break;
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
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 3, "toy is given by releasing it beside the child")) yield break;
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

        phase = "stage first breath";
        SceneManager.LoadScene("03_Stage");
        yield return Keys(1.2f);
        yield return Walk(0f);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 1, "first stage phrase requires a full breath and release")) yield break;
        phase = "stage elevated route";
        yield return Walk(3.2f);
        yield return Keys(0.45f, Key.D, Key.Space);
        yield return Keys(0.7f);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 2 && JourneyChoices.StageStyle.Contains("观众"),
            "high route is reachable and supports a complete standing breath")) yield break;
        yield return Walk(10f);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.3f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete, "third phrase completes the performance")) yield break;

        phase = "research reversible draft";
        SceneManager.LoadScene("05_Research");
        yield return Keys(1.2f);
        yield return Walk(-4f);
        yield return Keys(0.25f, Key.E);
        yield return Walk(0.5f, true);
        yield return Keys(0.2f);
        yield return Walk(6f);
        yield return Keys(0.25f, Key.E);
        yield return Walk(12.5f, true);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.CompletedObjectives == 2, "observed evidence is submitted under the lamp")) yield break;
        yield return Walk(9f);
        yield return Keys(0.25f, Key.E);
        yield return Walk(12.5f, true);
        yield return Keys(1.4f, Key.E);
        yield return Keys(0.2f);
        var evidence = GameObject.Find("Evidence Board").GetComponent<HoldSocket>();
        if (!Check(evidence.Occupant.TargetId == "evidence-data" && StorySceneDirector.Instance.CompletedObjectives == 2,
            "the draft can change without adding progress")) yield break;
        yield return Walk(18f);
        yield return Keys(0.25f, Key.E);
        yield return Walk(21.5f, true);
        yield return Keys(0.2f);
        if (!Check(StorySceneDirector.Instance.ObjectivesComplete && evidence.Locked, "conclusion commits the revised draft")) yield break;

        phase = "final farewell";
        SceneManager.LoadScene("06_FinalWalk");
        yield return Keys(1.2f);
        yield return Walk(36f);
        yield return Keys(1.7f, Key.E);
        yield return Keys(0.25f);
        var recipient = EmotionalJourney.Target("young-presenter");
        var initialX = recipient.transform.position.x;
        yield return Keys(0.7f);
        if (!Check(recipient.transform.position.x > initialX, "recipient walks away after release")) yield break;
        yield return Keys(2f);
        if (!Check(Player.ControlsEnabled, "the ending waits for the player's own last step")) yield break;
        yield return Walk(41.4f);
        yield return Keys(0.5f);
        if (!Check(!Player.ControlsEnabled, "walking onward triggers the final fade")) yield break;
        Finish("ALL PLAYTHROUGH CHECKS PASSED using real keyboard events and Unity physics.");
    }

    private IEnumerator Walk(float x, bool hold = false)
    {
        var timeout = Time.time + 18f;
        while (Mathf.Abs(Player.transform.position.x - x) > 0.12f && Time.time < timeout && Player.ControlsEnabled)
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
        yield return new WaitForSeconds(seconds);
    }
    private bool Check(bool condition, string message)
    {
        results.Add((condition ? "PASS: " : "FAIL: ") + message);
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
    private void OnDestroy() { if (keyboard != null) InputSystem.RemoveDevice(keyboard); }
}

#endif
