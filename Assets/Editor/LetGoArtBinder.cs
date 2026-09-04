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
    private const string SpriteRoot = "Assets/Sprites";
    private const string AudioRoot = "Assets/Audio/Final";

    [MenuItem("Tools/Let Go/Apply Final Art By Filename")]
    public static void ApplyFinalArt()
    {
        Directory.CreateDirectory(ArtRoot);
        Directory.CreateDirectory(AudioRoot);
        AssetDatabase.Refresh();
        LetGoSpriteAssetIntegrator.BuildChildController();
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
                    var targetBounds = GetTargetBounds(slot, slot.TargetRenderer);
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

    private static Vector3 GetTargetBounds(ArtSlot slot, SpriteRenderer renderer)
    {
        if (slot.SlotId.StartsWith("char_", System.StringComparison.OrdinalIgnoreCase) &&
            renderer.TryGetComponent<CapsuleCollider2D>(out var capsule))
            return capsule.bounds.size;
        return renderer.bounds.size;
    }

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
        var exact = FindSpriteFile(slotId, new[] { ArtRoot, SpriteRoot });
        if (exact != null) return exact;
        if (slotId == "char_child")
            return FindLargestSpriteAtPath("Assets/Sprites/char_child/char_child_Idle 4.png");
        var fallback = slotId switch
        {
            "memory_kindergarten_set" => "幼儿园背景图",
            "memory_stage_set" => "bg_stage_auditorium",
            "memory_research_set" => "bg_research_room",
            "prop_unknown_door" => "prop_meeting_door",
            _ => string.Empty
        };
        return string.IsNullOrEmpty(fallback) ? null : FindSpriteFile(fallback, new[] { SpriteRoot });
    }

    private static Sprite FindSpriteFile(string fileName, string[] roots)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{fileName} t:Sprite", roots))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(Path.GetFileNameWithoutExtension(path), fileName, System.StringComparison.OrdinalIgnoreCase)) continue;
            return FindLargestSpriteAtPath(path);
        }
        return null;
    }

    private static Sprite FindLargestSpriteAtPath(string path)
        => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height).FirstOrDefault();

    private static AnimatorController FindController(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{name} t:AnimatorController",
                     new[] { ArtRoot, LetGoSpriteAssetIntegrator.GeneratedRoot }))
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
