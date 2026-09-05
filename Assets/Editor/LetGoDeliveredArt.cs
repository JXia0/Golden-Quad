using System;
using System.IO;
using System.Linq;
using LetGo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Adds delivered art to saved scenes. Never recreates gameplay objects or moves existing art.
public static class LetGoDeliveredArt
{
    private const string Root = "Assets/Sprites/";
    private static Sprite Load(string name) => AssetDatabase.LoadAllAssetsAtPath(name.StartsWith("fx_soft_") || name.StartsWith("view_")
            ? "Assets/Art/Generated/" + name + ".asset" : Root + name.Split(':')[0] + ".png")
        .OfType<Sprite>().FirstOrDefault(s => !name.Contains(':') || s.name == name.Split(':')[1]);

    [MenuItem("Tools/Let Go/Fill Delivered Art (Preserve Layout)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareSprites();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var setting in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var scene = EditorSceneManager.OpenScene(setting.path);
                FillScene(scene.name);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        AssetDatabase.SaveAssets();
        Debug.Log("[LetGo Art] Filled delivered sprites while preserving existing transforms and gameplay colliders.");
    }

    public static void FillScene(string scene)
    {
        Hide("Ground");
        foreach (var slot in Object.FindObjectsByType<ArtSlot>(FindObjectsInactive.Include))
        {
            if (slot.SlotId == "fx_stage_spotlight" && slot.TargetRenderer != null)
                slot.TargetRenderer.sprite = Load("fx_soft_spotlight");
        }
        switch (scene)
        {
            case "01_Kindergarten":
                Reframe("Classroom Hand Gate", "prop_kindergarten_door", new Vector2(0, -0.05f), new Vector2(1.8f, 4.4f));
                Hide("Teacher Safe Area");
                Replace("Oversized Blocks", "deco_kindergarden_bricks:bricks_scattered", true);
                var wall = Find("Classroom Wall");
                Add(wall, "Entrance Backdrop", "view_kindergarten_entry", new Vector2(-22f, 0), new Vector2(10f, 5.5f), -5);
                Add(wall, "Cubby", "prop_cubby", new Vector2(-8.5f, -1.65f), new Vector2(2.3f, 1.6f), -1);
                Add(wall, "Welcome Flags", "deco_kindergarden_flag:flags_00", new Vector2(-10f, 2.3f), new Vector2(5f, 0.85f), -2);
                Add(wall, "Classroom Flags", "deco_kindergarden_flag:flags_01", new Vector2(7f, 2.3f), new Vector2(5f, 0.85f), -2);
                Add(wall, "Children Drawing A", "deco_kindergarden_drawing:drawings_00", new Vector2(-5f, 0.3f), new Vector2(0.65f, 0.7f), -2);
                Add(wall, "Children Drawing B", "deco_kindergarden_drawing:drawings_03", new Vector2(5.5f, 0.4f), new Vector2(0.65f, 0.7f), -2);
                Add(wall, "Wall Sun", "deco_kindergarden_pattern:patterns_00", new Vector2(-3.5f, 1.5f), new Vector2(0.65f, 0.65f), -2);
                Add(wall, "Wall Rainbow", "deco_kindergarden_pattern:patterns_03", new Vector2(9.5f, 1.6f), new Vector2(0.8f, 0.6f), -2);
                break;
            case "03_Stage":
                Replace("Forward Platform", "prop_stage_box", true);
                // Keep the gameplay platform at its original height; a pair of legs visually supports it.
                var platform = Find("Forward Platform");
                Add(platform, "Riser Left Support", "view_riser_legs", new Vector2(-0.75f, -0.7f), new Vector2(1.5f, 1.4f), -1).color = new Color(0.65f, 0.59f, 0.52f);
                Add(platform, "Riser Right Support", "view_riser_legs", new Vector2(0.75f, -0.7f), new Vector2(1.5f, 1.4f), -1).color = new Color(0.65f, 0.59f, 0.52f);
                Reframe("Stage Background", "bg_stage_auditorium", new Vector2(0, 2.25f), new Vector2(34f, 11.33f));
                FloorMark("First Breath Cue", -2.72f);
                FloorMark("Steady Route", -2.72f);
                FloorMark("Forward Route", -1.18f);
                FloorMark("Final Release Cue", -2.72f);
                var stage = Find("Stage Background");
                Add(stage, "Stage Chair", "prop_stage_chair", new Vector2(-2.5f, -1.95f), new Vector2(1f, 1.2f), 0);
                Add(Find("Final Release Cue"), "Stage Microphone", "prop_stage_microphone", new Vector2(0.75f, -0.25f), new Vector2(0.55f, 2f), 0);
                break;
            case "05_Research":
                Reframe("Research Desk", "prop_research_desk", Vector2.zero, new Vector2(6f, 1.4f));
                foreach (var board in new[] { "Question Board", "Evidence Board", "Conclusion Board" })
                {
                    var notes = Add(Find(board), "Pinned Draft", "prop_researchnote", Vector2.zero, new Vector2(1.5f, 1.6f), -1);
                    notes.color = new Color(0.6f, 0.63f, 0.64f);
                    Hide(board);
                }
                var desk = Find("Research Desk");
                Add(desk, "Desk Lamp", "prop_researchlight", new Vector2(-1.4f, 1.05f), new Vector2(0.75f, 0.7f), 1);
                Add(desk, "Reference Books", "prop_research_book", new Vector2(1.8f, 0.925f), new Vector2(0.9f, 0.45f), 1);
                Add(Find("Research Background"), "Research Notes", "prop_researchnote", new Vector2(5f, 1.1f), new Vector2(1.7f, 1.6f), -2);
                Add(Find("Research Background"), "Report Hallway", "view_office_wall", new Vector2(22f, 0), new Vector2(10f, 5.5f), -5);
                Add(Find("Research Background"), "Mentor Hallway", "view_office_wall", new Vector2(-22f, 0), new Vector2(10f, 5.5f), -5);
                break;
            case "06_FinalWalk":
                Hide("Memory Hand Gate");
                FloorMark("Remembered Steady Route", -2.72f);
                FloorMark("Remembered Forward Route", -2.72f);
                Reframe("Kindergarten Memory", "view_memory_kindergarten", Vector2.zero, new Vector2(14f, 5.5f));
                Reframe("Stage Memory", "view_memory_stage", Vector2.zero, new Vector2(14f, 5.5f));
                Reframe("Research Memory", "view_memory_research", Vector2.zero, new Vector2(14f, 5.5f));
                Reframe("Unknown Wall", "view_memory_office", Vector2.zero, new Vector2(14f, 5.5f));
                var memory = Find("Kindergarten Memory");
                string[] images = { "montage01_stayupwithfriends", "montage04_firstraisehand", "montage02_takebus",
                    "montage03_parentstired", "montage_growth_rejected", "montage_growth_conflict" };
                for (var i = 0; i < images.Length; i++)
                    Add(memory, "Memory Picture " + (i + 1), images[i], new Vector2(-3f + i * 6.7f, 3.45f), new Vector2(6.3f, 1.1f), -1);
                break;
        }
        GroundActors();
        var camera = Camera.main;
        var overlay = Add(camera.transform, "Fear Vignette", "fx_soft_vignette", Vector2.zero, Vector2.one, 30000);
        overlay.transform.localPosition = new Vector3(0, 0, 1);
        overlay.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        overlay.color = new Color(1, 1, 1, 0);
        var presentation = camera.GetComponent<SceneArtPresentation>();
        if (presentation == null) presentation = camera.gameObject.AddComponent<SceneArtPresentation>();
        var limits = scene switch {
            "00_Prologue" => new Vector2(-13, 17), "01_Kindergarten" => new Vector2(-18, 26),
            "03_Stage" => new Vector2(-11, 23), "05_Research" => new Vector2(-19, 35),
            _ => new Vector2(-9, 47) };
        presentation.Configure(overlay, limits);
    }

    private static void GroundActors()
    {
        foreach (var slot in Object.FindObjectsByType<ArtSlot>(FindObjectsInactive.Include))
        {
            if (!slot.SlotId.StartsWith("char_") || slot.GetComponent<PlayerController2D>() != null) continue;
            var source = slot.GetComponent<SpriteRenderer>();
            if (source == null || source.sprite == null || slot.transform.Find("Grounded Visual") != null) continue;
            var go = new GameObject("Grounded Visual", typeof(SpriteRenderer));
            var visible = go.GetComponent<SpriteRenderer>();
            visible.sprite = source.sprite;
            visible.sharedMaterial = source.sharedMaterial;
            visible.color = source.color;
            visible.sortingOrder = source.sortingOrder;
            go.transform.position = source.transform.position;
            go.transform.localScale = source.transform.lossyScale;
            go.transform.position += Vector3.up * (-2.72f - visible.bounds.min.y);
            go.transform.SetParent(source.transform, true);
            source.enabled = false;
            slot.Configure(slot.SlotId, visible, slot.TargetAnimator);
        }
    }

    private static Transform Find(string name) => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
        FindObjectsSortMode.None).FirstOrDefault(t => t.name == name);

