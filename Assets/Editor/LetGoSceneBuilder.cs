using System.IO;
using System.Linq;
using LetGo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LetGoSceneBuilder
{
    private const string BuildVersion = "1.1.0";
    private const string MarkerPath = "ProjectSettings/LetGoSceneBuild.version";
    private const string SpritePath = "Assets/Art/Placeholders/BlockSprite.asset";
    private static Sprite blockSprite;

    static LetGoSceneBuilder()
    {
        EditorApplication.delayCall += BuildWhenReady;
    }

    [MenuItem("Tools/Let Go/Build All Game Scenes")]
    public static void BuildAllScenes()
    {
        EnsureFolders();
        blockSprite = EnsureBlockSprite();
        BuildPrologue();
        BuildKindergarten();
        BuildInterludeOne();
        BuildStage();
        BuildInterludeTwo();
        BuildResearch();
        BuildFinalWalk();

        var paths = new[]
        {
            "Assets/Scenes/00_Prologue.unity",
            "Assets/Scenes/01_Kindergarten.unity",
            "Assets/Scenes/02_Interlude_Firsts.unity",
            "Assets/Scenes/03_Stage.unity",
            "Assets/Scenes/04_Interlude_Growing.unity",
            "Assets/Scenes/05_Research.unity",
            "Assets/Scenes/06_FinalWalk.unity"
        };
        EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        File.WriteAllText(MarkerPath, BuildVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(paths[0]);
        Debug.Log("[LetGo] Built the complete seven-scene playable flow.");
    }

    private static void BuildWhenReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildWhenReady;
            return;
        }
        if (File.Exists(MarkerPath) && File.ReadAllText(MarkerPath).Trim() == BuildVersion) return;
        BuildAllScenes();
    }

    private static void BuildPrologue()
    {
        var setup = CreateBase("01_Kindergarten", new Vector2(-5f, -2.1f), 0, 24f, new Color(0.08f, 0.09f, 0.14f));
        new GameObject("Reset Journey Choices", typeof(JourneyStartReset));
        CreateLabel("成年后的第一次独立研究汇报", new Vector2(1.5f, 2.8f), 0.4f, new Color(0.7f, 0.76f, 0.9f));
        CreateBlock("MeetingRoom", new Vector2(5f, -0.3f), new Vector2(2.2f, 5f), new Color(0.2f, 0.24f, 0.34f));
        CreateLabel("会议室", new Vector2(5f, 1.5f), 0.35f, Color.white);
        var door = CreateInteractable<StoryDoor>("Meeting Door", new Vector2(4.4f, -1.3f), new Vector2(1.2f, 2.7f), new Color(0.4f, 0.45f, 0.58f));
        door.Configure(false, false, "按 E 触碰门把手");
        AddOpening("小时候，我以为长大，是某一天突然发生的事。\n像生日，像毕业，像门框上突然高出的一条线。", 0.8f, 6f);
        Save("00_Prologue");
    }

    private static void BuildKindergarten()
    {
        var setup = CreateBase("02_Interlude_Firsts", new Vector2(-8f, -2.1f), 3, 38f, new Color(0.11f, 0.09f, 0.16f));
        var guardian = CreatePerson("Parent", new Vector2(-9f, -1.7f), new Vector2(1f, 2.2f), new Color(1f, 0.65f, 0.32f));
        CreateBlock("Warm Light", new Vector2(-9f, -0.8f), new Vector2(5f, 5f), new Color(1f, 0.55f, 0.2f, 0.12f), false, -5);
        CreateLabel("幼儿园", new Vector2(5f, 3f), 0.55f, new Color(0.95f, 0.78f, 0.46f));
        CreateBlock("Classroom Wall", new Vector2(8f, -0.3f), new Vector2(18f, 5.2f), new Color(0.15f, 0.17f, 0.25f), false, -4);

        var bondObject = new GameObject("Hand Light");
        var line = bondObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = new Color(1f, 0.7f, 0.35f);
        line.startWidth = line.endWidth = 0.12f;
        line.sortingOrder = 5;
        var bond = bondObject.AddComponent<GuardianBond>();
        bond.Configure(setup.Player.transform, guardian.transform);

        var release = CreateInteractable<HandReleasePoint>("Classroom Threshold", new Vector2(-3f, -1f), new Vector2(0.7f, 2.5f), new Color(0.8f, 0.65f, 0.35f, 0.4f));
        release.Configure(bond);
        var comfort = new GameObject("Return To Parent", typeof(BoxCollider2D), typeof(ComfortZone));
        comfort.transform.position = new Vector3(-8f, -1f, 0f);
        comfort.GetComponent<BoxCollider2D>().size = new Vector2(3f, 4f);
        comfort.GetComponent<BoxCollider2D>().isTrigger = true;
        var backpack = CreateObjective("书包柜", new Vector2(1f, -1.3f), "按 E 放好书包", "书包有了自己的位置。", 0, new Color(0.34f, 0.55f, 0.72f));
        backpack.RecordChoice("kindergarten", "我选择先安顿好自己。", true);
        var seat = CreateObjective("名字座位", new Vector2(6f, -1.3f), "按 E 坐到自己的位置", "这里写着我的名字。", 0, new Color(0.52f, 0.42f, 0.7f));
        seat.RecordChoice("kindergarten", "我选择先找到属于自己的位置。", true);
        var child = CreateObjective("哭泣的孩子", new Vector2(11f, -1.3f), "按 E 递出玩具", "原来，我也可以把一点勇气递给别人。", 0, new Color(0.72f, 0.42f, 0.48f));
        child.RecordChoice("kindergarten", "我选择先走向另一个害怕的人。", true);
        var exit = CreateInteractable<StoryDoor>("Classroom Exit", new Vector2(15f, -1.1f), new Vector2(1.3f, 3.1f), new Color(0.45f, 0.5f, 0.65f));
        exit.Configure(true, false, "按 E 走进教室");
        AddOpening("我第一次站在那扇门前时，\n以为门的另一边，是一个没有你的世界。", 0.8f, 5f);
        Save("01_Kindergarten");
    }

    private static void BuildInterludeOne()
    {
        CreateBase("03_Stage", Vector2.zero, 0, 20f, new Color(0.05f, 0.06f, 0.1f), false);
        AddInterlude(new[]
        {
            "第一次独自在朋友家过夜",
            "第一次在课堂上举手",
            "第一次一个人坐公交",
            "第一次发现，父母也会疲惫",
            "后来，我遇见了更多的第一次。\n我还是会害怕，只是渐渐学会了，不再每一次都回头。"
        }, 1.8f);
        Save("02_Interlude_Firsts");
    }

    private static void BuildStage()
    {
        var setup = CreateBase("04_Interlude_Growing", new Vector2(-8f, -2.1f), 3, 40f, new Color(0.07f, 0.05f, 0.12f));
        CreatePerson("Parent Backstage", new Vector2(-9f, -1.7f), new Vector2(1f, 2.2f), new Color(0.85f, 0.55f, 0.3f));
        CreateBlock("Curtain Left", new Vector2(-4.5f, 0f), new Vector2(2f, 7f), new Color(0.3f, 0.03f, 0.08f), false, 2);
        CreateBlock("Curtain Right", new Vector2(14.5f, 0f), new Vector2(2f, 7f), new Color(0.3f, 0.03f, 0.08f), false, 2);
        CreateBlock("Spotlight A", new Vector2(0f, 0f), new Vector2(4f, 7f), new Color(1f, 0.85f, 0.45f, 0.1f), false, -3);
        CreateBlock("Spotlight B", new Vector2(5f, 0f), new Vector2(4f, 7f), new Color(1f, 0.85f, 0.45f, 0.12f), false, -3);
        CreateBlock("Spotlight C", new Vector2(10f, 0f), new Vector2(4f, 7f), new Color(1f, 0.85f, 0.45f, 0.15f), false, -3);
        CreateEyes(new Vector2(4.5f, 2.4f));
        CreateZone("Stage Fear", new Vector2(-3f, -1f), "后来我学会了一个人走路。\n却还没有学会，被所有人看见。", true, false, true);
        CreateObjective("进入聚光灯", new Vector2(0f, -1.3f), "按 E 稳住呼吸", "脚步在发抖，但我还站在这里。", 0, new Color(0.7f, 0.55f, 0.25f));
        CreateChoice("稳稳完成", new Vector2(4f, -1.3f), "stage-performance", "stage", "我选择稳稳地完成每一个动作。", "按 E 选择稳稳完成", "我没有追求完美，只是完成了下一个动作。", 1, new Color(0.35f, 0.55f, 0.72f));
        CreateChoice("向前一步", new Vector2(7f, -1.3f), "stage-performance", "stage", "我选择向前一步，让所有人看见我。", "按 E 选择主动向前", "我向聚光灯前多走了一步。", 1, new Color(0.72f, 0.4f, 0.48f));
        CreateObjective("最后一句", new Vector2(10f, -1.3f), "按 E 说出最后一句", "我……准备好了。", 2, new Color(0.9f, 0.7f, 0.3f));
        var exit = CreateInteractable<StoryDoor>("Back Curtain", new Vector2(14f, -1.1f), new Vector2(1.3f, 3.1f), new Color(0.5f, 0.2f, 0.3f));
        exit.Configure(true, false, "按 E 走下舞台");
        AddOpening("第二次站在门前，我已经不再需要你牵着我。\n可我还是希望，你不要走。", 0.8f, 5f);
        Save("03_Stage");
    }

    private static void BuildInterludeTwo()
    {
        CreateBase("05_Research", Vector2.zero, 0, 20f, new Color(0.04f, 0.06f, 0.09f), false);
        AddInterlude(new[]
        {
            "第一次被拒绝",
            "第一次和朋友争吵",
            "第一次离开家",
            "第一次做出错误决定",
            "第一次发现，道歉比解释更困难",
            "长大以后，门没有变少。\n只是门外等我的人，越来越少。"
        }, 1.65f);
        Save("04_Interlude_Growing");
    }

    private static void BuildResearch()
    {
        var setup = CreateBase("06_FinalWalk", new Vector2(-8f, -2.1f), 3, 42f, new Color(0.035f, 0.07f, 0.1f));
        CreatePerson("Mentor", new Vector2(-9f, -1.7f), new Vector2(1f, 2.2f), new Color(0.34f, 0.6f, 0.68f));
        CreateLabel("这一次，我不能告诉你答案。\n因为它还不存在。", new Vector2(-6.5f, 2.2f), 0.3f, new Color(0.62f, 0.82f, 0.86f));
        CreateZone("Research Threshold", new Vector2(-4f, -1f), "再后来，没有人告诉我应该走向哪里。\n我才发现，大人也会害怕走错。", true, false, true);
        var question = CreateObjective("问题", new Vector2(-0.5f, -1.2f), "按 E 写下问题", "如果我理解错了呢？\n——我可能理解错，但我可以重新检查。", 0, new Color(0.25f, 0.55f, 0.68f));
        question.RecordChoice("research", "面对未知时，我选择先确认问题。", true);
        var evidence = CreateObjective("证据", new Vector2(5f, -1.2f), "按 E 整理证据", "如果结果毫无意义呢？\n——不完美的结果，仍然是结果。", 0, new Color(0.38f, 0.48f, 0.72f));
        evidence.RecordChoice("research", "面对未知时，我选择先相信证据。", true);
        var conclusion = CreateObjective("结论", new Vector2(10.5f, -1.2f), "按 E 写下结论", "如果他们问了我答不出的问题呢？\n——我可以诚实地说，我不知道。", 0, new Color(0.52f, 0.4f, 0.68f));
        conclusion.RecordChoice("research", "面对未知时，我选择先写下自己的判断。", true);
        CreateLabel("问题   →   证据   →   结论", new Vector2(5f, 2.6f), 0.38f, new Color(0.55f, 0.75f, 0.88f));
        var exit = CreateInteractable<StoryDoor>("Finished Report", new Vector2(15f, -1.1f), new Vector2(1.4f, 3.2f), new Color(0.22f, 0.46f, 0.55f));
        exit.Configure(true, false, "按 E 带着报告回到会议室");
        AddOpening("支持的方式也在改变。\n父母给我保护，老师给我鼓励，而导师给我信任。", 0.8f, 5f);
        Save("05_Research");
    }

    private static void BuildFinalWalk()
    {
        var setup = CreateBase(string.Empty, new Vector2(-8f, -2.1f), 0, 62f, new Color(0.035f, 0.035f, 0.065f));
        var courage = setup.Player.GetComponent<CourageSystem>();
        courage.Draining = false;

        CreateBlock("Kindergarten Memory", new Vector2(-2f, -0.2f), new Vector2(14f, 5.5f), new Color(0.17f, 0.12f, 0.2f), false, -4);
        CreatePerson("Parent Memory", new Vector2(-8.8f, -1.7f), new Vector2(1f, 2.2f), new Color(1f, 0.62f, 0.3f, 0.7f));
        CreateLabel("幼儿园", new Vector2(-2f, 2.6f), 0.42f, new Color(0.9f, 0.7f, 0.4f));
        CreateChoiceEcho("kindergarten", new Vector2(-2f, 0.7f), new Color(0.85f, 0.72f, 0.5f));
        CreateTransition("Child Memory", -7f, new Vector3(0.7f, 0.8f, 1f), new Color(0.75f, 0.82f, 1f), "第一次，我以为放开你的手，就会失去你。");

        CreateBlock("Stage Memory", new Vector2(12f, -0.2f), new Vector2(14f, 5.5f), new Color(0.16f, 0.06f, 0.13f), false, -4);
        CreateBlock("Memory Spotlight", new Vector2(12f, 0f), new Vector2(5f, 7f), new Color(1f, 0.82f, 0.4f, 0.12f), false, -3);
        CreateLabel("舞台", new Vector2(12f, 2.6f), 0.42f, new Color(0.9f, 0.68f, 0.4f));
        CreateChoiceEcho("stage", new Vector2(12f, 0.7f), new Color(0.85f, 0.65f, 0.55f));
        CreateTransition("Teen Memory", 7f, new Vector3(0.9f, 1f, 1f), new Color(0.68f, 0.75f, 1f), "后来我学会了一个人向前。\n却还没有学会，被所有人看见。");

        CreateBlock("Research Memory", new Vector2(26f, -0.2f), new Vector2(14f, 5.5f), new Color(0.045f, 0.13f, 0.17f), false, -4);
        CreateLabel("问题      证据      结论", new Vector2(26f, 2.6f), 0.38f, new Color(0.5f, 0.78f, 0.85f));
        CreateLabel("我可能会错。\n它可能还不够。\n我可以承认我不知道。", new Vector2(26f, 0.7f), 0.32f, new Color(0.48f, 0.58f, 0.68f));
        CreateChoiceEcho("research", new Vector2(26f, -0.45f), new Color(0.5f, 0.72f, 0.78f));
        CreateTransition("Adult Memory", 21f, Vector3.one, new Color(0.62f, 0.72f, 0.9f), "再后来，没有人告诉我应该走向哪里。\n我才发现，大人也会害怕走错。");

        CreateBlock("Unknown Wall", new Vector2(40f, -0.2f), new Vector2(14f, 5.5f), new Color(0.08f, 0.1f, 0.16f), false, -4);
        var finalDoor = CreateInteractable<StoryDoor>("Unknown Door", new Vector2(40f, -1f), new Vector2(1.6f, 3.6f), new Color(0.75f, 0.78f, 0.88f));
        finalDoor.Configure(false, true, "按 E 寻找一只手");
        CreateLabel("?", new Vector2(40f, 1.4f), 0.65f, new Color(0.9f, 0.92f, 1f));
        AddOpening("向右走。\n你可以停下来，也可以回头。", 0.8f, 4f);
        Save("06_FinalWalk");
    }

    private sealed class BaseSetup
    {
        public GameObject Player;
        public StorySceneDirector Director;
    }

    private static BaseSetup CreateBase(string nextScene, Vector2 spawn, int objectives, float groundWidth, Color background, bool playerEnabled = true)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow2D));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(spawn.x + 4f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.3f;
        camera.backgroundColor = background;
        camera.clearFlags = CameraClearFlags.SolidColor;

        GameObject player = null;
        Transform checkpoint = null;
        if (playerEnabled)
        {
            player = CreatePlayer(spawn);
            cameraObject.GetComponent<CameraFollow2D>().SetTarget(player.transform);
            var checkpointObject = new GameObject("Initial Checkpoint");
            checkpointObject.transform.position = spawn;
            checkpoint = checkpointObject.transform;
            CreateBlock("Ground", new Vector2(spawn.x + groundWidth * 0.5f - 3f, -3.25f), new Vector2(groundWidth, 1f), new Color(0.12f, 0.14f, 0.2f), true, -1);
        }

        var ui = CreateStoryUI();
        var directorObject = new GameObject("Story Scene Director");
        var director = directorObject.AddComponent<StorySceneDirector>();
        director.Configure(nextScene, player != null ? player.transform : null, checkpoint, objectives,
            ui.Narration, ui.Prompt, ui.Objective, ui.Fade, ui.Title);
        return new BaseSetup { Player = player, Director = director };
    }

    private static GameObject CreatePlayer(Vector2 position)
    {
        var player = new GameObject("Player", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CapsuleCollider2D),
            typeof(PlayerController2D), typeof(CourageSystem));
        player.transform.position = position;
        player.transform.localScale = new Vector3(0.75f, 1.3f, 1f);
        var renderer = player.GetComponent<SpriteRenderer>();
        renderer.sprite = blockSprite;
        renderer.color = new Color(0.7f, 0.78f, 1f);
        renderer.sortingOrder = 3;
        var body = player.GetComponent<Rigidbody2D>();
        body.gravityScale = 2.3f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = player.GetComponent<CapsuleCollider2D>();
        collider.size = Vector2.one;

        var glow = CreateBlock("Inner Light", Vector2.zero, Vector2.one, new Color(1f, 0.72f, 0.25f, 0.4f), false, 2);
        glow.transform.SetParent(player.transform, false);
        glow.transform.localScale = Vector3.one * 1.5f;
        var serialized = new SerializedObject(player.GetComponent<CourageSystem>());
        serialized.FindProperty("innerLight").objectReferenceValue = glow.GetComponent<SpriteRenderer>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return player;
    }

    private static GameObject CreatePerson(string name, Vector2 position, Vector2 size, Color color)
        => CreateBlock(name, position, size, color, false, 1);

    private static ObjectiveStation CreateObjective(string name, Vector2 position, string prompt, string line, int prerequisite, Color color)
    {
        var objective = CreateInteractable<ObjectiveStation>(name, position, new Vector2(1.2f, 1.2f), color);
        objective.Configure(prompt, line, prerequisite);
        CreateLabel(name, position + Vector2.up * 1.1f, 0.26f, new Color(0.82f, 0.86f, 0.95f));
        return objective;
    }

    private static ChoiceStation CreateChoice(string name, Vector2 position, string group, string category, string value,
        string prompt, string line, int prerequisite, Color color)
    {
        var choice = CreateInteractable<ChoiceStation>(name, position, new Vector2(1.2f, 1.2f), color);
        choice.Configure(group, category, value, prompt, line, prerequisite);
        CreateLabel(name, position + Vector2.up * 1.1f, 0.26f, new Color(0.85f, 0.86f, 0.95f));
        return choice;
    }

    private static T CreateInteractable<T>(string name, Vector2 position, Vector2 size, Color color) where T : Component
    {
        var go = CreateBlock(name, position, size, color, false, 1);
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        return go.AddComponent<T>();
    }

    private static StoryZone CreateZone(string name, Vector2 position, string line, bool beginDrain, bool stopDrain, bool checkpoint)
    {
        var go = new GameObject(name, typeof(BoxCollider2D), typeof(StoryZone));
        go.transform.position = position;
        go.GetComponent<BoxCollider2D>().size = new Vector2(1.5f, 5f);
        var zone = go.GetComponent<StoryZone>();
        zone.Configure(line, beginDrain, stopDrain, checkpoint);
        return zone;
    }

    private static void CreateTransition(string name, float x, Vector3 scale, Color color, string line)
    {
        var go = new GameObject(name, typeof(BoxCollider2D), typeof(FinalAgeTransition));
        go.transform.position = new Vector3(x, -1f, 0f);
        go.GetComponent<BoxCollider2D>().size = new Vector2(1f, 5f);
        go.GetComponent<FinalAgeTransition>().Configure(scale, color, line);
    }

    private static void CreateEyes(Vector2 center)
    {
        for (var i = 0; i < 10; i++)
        {
            var x = center.x - 6f + i * 1.3f;
            var y = center.y + (i % 2) * 0.55f;
            CreateBlock($"Audience Eye {i + 1}", new Vector2(x, y), new Vector2(0.28f, 0.12f), new Color(0.9f, 0.9f, 1f, 0.55f), false, 0);
        }
    }

    private static GameObject CreateBlock(string name, Vector2 position, Vector2 size, Color color, bool solid = false, int order = 0)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.position = position;
        go.transform.localScale = size;
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = blockSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (solid) go.AddComponent<BoxCollider2D>();
        return go;
    }

    private static void CreateLabel(string value, Vector2 position, float size, Color color)
    {
        var go = new GameObject($"Label - {value.Split('\n')[0]}", typeof(TextMesh));
        go.transform.position = position;
        var text = go.GetComponent<TextMesh>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 64;
        text.characterSize = size * 0.1f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        text.GetComponent<MeshRenderer>().sortingOrder = 6;
    }

    private static void CreateChoiceEcho(string category, Vector2 position, Color color)
    {
        var go = new GameObject($"Choice Echo - {category}", typeof(TextMesh), typeof(ChoiceEchoLabel));
        go.transform.position = position;
        var text = go.GetComponent<TextMesh>();
        text.text = string.Empty;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 64;
        text.characterSize = 0.03f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        text.GetComponent<MeshRenderer>().sortingOrder = 6;
        go.GetComponent<ChoiceEchoLabel>().Configure(category);
    }

    private static void AddOpening(string line, float delay, float duration)
    {
        var go = new GameObject("Opening Narration", typeof(OpeningNarration));
        go.GetComponent<OpeningNarration>().Configure(line, delay, duration);
    }

    private static void AddInterlude(string[] cards, float seconds)
    {
        var go = new GameObject("Interlude", typeof(InterludeController));
        go.GetComponent<InterludeController>().Configure(cards, seconds);
    }

    private sealed class StoryUI
    {
        public Text Narration;
        public Text Prompt;
        public Text Objective;
        public Image Fade;
        public Text Title;
    }

    private static StoryUI CreateStoryUI()
    {
        var canvasObject = new GameObject("Story UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var narration = CreateUIText("Narration", canvasObject.transform, 42, TextAnchor.LowerCenter, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.28f));
        var prompt = CreateUIText("Prompt", canvasObject.transform, 30, TextAnchor.MiddleCenter, new Vector2(0.3f, 0.28f), new Vector2(0.7f, 0.36f));
        prompt.color = new Color(1f, 0.82f, 0.4f);
        var objective = CreateUIText("Objective", canvasObject.transform, 24, TextAnchor.UpperRight, new Vector2(0.73f, 0.9f), new Vector2(0.97f, 0.98f));
        var fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeObject.transform.SetParent(canvasObject.transform, false);
        var fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
        var fade = fadeObject.GetComponent<Image>();
        fade.color = Color.black;
        var title = CreateUIText("Ending Title", canvasObject.transform, 52, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        title.color = new Color(0.95f, 0.9f, 0.78f);
        return new StoryUI { Narration = narration, Prompt = prompt, Objective = objective, Fade = fade, Title = title };
    }

    private static Text CreateUIText(string name, Transform parent, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.92f, 0.94f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Sprite EnsureBlockSprite()
    {
        var existing = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;
        var texture = new Texture2D(2, 2) { name = "Block Texture" };
        texture.SetPixels(Enumerable.Repeat(Color.white, 4).ToArray());
        texture.Apply();
        AssetDatabase.CreateAsset(texture, SpritePath);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        sprite.name = "Block Sprite";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssets();
        return sprite;
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Art/Placeholders");
    }

    private static void Save(string sceneName)
    {
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
    }
}
