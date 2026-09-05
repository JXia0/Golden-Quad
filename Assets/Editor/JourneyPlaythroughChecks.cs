using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private const string BuildAfterKey = "LetGo.JourneyPlaytest.BuildAfter";
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
        EditorSceneManager.OpenScene("Assets/Scenes/00_Prologue.unity");
        EditorApplication.isPlaying = true;
    }

    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/00_Prologue.unity");
        JourneyArtImporter.Refresh();
        JourneyRegressionChecks.Run();
        if (!File.ReadAllText("Logs/JourneyRegressionChecks.txt").Contains("ALL CHECKS PASSED"))
        { EditorApplication.Exit(1); return; }
        SessionState.SetBool(BatchKey, true);
        Run();
    }

    public static void ValidateWorkshopBatch()
    {
        SessionState.SetBool("LetGo.QA.WorkshopOnly", true);
        RunBatch();
    }

    public static void ValidateWorkshopAndBuildWindowsBatch()
    {
        SessionState.SetBool(BuildAfterKey, true);
        ValidateWorkshopBatch();
    }

    public static void ValidateTransitionsBatch()
    {
        SessionState.SetBool("LetGo.QA.TransitionsOnly", true);
        RunBatch();
    }

    public static void BuildWindowsBatch()
    {
        JourneyArtImporter.Refresh();
        const string output = "Builds/Windows/LetGo.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/JourneyBuild.txt", report.summary.result + "\n" +
            "Errors: " + report.summary.totalErrors + "\n" + "Bytes: " + report.summary.totalSize + "\n" + Path.GetFullPath(output));
        EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    public static void ValidateAndBuildWindowsBatch()
    {
        SessionState.SetBool(BuildAfterKey, true);
        RunBatch();
    }

    private static void ConfigureBatchView()
    {
        if (!Application.isBatchMode) return;
        // Screen.SetResolution does not resize the editor Game view. Fix only the isolated QA view.
        try
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var assembly = typeof(EditorWindow).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = sizesType.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup", flags).Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var kindType = assembly.GetType("UnityEditor.GameViewSizeType");
            var size = Activator.CreateInstance(sizeType, flags, null, new[] { Enum.Parse(kindType, "FixedResolution"), (object)1280, 720, "Journey QA 720p" }, null);
            group.GetType().GetMethod("AddCustomSize", flags).Invoke(group, new[] { size });
            var count = (int)group.GetType().GetMethod("GetBuiltinCount", flags).Invoke(group, null) +
                (int)group.GetType().GetMethod("GetCustomCount", flags).Invoke(group, null);
            var viewType = assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex", flags).SetValue(view, count - 1);
        }
        catch (Exception error) { Debug.LogWarning("[LetGo QA] Could not fix capture aspect: " + error.Message); }
    }

    private static void OnMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            ConfigureBatchView();
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
                if (SessionState.GetBool(BuildAfterKey, false) && passed)
                {
                    SessionState.SetBool(BuildAfterKey, false);
                    EditorApplication.delayCall += BuildWindowsBatch;
                    return;
                }
                SessionState.SetBool(BuildAfterKey, false);
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }

    [Serializable] private sealed class SavedSetup { public SceneSetup[] scenes; }
}
