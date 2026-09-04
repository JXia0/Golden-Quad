using System.IO;
using System.Linq;
using LetGo;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LetGoArtBinder
{
    private const string ArtRoot = "Assets/Art/Final";

    [MenuItem("Tools/Let Go/Apply Final Art By Filename")]
    public static void ApplyFinalArt()
    {
        Directory.CreateDirectory(ArtRoot);
        AssetDatabase.Refresh();
        var previousScenes = EditorSceneManager.GetSceneManagerSetup();
        var changedSlots = 0;

        foreach (var sceneSetting in EditorBuildSettings.scenes.Where(scene => scene.enabled))
        {
            var scene = EditorSceneManager.OpenScene(sceneSetting.path, OpenSceneMode.Single);
            var sceneChanged = false;
            foreach (var slot in Object.FindObjectsByType<ArtSlot>(FindObjectsInactive.Include))
            {
                var sprite = FindSprite(slot.SlotId);
                if (sprite != null && slot.TargetRenderer != null)
                {
                    slot.TargetRenderer.sprite = sprite;
                    slot.TargetRenderer.color = Color.white;
                    sceneChanged = true;
                    changedSlots++;
                }

                var controller = FindController(slot.SlotId + "_controller");
                if (controller != null && slot.TargetAnimator != null)
                {
                    slot.TargetAnimator.runtimeAnimatorController = controller;
                    sceneChanged = true;
                }
            }

            foreach (var transition in Object.FindObjectsByType<FinalAgeTransition>(FindObjectsInactive.Include))
            {
                var sprite = FindSprite(transition.ArtSlotId);
                if (sprite != null)
                {
                    transition.SetTransitionSprite(sprite);
                    sceneChanged = true;
                }
                var controller = FindController(transition.ArtSlotId + "_controller");
                if (controller != null)
                {
                    transition.SetTransitionController(controller);
                    sceneChanged = true;
                }
                if (sprite != null || controller != null) EditorUtility.SetDirty(transition);
            }
            if (sceneChanged) EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LetGo] Applied final art to {changedSlots} scene slots.");
    }

    private static Sprite FindSprite(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId)) return null;
        foreach (var guid in AssetDatabase.FindAssets($"{slotId} t:Sprite", new[] { ArtRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(Path.GetFileNameWithoutExtension(path), slotId, System.StringComparison.OrdinalIgnoreCase)) continue;
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
        return null;
    }

    private static AnimatorController FindController(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{name} t:AnimatorController", new[] { ArtRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(Path.GetFileNameWithoutExtension(path), name, System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        return null;
    }
}
