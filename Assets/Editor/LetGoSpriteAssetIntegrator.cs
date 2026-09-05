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
    private const string TeenStageSheet = "Assets/Sprites/char_teen/char_teen_舞台动作 3.png";
    private const string TeenControllerPath = GeneratedRoot + "/char_teen_controller.controller";
    private const string AdultIdleSheet = "Assets/Sprites/char_adult/char_adult_Idle 4.png";
    private const string AdultWalkSheet = "Assets/Sprites/char_adult/char_adult_Walk 6.png";
    private const string AdultControllerPath = GeneratedRoot + "/char_adult_controller.controller";

    public static void BuildAvailableControllers()
    {
        PrepareSupplementalSprites();
        BuildChildController();
        BuildTeenController();
        BuildAdultController();
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
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_child_idle.anim")) &&
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_child_walk.anim")) &&
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_child_hold_parent.anim")) &&
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_child_release_parent.anim")))
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
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_teen_idle.anim")) &&
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_teen_walk.anim")) &&
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_teen_breath.anim")))
        {
            EnsureStageGesture(existingController);
            return existingController;
        }

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
        EnsureStageGesture(controller);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    [InitializeOnLoadMethod]
    private static void QueueStageGestureUpgrade() => EditorApplication.delayCall += () =>
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) BuildTeenController();
    };

    private static void EnsureStageGesture(AnimatorController controller)
    {
        if (!File.Exists(TeenStageSheet)) return;
        EnsureGridSlices(TeenStageSheet, "char_teen_stage", 3);
        var machine = controller.layers[0].stateMachine;
        if (machine.states.Any(item => item.state.name == "Stage Gesture" && item.state.motion != null) &&
            controller.parameters.Any(item => item.name == "Perform")) return;
        var sprites = LoadSprites(TeenStageSheet);
        if (sprites.Length != 3) return;
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_teen_stage.anim");
        if (clip == null) clip = CreateClip("char_teen_stage.anim", sprites, 3f, false);
        if (!controller.parameters.Any(item => item.name == "Perform")) controller.AddParameter("Perform", AnimatorControllerParameterType.Trigger);
        var gesture = machine.states.Select(item => item.state).FirstOrDefault(item => item.name == "Stage Gesture") ?? machine.AddState("Stage Gesture");
        gesture.motion = clip;
        var enter = machine.AddAnyStateTransition(gesture);
        enter.hasExitTime = false;
        enter.duration = 0f;
        enter.canTransitionToSelf = true;
        enter.AddCondition(AnimatorConditionMode.If, 0f, "Perform");
        var leave = gesture.AddTransition(machine.defaultState);
        leave.hasExitTime = true;
        leave.exitTime = 1f;
        leave.duration = 0f;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
    }

    public static AnimatorController BuildAdultController()
    {
        if (!File.Exists(AdultIdleSheet) || !File.Exists(AdultWalkSheet)) return null;
        Directory.CreateDirectory(GeneratedRoot);
        EnsureGridSlices(AdultIdleSheet, "char_adult_idle", 4);
        EnsureGridSlices(AdultWalkSheet, "char_adult_walk", 6);

        var idleSprites = LoadSprites(AdultIdleSheet);
        var walkSprites = LoadSprites(AdultWalkSheet);
        if (idleSprites.Length != 4 || walkSprites.Length != 6) return null;

        var existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AdultControllerPath);
        if (existingController != null &&
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_adult_idle.anim")) &&
            UsesCharacterVisual(AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedRoot + "/char_adult_walk.anim")))
            return existingController;

        DeleteGeneratedAsset("char_adult_idle.anim");
        DeleteGeneratedAsset("char_adult_walk.anim");
        AssetDatabase.DeleteAsset(AdultControllerPath);

        var idleClip = CreateClip("char_adult_idle.anim", idleSprites, 4f, true);
        var walkClip = CreateClip("char_adult_walk.anim", walkSprites, 8f, true);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(AdultControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Idle");
        var walk = machine.AddState("Walk");
        idle.motion = idleClip;
        walk.motion = walkClip;
        machine.defaultState = idle;
        AddCondition(idle, walk, AnimatorConditionMode.Greater, 0.05f, "Speed");
        AddCondition(walk, idle, AnimatorConditionMode.Less, 0.05f, "Speed");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void PrepareSupplementalSprites()
    {
        EnsureGridSlices("Assets/Sprites/char_child/char_child_递玩具 3.png", "char_child_give_toy", 3);
        EnsureGridSlices("Assets/Sprites/char_teen/char_teen_舞台动作 3.png", "char_teen_stage", 3);
        EnsureGridSlices("Assets/Sprites/char_adult/char_adult_伸手 3.png", "char_adult_reach", 3);
        EnsureGridSlices("Assets/Sprites/char_adult/char_adult_按胸口 3.png", "char_adult_chest", 3);
        EnsureGridSlices("Assets/Sprites/char_adult/char_adult_推门 4.png", "char_adult_push_door", 4);
        EnsureGridSlices("Assets/Sprites/char_crying_child/char_crying_child_哭泣 4.png", "char_crying_child_cry", 4);
        EnsureGridSlices("Assets/Sprites/char_crying_child/char_crying_child_接过玩具 3.png", "char_crying_child_receive", 3);
        EnsureGridSlices("Assets/Sprites/char_parent/char_parent_Idle 4.png", "char_parent_idle", 4);

        EnsureSingleSprite("Assets/Sprites/char_adult/char_adult_持报告 1.png", SpriteAlignment.BottomCenter);
        EnsureSingleSprite("Assets/Sprites/char_parent/char_parent_手放肩膀 1.png", SpriteAlignment.BottomCenter);
        EnsureSingleSprite("Assets/Sprites/char_parent/char_parent_牵手 1.png", SpriteAlignment.BottomCenter);
        EnsureSingleSprite("Assets/Sprites/char_parent/char_parent_门口等待 1.png", SpriteAlignment.BottomCenter);
        EnsureSingleSprite("Assets/Sprites/char_teacher/char_teacher 侧面站立 1.png", SpriteAlignment.BottomCenter);
        EnsureSingleSprite("Assets/Sprites/char_mentor/char_mentor  侧面站立 1.png", SpriteAlignment.BottomCenter);
        EnsureSingleSprite("Assets/Sprites/audience_eyes.png", SpriteAlignment.Center);
        EnsureSingleSprite("Assets/Sprites/fx_shadow_blob.png", SpriteAlignment.Center);
    }

    private static void EnsureGridSlices(string path, string prefix, int frameCount)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.GetSourceTextureWidthAndHeight(out var sourceWidth, out var sourceHeight);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var current = provider.GetSpriteRects();
        var preserveIds = current.Length == frameCount && current.All(rect => rect.name.StartsWith(prefix));
        if (preserveIds) current = current.OrderBy(rect => rect.name).ToArray();
        var correct = preserveIds && importer.maxTextureSize >= 4096 &&
            importer.textureCompression == TextureImporterCompression.Uncompressed;
        for (var i = 0; correct && i < frameCount; i++)
        {
            var min = Mathf.RoundToInt(sourceWidth * i / (float)frameCount);
            var max = Mathf.RoundToInt(sourceWidth * (i + 1) / (float)frameCount);
            correct &= current[i].rect == new Rect(min, 0f, max - min, sourceHeight);
        }
        if (correct) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var rects = new SpriteRect[frameCount];
        var pairs = new SpriteNameFileIdPair[frameCount];
        for (var i = 0; i < frameCount; i++)
        {
            var xMin = Mathf.RoundToInt(sourceWidth * i / (float)frameCount);
            var xMax = Mathf.RoundToInt(sourceWidth * (i + 1) / (float)frameCount);
            var id = preserveIds ? current[i].spriteID : GUID.Generate();
            var name = $"{prefix}_{i:00}";
            rects[i] = new SpriteRect
            {
                name = name,
                rect = new Rect(xMin, 0f, xMax - xMin, sourceHeight),
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

    private static void EnsureSingleSprite(string path, SpriteAlignment alignment)
    {
        if (!File.Exists(path)) return;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        var pivot = alignment == SpriteAlignment.BottomCenter ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (importer.textureType == TextureImporterType.Sprite &&
            settings.spriteMode == (int)SpriteImportMode.Single &&
            settings.spriteAlignment == (int)alignment &&
            Vector2.Distance(settings.spritePivot, pivot) < 0.001f)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100f;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)alignment;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
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
            path = "Character Visual",
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

    private static bool UsesCharacterVisual(AnimationClip clip)
        => clip != null && AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .Any(binding => binding.path == "Character Visual" && binding.propertyName == "m_Sprite");
}
