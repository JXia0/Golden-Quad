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
    private const string AudioRoot = "Assets/Audio/Final";

    [MenuItem("Tools/Let Go/Apply Final Art By Filename")]
    public static void ApplyFinalArt()
    {
        Directory.CreateDirectory(ArtRoot);
        Directory.CreateDirectory(AudioRoot);
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
                    var targetBounds = slot.TargetRenderer.bounds.size;
                    slot.TargetRenderer.sprite = sprite;
                    slot.TargetRenderer.color = Color.white;
                    if (IsBackdrop(slot.SlotId)) FitRendererToBounds(slot.TargetRenderer, targetBounds);
                    else FitRendererInsideBounds(slot.TargetRenderer, targetBounds);
                    EditorUtility.SetDirty(slot.TargetRenderer);
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
            var audio = Object.FindAnyObjectByType<SceneAudio>(FindObjectsInactive.Include);
            if (audio != null)
            {
                audio.SetClips(
                    FindClip("sfx_footstep_soft_01"), FindClip("sfx_footstep_soft_02"),
                    FindClip("sfx_interact"), FindClip("sfx_item_move"), FindClip("sfx_objective_light"),
                    FindClip("sfx_door_open"), FindClip("sfx_hand_release"), FindClip("sfx_heartbeat_loop"),
                    FindClip("sfx_breath_calm"), FindClip("sfx_applause"), FindClip("sfx_paper_rustle"),
                    FindClip("sfx_final_light"), FindClip("amb_indoor_loop"), FindClip("bgm_growth_theme_loop"));
                EditorUtility.SetDirty(audio);
                sceneChanged = true;
            }
            if (sceneChanged) EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LetGo] Applied final art to {changedSlots} scene slots and refreshed scene audio bindings.");
    }

    private static bool IsBackdrop(string slotId)
        => slotId.StartsWith("bg_", System.StringComparison.OrdinalIgnoreCase) ||
           slotId.StartsWith("memory_", System.StringComparison.OrdinalIgnoreCase);

    private static void FitRendererToBounds(SpriteRenderer renderer, Vector3 targetBounds)
    {
        var currentBounds = renderer.bounds.size;
        if (currentBounds.x <= 0.0001f || currentBounds.y <= 0.0001f) return;
        var scale = renderer.transform.localScale;
        scale.x *= targetBounds.x / currentBounds.x;
        scale.y *= targetBounds.y / currentBounds.y;
        renderer.transform.localScale = scale;
        EditorUtility.SetDirty(renderer.transform);
    }

    private static void FitRendererInsideBounds(SpriteRenderer renderer, Vector3 targetBounds)
    {
        var currentBounds = renderer.bounds.size;
        if (currentBounds.x <= 0.0001f || currentBounds.y <= 0.0001f) return;
        var factor = Mathf.Min(targetBounds.x / currentBounds.x, targetBounds.y / currentBounds.y);
        renderer.transform.localScale *= factor;
        EditorUtility.SetDirty(renderer.transform);
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

    private static AudioClip FindClip(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{name} t:AudioClip", new[] { AudioRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(Path.GetFileNameWithoutExtension(path), name, System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }
}
