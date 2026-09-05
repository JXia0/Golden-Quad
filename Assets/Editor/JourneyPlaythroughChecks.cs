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

/// <summary>Drives the real keyboard/physics path in Play Mode and restores the open editor scene.</summary>
[InitializeOnLoad]
public static class JourneyPlaythroughChecks
{
    private const string ActiveKey = "LetGo.JourneyPlaytest.Active";
    private const string SceneKey = "LetGo.JourneyPlaytest.Scene";
    private const string BatchKey = "LetGo.JourneyPlaytest.Batch";
    static JourneyPlaythroughChecks()
    {
        EditorApplication.update += CheckRequest;
        EditorApplication.playModeStateChanged += OnMode;
    }

    private static void CheckRequest()
    {
        if (SessionState.GetBool(ActiveKey, false) && EditorApplication.isPlaying)
        {
            EditorApplication.isPaused = false;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        if (!File.Exists("Logs/run-journey-playthrough.request") || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Logs/run-journey-playthrough.request");
        Run();
    }

    [MenuItem("Tools/Let Go/Run Emotional Journey Playthrough Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Never overwrite or unload an unsaved art edit to run a test.
        for (var i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
            { Debug.LogWarning("[LetGo] Save scene edits before running playthrough checks."); return; }
        SessionState.SetString(SceneKey, JsonUtility.ToJson(new SavedSetup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        SessionState.SetBool(ActiveKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/01_Kindergarten.unity");
        EditorApplication.isPlaying = true;
    }

    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/00_Prologue.unity");
        JourneyRegressionChecks.Run();
        if (!File.ReadAllText("Logs/JourneyRegressionChecks.txt").Contains("ALL CHECKS PASSED"))
        { EditorApplication.Exit(1); return; }
        SessionState.SetBool(BatchKey, true);
        Run();
    }

    private static void OnMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            var go = new GameObject("Journey automated playthrough");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<JourneyPlaythroughDriver>().Begin();
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(ActiveKey, false);
            var setup = JsonUtility.FromJson<SavedSetup>(SessionState.GetString(SceneKey, ""));
            if (setup?.scenes != null) EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);
            if (SessionState.GetBool(BatchKey, false))
            {
                SessionState.SetBool(BatchKey, false);
                var passed = File.Exists("Logs/JourneyPlaythroughChecks.txt") && File.ReadAllText("Logs/JourneyPlaythroughChecks.txt").Contains("ALL PLAYTHROUGH CHECKS PASSED");
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }

    [Serializable] private sealed class SavedSetup { public SceneSetup[] scenes; }
}
