using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class LetGoSpriteAssetIntegrator
{
    public const string GeneratedRoot = "Assets/Art/Generated";
    private const string IdleSheet = "Assets/Sprites/char_child/char_child_Idle 4.png";
    private const string WalkSheet = "Assets/Sprites/char_child/char_child_Walk 6.png";
    private const string HandSheet = "Assets/Sprites/char_child/char_child_牵手放手.png";
    private const string ControllerPath = GeneratedRoot + "/char_child_controller.controller";
    private const string TeenIdleSheet = "Assets/Sprites/char_teen/char_teen_Idle 4.png";
    private const string TeenWalkSheet = "Assets/Sprites/char_teen/char_teen_Walk 6.png";
    private const string TeenBreathSheet = "Assets/Sprites/char_teen/char_teen_手按胸口呼吸 4.png";
    private const string TeenControllerPath = GeneratedRoot + "/char_teen_controller.controller";

    public static void BuildAvailableControllers()
    {
        BuildChildController();
        BuildTeenController();
    }

    public static AnimatorController BuildChildController()
    {
        if (!File.Exists(IdleSheet) || !File.Exists(WalkSheet)) return null;
        Directory.CreateDirectory(GeneratedRoot);
        EnsureGridSlices(IdleSheet, "char_child_idle", 4);
        EnsureGridSlices(WalkSheet, "char_child_walk", 6);
        var hasHandAnimation = File.Exists(HandSheet);
        if (hasHandAnimation) EnsureGridSlices(HandSheet, "char_child_hand", 4);

        var idleSprites = LoadSprites(IdleSheet);
        var walkSprites = LoadSprites(WalkSheet);
        var handSprites = hasHandAnimation ? LoadSprites(HandSheet) : new Sprite[0];
        if (idleSprites.Length != 4 || walkSprites.Length != 6) return null;

        var existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existingController != null &&
            AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_child_idle.anim") != null &&
            AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_child_walk.anim") != null &&
            AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_child_hold_parent.anim") != null &&
            AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_child_release_parent.anim") != null)
            return existingController;

        DeleteGeneratedAsset("char_child_idle.anim");
        DeleteGeneratedAsset("char_child_walk.anim");
        DeleteGeneratedAsset("char_child_hold_parent.anim");
        DeleteGeneratedAsset("char_child_release_parent.anim");
        AssetDatabase.DeleteAsset(ControllerPath);

        var idleClip = CreateClip("char_child_idle.anim", idleSprites, 4f, true);
        var walkClip = CreateClip("char_child_walk.anim", walkSprites, 8f, true);
        var holdClip = handSprites.Length == 4
            ? CreateClip("char_child_hold_parent.anim", new[] { handSprites[0] }, 1f, true)
            : idleClip;
        var releaseClip = handSprites.Length == 4
            ? CreateClip("char_child_release_parent.anim", handSprites, 7f, false)
            : idleClip;

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("HoldingParent", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Idle");
        var walk = machine.AddState("Walk");
        var hold = machine.AddState("Hold Parent");
        var release = machine.AddState("Release Parent");
        idle.motion = idleClip;
        walk.motion = walkClip;
        hold.motion = holdClip;
        release.motion = releaseClip;
        machine.defaultState = idle;

        AddCondition(idle, walk, AnimatorConditionMode.Greater, 0.05f, "Speed");
        AddCondition(walk, idle, AnimatorConditionMode.Less, 0.05f, "Speed");
        AddCondition(idle, hold, AnimatorConditionMode.If, 0f, "HoldingParent");
        AddCondition(walk, hold, AnimatorConditionMode.If, 0f, "HoldingParent");
        AddCondition(hold, release, AnimatorConditionMode.IfNot, 0f, "HoldingParent");
        var releaseToIdle = release.AddTransition(idle);
        releaseToIdle.hasExitTime = true;
        releaseToIdle.exitTime = 1f;
        releaseToIdle.duration = 0.05f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    public static AnimatorController BuildTeenController()
    {
        if (!File.Exists(TeenIdleSheet) || !File.Exists(TeenWalkSheet) || !File.Exists(TeenBreathSheet)) return null;
        Directory.CreateDirectory(GeneratedRoot);
        EnsureGridSlices(TeenIdleSheet, "char_teen_idle", 4);
        EnsureGridSlices(TeenWalkSheet, "char_teen_walk", 6);
        EnsureGridSlices(TeenBreathSheet, "char_teen_breath", 4);

        var idleSprites = LoadSprites(TeenIdleSheet);
        var walkSprites = LoadSprites(TeenWalkSheet);
        var breathSprites = LoadSprites(TeenBreathSheet);
        if (idleSprites.Length != 4 || walkSprites.Length != 6 || breathSprites.Length != 4) return null;

        var existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(TeenControllerPath);
        if (existingController != null &&
            AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_teen_idle.anim") != null &&
            AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_teen_walk.anim") != null &&
            AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_teen_breath.anim") != null)
            return existingController;

        DeleteGeneratedAsset("char_teen_idle.anim");
        DeleteGeneratedAsset("char_teen_walk.anim");
        DeleteGeneratedAsset("char_teen_breath.anim");
        AssetDatabase.DeleteAsset(TeenControllerPath);

        var idleClip = CreateClip("char_teen_idle.anim", idleSprites, 4f, true);
        var walkClip = CreateClip("char_teen_walk.anim", walkSprites, 8f, true);
        var breathClip = CreateClip("char_teen_breath.anim", breathSprites, 4f, true);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(TeenControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("SelfAnchoring", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Idle");
        var walk = machine.AddState("Walk");
        var breath = machine.AddState("Hand On Chest");
        idle.motion = idleClip;
        walk.motion = walkClip;
        breath.motion = breathClip;
        machine.defaultState = idle;

        AddCondition(idle, walk, AnimatorConditionMode.Greater, 0.05f, "Speed");
        AddCondition(walk, idle, AnimatorConditionMode.Less, 0.05f, "Speed");
        AddCondition(idle, breath, AnimatorConditionMode.If, 0f, "SelfAnchoring");
        AddCondition(walk, breath, AnimatorConditionMode.If, 0f, "SelfAnchoring");
        AddCondition(breath, idle, AnimatorConditionMode.IfNot, 0f, "SelfAnchoring");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void EnsureGridSlices(string path, string prefix, int frameCount)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (importer == null || texture == null) return;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var current = provider.GetSpriteRects();
        if (current.Length == frameCount && current.All(rect => rect.name.StartsWith(prefix))) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        var rects = new SpriteRect[frameCount];
        var pairs = new SpriteNameFileIdPair[frameCount];
        for (var i = 0; i < frameCount; i++)
        {
            var xMin = Mathf.RoundToInt(texture.width * i / (float)frameCount);
            var xMax = Mathf.RoundToInt(texture.width * (i + 1) / (float)frameCount);
            var id = GUID.Generate();
            var name = $"{prefix}_{i:00}";
            rects[i] = new SpriteRect
            {
                name = name,
                rect = new Rect(xMin, 0f, xMax - xMin, texture.height),
                alignment = SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f),
                spriteID = id
            };
            pairs[i] = new SpriteNameFileIdPair(name, id);
        }

        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names?.SetNameFileIdPairs(pairs);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadSprites(string path)
        => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();

    private static AnimationClip CreateClip(string fileName, Sprite[] sprites, float frameRate, bool loop)
    {
        var clip = new AnimationClip { frameRate = frameRate };
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (var i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprites[i] };
        keys[^1] = new ObjectReferenceKeyframe
        {
            time = sprites.Length / frameRate,
            value = loop ? sprites[0] : sprites[^1]
        };
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        var path = GeneratedRoot + "/" + fileName;
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void AddCondition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode,
        float threshold, string parameter)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void DeleteGeneratedAsset(string fileName)
        => AssetDatabase.DeleteAsset(GeneratedRoot + "/" + fileName);
}
