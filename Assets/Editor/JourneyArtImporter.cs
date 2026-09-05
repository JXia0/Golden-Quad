using System.Collections.Generic;
using System.IO;
using LetGo;
using UnityEditor;
using UnityEngine;

/// <summary>Build references for new runtime props, without rebuilding or saving any scene.</summary>
public sealed class JourneyArtImporter : AssetPostprocessor
{
    private const string PalettePath = "Assets/Resources/JourneyArtPalette.asset";
    private static readonly string[] Slots = { "prop_child_backpack", "prop_research_model", "prop_research_bridge" };
    private static bool queued;

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
            Sprite sprite = null;
            // Final art takes priority over work-in-progress sprites.
            foreach (var root in new[] { "Assets/Art/Final", "Assets/Sprites" })
            {
                if (!AssetDatabase.IsValidFolder(root)) continue;
                foreach (var guid in AssetDatabase.FindAssets(slot + " t:Sprite", new[] { root }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(path) != slot) continue;
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null) break;
                }
                if (sprite != null) break;
            }
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
}
