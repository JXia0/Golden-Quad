using System.Collections.Generic;
using System.IO;
using LetGo;
using UnityEditor;
using UnityEngine;

/// <summary>Build references for new runtime props, without rebuilding or saving any scene.</summary>
public sealed class JourneyArtImporter : AssetPostprocessor
{
    private const string PalettePath = "Assets/Resources/JourneyArtPalette.asset";
    private static readonly string[] Slots =
    {
        "prop_child_backpack", "prop_research_model", "prop_research_crate", "prop_research_bridge", "prop_toy", "prop_researchlight", "prop_stage_box", "prop_research_report", "prop_research_book",
        "workshop-platform", "workshop-notebook", "workshop-plate", "workshop-shutter", "workshop-shutter_frame", "workshop-shutter_door",
        "workshop-shutter_locked", "workshop-shutter_unlocked", "workshop-latch",
        "workshop-window", "workshop-report", "workshop-desk"
        , "prop_researchnote", "montage01_stayupwithfriends", "montage02_takebus", "montage03_parentstired",
        "montage04_firstraisehand", "montage_growth_rejected", "montage_growth_conflict", "bg_research_room"
    };
    private static readonly Dictionary<string, string> Fallbacks = new()
    {
        { "prop_research_model", "char_child_Idle 4" },
        { "prop_research_bridge", "prop_stage_box" },
        { "workshop-platform", "prop_stage_box" },
        { "workshop-notebook", "prop_research_book" },
        { "workshop-plate", "prop_stage_box" },
        { "workshop-shutter", "workshop-shutter_door" },
        { "workshop-latch", "workshop-shutter_locked" },
        { "workshop-window", "prop_office_doorsigns" },
        { "workshop-report", "prop_research_report" },
        { "workshop-desk", "prop_research_desk" }
    };
    private static bool queued;

    private void OnPreprocessTexture()
    {
        var file = Path.GetFileNameWithoutExtension(assetPath);
        if (file != "prop_research_crate" && !file.StartsWith("workshop-")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        Queue();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) Queue();
        };
    }

    private static void Queue() { if (queued) return; queued = true; EditorApplication.delayCall += Refresh; }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var paths in new[] { imported, deleted, moved, movedFrom })
            foreach (var path in paths)
                if (path.StartsWith("Assets/Sprites/") || path.StartsWith("Assets/Art/Final/")) { Queue(); return; }
    }

    [MenuItem("Tools/Let Go/Refresh Emotional Journey Art")]
    public static void Refresh()
    {
        queued = false;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var entries = new List<JourneyArtPalette.Entry>();
        foreach (var slot in Slots)
        {
            var sprite = FindSprite(slot);
            if (sprite == null && Fallbacks.TryGetValue(slot, out var fallback)) sprite = FindSprite(fallback);
            entries.Add(new JourneyArtPalette.Entry { id = slot, sprite = sprite });
        }
        var palette = AssetDatabase.LoadAssetAtPath<JourneyArtPalette>(PalettePath);
        if (palette != null && palette.entries != null && palette.entries.Length == entries.Count)
        {
            var equal = true;
            for (var i = 0; i < entries.Count; i++)
                equal &= palette.entries[i].id == entries[i].id && palette.entries[i].sprite == entries[i].sprite;
            if (equal) return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (palette == null)
        {
            palette = ScriptableObject.CreateInstance<JourneyArtPalette>();
            AssetDatabase.CreateAsset(palette, PalettePath);
        }
        palette.entries = entries.ToArray();
        EditorUtility.SetDirty(palette);
        AssetDatabase.SaveAssetIfDirty(palette);
    }

    private static Sprite FindSprite(string assetName)
    {
        // Final art takes priority over work-in-progress sprites.
        foreach (var root in new[] { "Assets/Art/Final", "Assets/Sprites" })
        {
            if (!AssetDatabase.IsValidFolder(root)) continue;
            foreach (var guid in AssetDatabase.FindAssets(assetName + " t:Sprite", new[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != assetName) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite sprite) return sprite;
            }
            foreach (var file in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
            {
                if (Path.GetFileNameWithoutExtension(file) != assetName) continue;
                var assetPath = file.Replace('\\', '/');
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                    if (asset is Sprite sprite) return sprite;
            }
        }
        return null;
    }
}