    private static void FloorMark(string name, float groundY)
    {
        var anchor = Find(name);
        var mark = Add(anchor, "Floor Light", "fx_soft_pool", new Vector2(0, groundY - anchor.position.y), new Vector2(1.35f, 0.2f), 1);
        mark.color = new Color(1f, 0.78f, 0.44f, 0.35f);
        anchor.GetComponent<SpriteRenderer>().enabled = false;
        anchor.GetComponent<ArtSlot>().Configure("fx_soft_pool", mark);
    }

    private static void Hide(string name)
    {
        var renderer = Find(name)?.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    private static void Reframe(string name, string asset, Vector2 offset, Vector2 size)
    {
        var anchor = Find(name);
        if (anchor == null) throw new InvalidOperationException("Missing art anchor " + name);
        var original = anchor.GetComponent<SpriteRenderer>();
        var child = Add(anchor, "Aligned Visual", asset, offset, size, original.sortingOrder);
        original.enabled = false;
        var slot = anchor.GetComponent<ArtSlot>();
        if (slot != null) slot.Configure(asset, child);
    }

    private static SpriteRenderer Add(Transform anchor, string name, string asset, Vector2 offset, Vector2 size, int order)
    {
        if (anchor == null) throw new InvalidOperationException("Missing anchor for " + name);
        var existing = anchor.Find(name);
        if (existing != null) return existing.GetComponent<SpriteRenderer>();
        var sprite = Load(asset);
        if (sprite == null) throw new InvalidOperationException("Missing delivered sprite " + asset);
        var go = new GameObject(name, typeof(SpriteRenderer), typeof(ArtSlot));
        go.transform.position = anchor.position + (Vector3)offset;
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        var anchorRenderer = anchor.GetComponent<SpriteRenderer>();
        renderer.sharedMaterial = anchorRenderer != null ? anchorRenderer.sharedMaterial
            : AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        renderer.sortingOrder = order;
        var factor = Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
        go.transform.localScale = new Vector3(factor, factor, 1f);
        go.transform.SetParent(anchor, true);
        go.GetComponent<ArtSlot>().Configure(asset, renderer);
        return renderer;
    }

    private static void Replace(string name, string asset, bool stretch = false)
    {
        var anchor = Find(name);
        if (anchor == null) throw new InvalidOperationException("Missing replacement target " + name);
        var original = anchor.GetComponent<SpriteRenderer>();
        if (anchor.Find("Delivered Visual") != null) return;
        var bounds = original.bounds;
        var renderer = Add(anchor, "Delivered Visual", asset, bounds.center - anchor.position, bounds.size, original.sortingOrder);
        if (stretch)
        {
            var scale = renderer.transform.localScale;
            scale.x *= bounds.size.x / renderer.bounds.size.x;
            scale.y *= bounds.size.y / renderer.bounds.size.y;
            renderer.transform.localScale = scale;
        }
        // The gameplay collider stays on the original transform, with exactly the same size.
        original.enabled = false;
        var slot = anchor.GetComponent<ArtSlot>();
        if (slot != null)
        {
            slot.Configure(asset, renderer);
            Object.DestroyImmediate(renderer.GetComponent<ArtSlot>());
        }
    }

    public static void PrepareSprites()
    {
        MakeSoftSprite("fx_soft_spotlight", true);
        MakeSoftSprite("fx_soft_pool", false);
        MakeSoftSprite("fx_soft_vignette", false);
        var vignetteImporter = (TextureImporter)AssetImporter.GetAtPath(Root + "fx_fear_vignette.png");
        vignetteImporter.GetSourceTextureWidthAndHeight(out var vignetteWidth, out var vignetteHeight);
        Crop("fx_fear_vignette", new Rect(0, 0, vignetteWidth, vignetteHeight));
        Slice("deco_kindergarden_bricks", new[] { "bricks_tower", "bricks_arch", "bricks_scattered", "bricks_house" },
            new[] { new Rect(210, 35, 350, 495), new Rect(750, 70, 600, 445), new Rect(120, 575, 640, 375), new Rect(820, 515, 475, 430) });
        Grid("deco_kindergarden_drawing", "drawings", 4, 2);
        Grid("deco_kindergarden_flag", "flags", 1, 3);
        Grid("deco_kindergarden_pattern", "patterns", 4, 3);
        Crop("prop_child_backpack", new Rect(140, 147, 889, 917));
        Crop("prop_cubby", new Rect(111, 140, 1180, 841));
        Crop("prop_stage_box", new Rect(115, 487, 1310, 326));
        Crop("prop_stage_chair", new Rect(131, 69, 839, 1342));
        Crop("prop_stage_microphone", new Rect(329, 42, 367, 1447));
        Crop("prop_research_book", new Rect(72, 208, 1375, 698));
        Crop("prop_researchlight", new Rect(273, 60, 970, 897));
        Crop("prop_researchnote", new Rect(56, 54, 1150, 1103));
        foreach (var path in Directory.GetFiles(Root, "montage*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            Crop(Path.GetFileNameWithoutExtension(path), new Rect(0, 0, width, height));
        }
        MakeView("view_riser_legs", "prop_stage_chair", new Rect(0, 0, 1, 0.5f));
        MakePanel("view_memory_kindergarten", "幼儿园背景图", 14f / 5.5f);
        MakePanel("view_memory_stage", "bg_stage_auditorium", 14f / 5.5f);
        MakePanel("view_memory_research", "bg_research_room", 14f / 5.5f);
        MakePanel("view_memory_office", "bg_office_hallway", 14f / 5.5f);
        MakePanel("view_office_wall", "bg_office_hallway", 10f / 5.5f);
        MakeView("view_kindergarten_entry", "bg_kindergarten_hall", new Rect(0, 0, 10f / 34f, 1));
    }

    private static void MakePanel(string name, string source, float aspect)
    {
        var rect = Load(source).rect;
        var width = Mathf.Min(1, aspect / (rect.width / rect.height));
        var height = Mathf.Min(1, (rect.width / rect.height) / aspect);
        MakeView(name, source, new Rect((1 - width) * 0.5f, (1 - height) * 0.5f, width, height));
    }

    private static void MakeView(string name, string source, Rect portion)
    {
        var path = "Assets/Art/Generated/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null) return;
        var original = Load(source);
        var rect = original.rect;
        var sprite = Sprite.Create(original.texture, new Rect(rect.x + portion.x * rect.width,
            rect.y + portion.y * rect.height, rect.width * portion.width, rect.height * portion.height),
            Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        AssetDatabase.CreateAsset(sprite, path);
    }

    // Small, baked textures: no per-frame texture generation or extra lighting simulation.
    private static void MakeSoftSprite(string name, bool cone)
    {
        var path = "Assets/Art/Generated/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) return;
        const int width = 128;
        var vignette = name == "fx_soft_vignette";
        var height = cone || vignette ? 128 : 32;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var u = (x + 0.5f) / width * 2 - 1;
            var v = (y + 0.5f) / height;
            float alpha;
            if (vignette)
                alpha = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.6f, 1.4f,
                    Mathf.Sqrt(u * u + Mathf.Pow(v * 2 - 1, 2))));
            else if (cone)
            {
                var halfWidth = Mathf.Lerp(0.96f, 0.06f, v);
                alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((halfWidth - Mathf.Abs(u)) / 0.15f));
                alpha *= Mathf.SmoothStep(0, 1, Mathf.Clamp01(v / 0.15f)) * Mathf.SmoothStep(0, 1, Mathf.Clamp01((1 - v) / 0.08f));
            }
            else
                alpha = Mathf.Pow(Mathf.Clamp01(1 - u * u - Mathf.Pow(v * 2 - 1, 2)), 2);
            texture.SetPixel(x, y, vignette ? new Color(0.01f, 0.01f, 0.08f, alpha) : new Color(1, 1, 1, alpha));
        }
        texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f,
            width, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssets();
    }

    private static void Crop(string file, Rect rect) => Slice(file, new[] { file }, new[] { rect });

    private static void Grid(string file, string prefix, int columns, int rows)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Root + file + ".png");
        importer.GetSourceTextureWidthAndHeight(out var width, out var height);
        var rects = new Rect[columns * rows];
        var names = new string[rects.Length];
        for (var i = 0; i < rects.Length; i++)
        {
            rects[i] = new Rect(i % columns * width / (float)columns, i / columns * height / (float)rows,
                width / (float)columns, height / (float)rows);
            names[i] = prefix + "_" + i.ToString("00");
        }
        Slice(file, names, rects);
    }

    // Input rectangles use top-left image coordinates; SpriteRect uses bottom-left coordinates.
    private static void Slice(string file, string[] names, Rect[] regions)
    {
        var path = Root + file + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new FileNotFoundException(path);
        importer.GetSourceTextureWidthAndHeight(out _, out var height);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var current = provider.GetSpriteRects();
        if (current.Length == names.Length && current.Select(r => r.name).SequenceEqual(names)) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        var rects = new SpriteRect[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var region = regions[i];
            rects[i] = new SpriteRect { name = names[i], spriteID = GUID.Generate(),
                rect = new Rect(region.x, height - region.yMax, region.width, region.height),
                alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f) };
        }
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(
            rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }
}
