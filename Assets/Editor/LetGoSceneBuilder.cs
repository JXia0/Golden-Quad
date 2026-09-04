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
    private const string BuildVersion = "4.7.0";
    private const string MarkerPath = "ProjectSettings/LetGoSceneBuild.version";
    private const string SpritePath = "Assets/Art/Placeholders/BlockSprite.asset";
    private const string GlowSpritePath = "Assets/Art/Placeholders/CourageGlow.asset";
    private static Sprite blockSprite;
    private static Sprite glowSprite;

    static LetGoSceneBuilder()
    {
        EditorApplication.delayCall += BuildWhenReady;
    }

    [MenuItem("Tools/Let Go/Build All Game Scenes")]
    public static void BuildAllScenes()
    {
        EnsureFolders();
        blockSprite = EnsureBlockSprite();
        glowSprite = EnsureGlowSprite();
        BuildPrologue();
        BuildKindergarten();
        BuildStage();
        BuildResearch();
        BuildFinalWalk();

        var paths = new[]
        {
            "Assets/Scenes/00_Prologue.unity",
            "Assets/Scenes/01_Kindergarten.unity",
            "Assets/Scenes/03_Stage.unity",
            "Assets/Scenes/05_Research.unity",
            "Assets/Scenes/06_FinalWalk.unity"
        };
        EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        File.WriteAllText(MarkerPath, BuildVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(paths[0]);
        LetGoArtBinder.ApplyFinalArt();
        Debug.Log("[LetGo] Built the five-scene hold-and-release playable flow.");
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
        var setup = CreateBase("01_Kindergarten", new Vector2(-5f, -2.1f), 0, 24f, new Color(0.08f, 0.09f, 0.14f), true, false);
        SetPlayerArtSlot(setup.Player, "char_adult");
        var report = CreateBlock("Carried Research Report", Vector2.zero, new Vector2(0.42f, 0.68f), Color.white, false, 4);
        report.transform.SetParent(setup.Player.transform, false);
        report.transform.localPosition = new Vector3(0.48f, 0.08f, 0f);
        AddArtSlot(report, "prop_research_report");
        new GameObject("Reset Journey Choices", typeof(JourneyStartReset));
        CreateLabel("成年后的第一次独立研究汇报", new Vector2(1.5f, 2.8f), 0.4f, new Color(0.7f, 0.76f, 0.9f));
        var officeBackground = CreateBlock("MeetingRoom", new Vector2(0f, -0.2f), new Vector2(34f, 5.5f), new Color(0.2f, 0.24f, 0.34f), false, -4);
        AddArtSlot(officeBackground, "bg_office_hallway");
        var officeClock = CreateBlock("Office Clock", new Vector2(-0.2f, 1.65f), new Vector2(1.15f, 1.15f), Color.white, false, -2);
        AddArtSlot(officeClock, "prop_office_clock");
        var waySign = CreateBlock("Meeting Way Sign", new Vector2(2f, 1.65f), new Vector2(2.4f, 0.65f), Color.white, false, -2);
        AddArtSlot(waySign, "prop_office_waysign");
        var doorSign = CreateBlock("Meeting Door Sign", new Vector2(4.4f, 0.6f), new Vector2(2.4f, 0.65f), Color.white, false, 2);
        AddArtSlot(doorSign, "prop_office_doorsigns");
        var door = CreateInteractable<StoryDoor>("Meeting Door", new Vector2(4.4f, -1.3f), new Vector2(1.2f, 2.7f), new Color(0.4f, 0.45f, 0.58f));
        AddArtSlot(door.gameObject, "prop_meeting_door");
        door.Configure(false, false, "按 E 触碰门把手");
        AddOpening("小时候，我以为长大，是某一天突然发生的事。\n像生日，像毕业，像门框上突然高出的一条线。", 0.8f, 6f);
        Save("00_Prologue");
    }

    private static void BuildKindergarten()
    {
        var setup = CreateBase("03_Stage", new Vector2(-8f, -2.1f), 4, 50f, new Color(0.11f, 0.09f, 0.16f));
        SetPlayerArtSlot(setup.Player, "char_child");
        var hand = AddHandConnection(setup.Player, false);
        var courage = setup.Player.GetComponent<CourageSystem>();
        courage.Configure(4.5f, 1.2f);
        courage.ConfigureSupport(hand, "parent", 3f, 4f, false);
        var guardian = CreateCoreHoldTarget("Parent", new Vector2(-9f, -1.7f), new Vector2(1f, 2.2f),
            new Color(1f, 0.65f, 0.32f), "char_parent", "parent", HoldTargetMode.Companion,
            "按住 E 牵住父母的手", 2.1f, 2.3f, 0, 1.15f, 3.6f, -4.2f);
        var warmLight = CreateBlock("Warm Light", new Vector2(-9f, -0.8f), new Vector2(5f, 5f),
            new Color(1f, 0.55f, 0.2f, 0.12f), false, -5);
        warmLight.transform.SetParent(guardian.transform, true);
        CreateLabel("牵手时勇气不会下降 · 门前放手后，余温会陪你 3 秒", new Vector2(-7.8f, 2.5f),
            0.25f, new Color(0.95f, 0.78f, 0.46f));
        CreateZone("Fear Begins", new Vector2(-6.3f, 0f),
            "牵着手时，影子不会靠近。松开以后，手心的余温还会陪我走一小段。",
            true, false, false);
        var kindergartenBackground = CreateBlock("Classroom Wall", new Vector2(9f, -0.3f), new Vector2(34f, 5.5f), new Color(0.15f, 0.17f, 0.25f), false, -4);
        AddArtSlot(kindergartenBackground, "bg_kindergarten_hall");

        var classroomGate = CreateBlock("Classroom Hand Gate", new Vector2(-2.8f, -0.5f), new Vector2(2.2f, 5.5f), new Color(0.95f, 0.72f, 0.35f, 0.3f), false, 2);
        AddArtSlot(classroomGate, "prop_kindergarten_door");
        var releaseGate = new GameObject("Release Parent At Door", typeof(ReleaseGate)).GetComponent<ReleaseGate>();
        releaseGate.Configure(hand, guardian.TargetId, -1.8f,
            "你停在门外，手心的温度却没有立刻消失。现在，我要带着它找到自己的座位。");

        var chair = CreateBlock("Oversized Chair", new Vector2(3f, -2.05f), new Vector2(1.3f, 1.4f), new Color(0.35f, 0.32f, 0.48f), true, 1);
        AddArtSlot(chair, "prop_named_chair");
        chair.AddComponent<ObjectiveStation>().Configure(
            "按 E 确认写着自己名字的座位",
            "我第一次在没有你指给我的时候，认出了自己的名字。", 1);
        CreateLabel("按 E 确认名字", new Vector2(3f, -0.95f), 0.24f, new Color(0.85f, 0.88f, 1f));
        var blocks = CreateBlock("Oversized Blocks", new Vector2(6.3f, -2.42f), new Vector2(1.6f, 0.65f), new Color(0.38f, 0.55f, 0.55f), true, 1);
        AddArtSlot(blocks, "deco_blocks");
        blocks.AddComponent<InspectPoint>().Configure(
            "按 E 看看散落的积木",
            "它们没有家里的积木那么可怕。只是需要我自己决定，绕过去，还是跳过去。");
        CreateLabel("Space / W / ↑ 跳过积木", new Vector2(6.3f, -1.25f), 0.24f, new Color(0.72f, 0.88f, 0.9f));
        CreateCarryItem("掉落的玩具", "kindergarten-toy", new Vector2(8.3f, -2.35f),
            "按 E 捡起玩具", "我可以把它带给那个正在哭的孩子。", "prop_toy", Color.white);

        var cryingChild = CreateCoreHoldTarget("Crying Child", new Vector2(10f, -1.7f), new Vector2(0.9f, 1.9f),
            new Color(0.72f, 0.42f, 0.48f), "char_crying_child", "crying-child", HoldTargetMode.Companion,
            "按住 E 牵住孩子的手", 2.1f, 2.5f, 3, 1.1f, 2.6f);
        cryingChild.gameObject.AddComponent<DeliveryStation>().Configure(
            "kindergarten-toy", "按 E 把玩具递给哭泣的孩子",
            "他接过玩具，慢慢停止了哭泣。刚才还需要别人安慰的我，也能先向前伸出手。",
            "地上好像有一个属于他的玩具。", "kindergarten", "shared-comfort");
        var teacher = CreatePerson("Teacher", new Vector2(16f, -1.7f), new Vector2(1f, 2.2f), new Color(0.55f, 0.68f, 0.72f));
        AddArtSlot(teacher, "char_teacher");
        CreateCoreSocket("Teacher Safe Area", new Vector2(16f, -1.5f), "crying-child", string.Empty, 1.8f, 3,
            "刚才还需要别人牵着的我，现在也能陪另一个人走一段路。", new Color(0.55f, 0.72f, 0.64f, 0.2f));

        var fearRoot = new GameObject("Unfamiliar Shadows").transform;
        var fears = new[]
        {
            CreateBlock("Shadow A", new Vector2(1f, 0.4f), new Vector2(1.4f, 2.8f), new Color(0.05f, 0.04f, 0.1f, 0.4f), false, -2).GetComponent<SpriteRenderer>(),
            CreateBlock("Shadow B", new Vector2(8f, 0.8f), new Vector2(1.8f, 3.2f), new Color(0.05f, 0.04f, 0.1f, 0.4f), false, -2).GetComponent<SpriteRenderer>()
        };
        foreach (var fear in fears)
        {
            fear.transform.SetParent(fearRoot, true);
            AddArtSlot(fear.gameObject, "fx_shadow_blob");
        }
        new GameObject("Emotional Environment", typeof(EmotionalEnvironment)).GetComponent<EmotionalEnvironment>()
            .Configure(hand, fearRoot, fears, Camera.main, new Color(0.11f, 0.09f, 0.16f), new Color(0.035f, 0.025f, 0.08f));

        var exit = CreateInteractable<StoryDoor>("Classroom Exit", new Vector2(20f, -1.1f), new Vector2(1.3f, 3.1f), new Color(0.45f, 0.5f, 0.65f));
        AddArtSlot(exit.gameObject, "prop_kindergarten_door");
        exit.Configure(true, false, "按 E 走进教室");
        AddOpening("第一次，我必须自己决定什么时候松开。", 0.8f, 3.5f);
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
        var setup = CreateBase("05_Research", new Vector2(-8f, -2.1f), 3, 48f, new Color(0.07f, 0.05f, 0.12f));
        SetPlayerArtSlot(setup.Player, "char_teen");
        var hand = AddHandConnection(setup.Player, true);
        var backstageParent = CreateCoreHoldTarget("Parent Backstage", new Vector2(-9f, -1.7f), new Vector2(1f, 2.2f),
            new Color(0.85f, 0.55f, 0.3f), "char_parent", "stage-parent", HoldTargetMode.Companion,
            "按住 E 接住肩上的手", 2.1f, 2.3f, 0, 1.15f, 3.6f, -4.5f);
        // The delivered curtain art includes a large transparent half, so each full 3:1 canvas
        // spans one side of the stage while its painted half frames the playable center.
        var curtainLeft = CreateBlock("Curtain Left", new Vector2(-4.5f, 0f), new Vector2(17.5f, 5.8f), new Color(0.3f, 0.03f, 0.08f), false, 2);
        var curtainRight = CreateBlock("Curtain Right", new Vector2(14.5f, 0f), new Vector2(17.5f, 5.8f), new Color(0.3f, 0.03f, 0.08f), false, 2);
        AddArtSlot(curtainLeft, "prop_stage_curtain_l");
        AddArtSlot(curtainRight, "prop_stage_curtain_r");
        var stageBackground = CreateBlock("Stage Background", new Vector2(6f, -0.2f), new Vector2(34f, 11.33f), new Color(0.08f, 0.04f, 0.12f), false, -5);
        AddArtSlot(stageBackground, "bg_stage_auditorium");
        var spotlightA = CreateBlock("Spotlight A", new Vector2(0f, 0f), new Vector2(3.6f, 7f), new Color(1f, 0.85f, 0.45f, 0.1f), false, -3);
        var spotlightB = CreateBlock("Spotlight B", new Vector2(5f, 0f), new Vector2(4f, 7f), new Color(1f, 0.85f, 0.45f, 0.12f), false, -3);
        var spotlightC = CreateBlock("Spotlight C", new Vector2(10f, 0f), new Vector2(3.6f, 7f), new Color(1f, 0.85f, 0.45f, 0.15f), false, -3);
        AddArtSlot(spotlightA, "fx_stage_spotlight");
        AddArtSlot(spotlightB, "fx_stage_spotlight");
        AddArtSlot(spotlightC, "fx_stage_spotlight");

        var fearRoot = new GameObject("Audience Fear").transform;
        var fears = CreateAudienceFear(fearRoot, new Vector2(5f, 0.55f));
        new GameObject("Emotional Environment", typeof(EmotionalEnvironment)).GetComponent<EmotionalEnvironment>()
            .Configure(hand, fearRoot, fears, Camera.main, new Color(0.1f, 0.06f, 0.13f), new Color(0.025f, 0.015f, 0.055f));

        var firstCueObject = CreateBlock("First Breath Cue", new Vector2(0f, -1.5f), new Vector2(1.8f, 0.25f), new Color(1f, 0.72f, 0.25f), false, 2);
        AddArtSlot(firstCueObject, "prop_stage_marker");
        firstCueObject.AddComponent<SelfAnchorCue>().Configure(hand, 2.2f, 0, string.Empty);
        CreateLabel("停下 · 按住 E 呼吸", new Vector2(0f, -0.4f), 0.28f, new Color(0.95f, 0.78f, 0.45f));

        var safeRoute = CreateBlock("Steady Route", new Vector2(5f, -2.25f), new Vector2(2f, 0.7f), new Color(0.35f, 0.55f, 0.72f), false, 1);
        AddArtSlot(safeRoute, "prop_stage_marker");
        safeRoute.AddComponent<BoxCollider2D>().isTrigger = true;
        safeRoute.AddComponent<StageRouteChoiceTrigger>().Configure("我选择了离幕布更近的位置。", string.Empty);
        CreateLabel("向右走：稳稳完成", new Vector2(5f, -1.35f), 0.25f, new Color(0.55f, 0.72f, 0.9f));

        CreateBlock("Forward Platform", new Vector2(5f, -1.35f), new Vector2(2.8f, 0.3f), new Color(0.72f, 0.4f, 0.48f), false, 0);
        var forwardRoute = CreateBlock("Forward Route", new Vector2(5f, -0.45f), new Vector2(2f, 1.1f), new Color(0.72f, 0.4f, 0.48f, 0.25f), false, 1);
        AddArtSlot(forwardRoute, "prop_stage_marker");
        forwardRoute.AddComponent<BoxCollider2D>().isTrigger = true;
        forwardRoute.AddComponent<StageRouteChoiceTrigger>().Configure("我选择向观众再靠近一步。", string.Empty);
        CreateLabel("跳进上方标记：主动向前一步", new Vector2(5f, 0.55f), 0.25f, new Color(0.9f, 0.55f, 0.62f));

        var finalCueObject = CreateBlock("Final Release Cue", new Vector2(10f, -1.5f), new Vector2(1.8f, 0.25f), new Color(1f, 0.82f, 0.4f), false, 2);
        AddArtSlot(finalCueObject, "prop_stage_marker");
        finalCueObject.AddComponent<FinalReleaseCue>().Configure(hand, 2.2f, 2);
        CreateLabel("吸气 · 松开时说出台词", new Vector2(10f, -0.4f), 0.28f, new Color(0.95f, 0.82f, 0.55f));
        var exit = CreateInteractable<StoryDoor>("Back Curtain", new Vector2(17f, -1.1f), new Vector2(1.3f, 3.1f), new Color(0.5f, 0.2f, 0.3f, 0.05f));
        exit.Configure(true, false, "按 E 走下舞台");
        AddOpening("这一次，没有人能替我走到灯光中央。", 0.8f, 3.5f);
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
        var setup = CreateBase("06_FinalWalk", new Vector2(-8f, -2.1f), 3, 60f, new Color(0.035f, 0.07f, 0.1f));
        SetPlayerArtSlot(setup.Player, "char_adult");
        var hand = AddHandConnection(setup.Player, false);
        var mentor = CreatePerson("Mentor", new Vector2(-9f, -1.7f), new Vector2(1f, 2.2f), new Color(0.34f, 0.6f, 0.68f));
        AddArtSlot(mentor, "char_mentor");
        var researchBackground = CreateBlock("Research Background", new Vector2(8f, -0.2f), new Vector2(34f, 5.5f), new Color(0.03f, 0.1f, 0.14f), false, -5);
        AddArtSlot(researchBackground, "bg_research_room");
        var researchDesk = CreateBlock("Research Desk", new Vector2(13f, -2.05f), new Vector2(8f, 2.1f), Color.white, false, 0);
        AddArtSlot(researchDesk, "prop_research_desk");
        CreateLabel("按住 E 抓住资料 · 松开 E 提交", new Vector2(-5f, 2.5f), 0.28f, new Color(0.62f, 0.82f, 0.86f));

        CreateCoreHoldTarget("Question Card", new Vector2(-4f, -1.45f), new Vector2(0.9f, 1.2f),
            new Color(0.25f, 0.55f, 0.68f), "node_question", "question", HoldTargetMode.Carryable,
            "按住 E 抓住问题", 1.8f, 3f);
        CreateCoreSocket("Question Board", new Vector2(1f, -1.2f), "question", string.Empty, 1.8f, 0,
            string.Empty, new Color(0.25f, 0.55f, 0.68f, 0.28f));
        CreateLabel("问题", new Vector2(1f, 0.1f), 0.32f, new Color(0.5f, 0.78f, 0.86f));

        CreateCoreHoldTarget("Photo Evidence", new Vector2(6f, -1.45f), new Vector2(1f, 1.2f),
            new Color(0.55f, 0.48f, 0.7f), "node_evidence_photo", "evidence-photo", HoldTargetMode.Carryable,
            "按住 E 抓住照片记录", 1.8f, 3f, 1, group: "evidence-choice", category: "research",
            value: "我选择相信亲眼记录下来的细节。");
        CreateCoreHoldTarget("Data Evidence", new Vector2(9f, -1.45f), new Vector2(1f, 1.2f),
            new Color(0.35f, 0.58f, 0.78f), "node_evidence_data", "evidence-data", HoldTargetMode.Carryable,
            "按住 E 抓住数据图表", 1.8f, 3f, 1, group: "evidence-choice", category: "research",
            value: "我选择相信仍不完整的数据趋势。");
        CreateCoreSocket("Evidence Board", new Vector2(13f, -1.2f), string.Empty, "evidence-choice", 1.8f, 1,
            string.Empty, new Color(0.4f, 0.55f, 0.75f, 0.28f));
        CreateLabel("选择一份证据", new Vector2(13f, 0.1f), 0.3f, new Color(0.55f, 0.7f, 0.9f));

        CreateCoreHoldTarget("Conclusion Card", new Vector2(18f, -1.45f), new Vector2(1f, 1.25f),
            new Color(0.52f, 0.4f, 0.68f), "node_conclusion", "conclusion", HoldTargetMode.Carryable,
            "按住 E 抓住尚未确定的结论", 1.9f, 3f, 2);
        CreateCoreSocket("Conclusion Board", new Vector2(22f, -1.2f), "conclusion", string.Empty, 1.9f, 2,
            "这不是唯一的答案，但这是我愿意负责的判断。", new Color(0.52f, 0.4f, 0.68f, 0.28f));
        CreateLabel("松手提交", new Vector2(22f, 0.1f), 0.3f, new Color(0.72f, 0.6f, 0.86f));

        var doubtRoot = new GameObject("Doubt Notes").transform;
        var doubts = new[]
        {
            CreateBlock("Doubt A", new Vector2(17f, 1.4f), new Vector2(0.9f, 1.8f), new Color(0.2f, 0.24f, 0.34f, 0.5f), false, 2).GetComponent<SpriteRenderer>(),
            CreateBlock("Doubt B", new Vector2(20f, 1.7f), new Vector2(1.1f, 2.2f), new Color(0.2f, 0.24f, 0.34f, 0.5f), false, 2).GetComponent<SpriteRenderer>(),
            CreateBlock("Doubt C", new Vector2(23f, 1.2f), new Vector2(0.85f, 1.7f), new Color(0.2f, 0.24f, 0.34f, 0.5f), false, 2).GetComponent<SpriteRenderer>()
        };
        foreach (var doubt in doubts)
        {
            doubt.transform.SetParent(doubtRoot, true);
            AddArtSlot(doubt.gameObject, "fx_shadow_blob");
        }
        new GameObject("Emotional Environment", typeof(EmotionalEnvironment)).GetComponent<EmotionalEnvironment>()
            .Configure(hand, doubtRoot, doubts, Camera.main, new Color(0.035f, 0.07f, 0.1f), new Color(0.02f, 0.035f, 0.06f), 2, 3);

        var exit = CreateInteractable<StoryDoor>("Finished Report", new Vector2(26f, -1.1f), new Vector2(1.4f, 3.2f), new Color(0.22f, 0.46f, 0.55f));
        AddArtSlot(exit.gameObject, "prop_meeting_door");
        exit.Configure(true, false, "按 E 带着报告回到会议室");
        AddOpening("这一次，没有人能告诉我哪个答案一定正确。", 0.8f, 3.5f);
        Save("05_Research");
    }

    private static void BuildFinalWalk()
    {
        var setup = CreateBase(string.Empty, new Vector2(-8f, -2.1f), 0, 72f, new Color(0.035f, 0.035f, 0.065f));
        SetPlayerArtSlot(setup.Player, "char_child");
        var hand = AddHandConnection(setup.Player, false);

        var kindergartenMemory = CreateBlock("Kindergarten Memory", new Vector2(-2f, -0.2f), new Vector2(14f, 5.5f), new Color(0.17f, 0.12f, 0.2f), false, -4);
        AddArtSlot(kindergartenMemory, "memory_kindergarten_set");
        var parentMemory = CreatePerson("Parent Memory", new Vector2(-8.8f, -1.7f), new Vector2(1f, 2.2f), new Color(1f, 0.62f, 0.3f, 0.7f));
        AddArtSlot(parentMemory, "char_parent");
        CreateBlock("Memory Hand Gate", new Vector2(3f, -0.3f), new Vector2(0.35f, 5.2f), new Color(1f, 0.7f, 0.3f, 0.25f), false, -2);
        CreateTransition("Child Memory", -7f, new Vector3(0.78f, 0.78f, 1f), new Color(0.75f, 0.82f, 1f), string.Empty, "char_child");

        var stageMemory = CreateBlock("Stage Memory", new Vector2(12f, -0.2f), new Vector2(14f, 5.5f), new Color(0.16f, 0.06f, 0.13f), false, -4);
        AddArtSlot(stageMemory, "memory_stage_set");
        var memorySpotlight = CreateBlock("Memory Spotlight", new Vector2(12f, 0f), new Vector2(5f, 7f), new Color(1f, 0.82f, 0.4f, 0.12f), false, -3);
        AddArtSlot(memorySpotlight, "fx_stage_spotlight");
        var steadyEcho = CreateBlock("Remembered Steady Route", new Vector2(10.5f, -1.75f), new Vector2(2.2f, 0.25f), new Color(0.35f, 0.55f, 0.72f), false, 1);
        steadyEcho.AddComponent<MemoryChoiceEcho>().Configure("stage", "我选择了离幕布更近的位置。");
        AddArtSlot(steadyEcho, "prop_stage_marker");
        var forwardEcho = CreateBlock("Remembered Forward Route", new Vector2(13.5f, -0.85f), new Vector2(2.2f, 0.25f), new Color(0.72f, 0.4f, 0.48f), false, 1);
        forwardEcho.AddComponent<MemoryChoiceEcho>().Configure("stage", "我选择向观众再靠近一步。");
        AddArtSlot(forwardEcho, "prop_stage_marker");
        CreateTransition("Teen Memory", 7f, new Vector3(0.9f, 0.9f, 1f), new Color(0.68f, 0.75f, 1f), string.Empty, "char_teen");

        var researchMemory = CreateBlock("Research Memory", new Vector2(26f, -0.2f), new Vector2(14f, 5.5f), new Color(0.045f, 0.13f, 0.17f), false, -4);
        AddArtSlot(researchMemory, "memory_research_set");
        var rememberedQuestion = CreateBlock("Remembered Question", new Vector2(23f, -1.5f), new Vector2(0.7f, 1f), new Color(0.25f, 0.55f, 0.68f), false, 1);
        AddArtSlot(rememberedQuestion, "node_question");
        var photoEcho = CreateBlock("Remembered Photo Evidence", new Vector2(26f, -1.5f), new Vector2(0.8f, 1f), new Color(0.55f, 0.48f, 0.7f), false, 1);
        photoEcho.AddComponent<MemoryChoiceEcho>().Configure("research", "我选择相信亲眼记录下来的细节。");
        AddArtSlot(photoEcho, "node_evidence_photo");
        var dataEcho = CreateBlock("Remembered Data Evidence", new Vector2(26f, -1.5f), new Vector2(0.8f, 1f), new Color(0.35f, 0.58f, 0.78f), false, 1);
        dataEcho.AddComponent<MemoryChoiceEcho>().Configure("research", "我选择相信仍不完整的数据趋势。");
        AddArtSlot(dataEcho, "node_evidence_data");
        var rememberedConclusion = CreateBlock("Remembered Conclusion", new Vector2(29f, -1.5f), new Vector2(0.7f, 1f), new Color(0.52f, 0.4f, 0.68f), false, 1);
        AddArtSlot(rememberedConclusion, "node_conclusion");
        CreateTransition("Adult Memory", 21f, Vector3.one, new Color(0.62f, 0.72f, 0.9f), string.Empty, "char_adult");

        CreateBlock("Unknown Wall", new Vector2(40f, -0.2f), new Vector2(14f, 5.5f), new Color(0.08f, 0.1f, 0.16f), false, -4);
        var youngPerson = CreateCoreHoldTarget("Young Presenter", new Vector2(37f, -1.7f), new Vector2(0.9f, 2f),
            new Color(0.45f, 0.62f, 0.82f), "char_teen", "young-presenter", HoldTargetMode.Recipient,
            "按住 E 牵住他的手", 2.2f, 2.5f);
        var finalDoor = CreateBlock("Unknown Door", new Vector2(40f, -1f), new Vector2(1.6f, 3.6f), new Color(0.75f, 0.78f, 0.88f), false, 1);
        AddArtSlot(finalDoor, "prop_unknown_door");
        new GameObject("Release The Next Person", typeof(ReleaseEndingGoal)).GetComponent<ReleaseEndingGoal>()
            .Configure(hand, youngPerson, 1.4f);
        AddOpening("向右走。", 0.8f, 2.5f);
        Save("06_FinalWalk");
    }

    private sealed class BaseSetup
    {
        public GameObject Player;
        public StorySceneDirector Director;
    }

    private static BaseSetup CreateBase(string nextScene, Vector2 spawn, int objectives, float groundWidth, Color background,
        bool playerEnabled = true, bool showGround = true)
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
            var groundPosition = new Vector2(spawn.x + groundWidth * 0.5f - 3f, -3.25f);
            var groundSize = new Vector2(groundWidth, 1f);
            if (showGround)
                CreateBlock("Ground", groundPosition, groundSize, new Color(0.12f, 0.14f, 0.2f), true, -1);
            else
                CreateInvisibleGround(groundPosition, groundSize);
        }

        var ui = CreateStoryUI();
        CreateSceneAudio();
        var directorObject = new GameObject("Story Scene Director");
        var director = directorObject.AddComponent<StorySceneDirector>();
        director.Configure(nextScene, player != null ? player.transform : null, checkpoint, objectives,
            ui.Narration, ui.Prompt, ui.Objective, ui.Fade, ui.Title);
        return new BaseSetup { Player = player, Director = director };
    }

    private static void CreateSceneAudio()
    {
        var go = new GameObject("Scene Audio", typeof(SceneAudio));
        var effects = go.AddComponent<AudioSource>();
        var ambience = go.AddComponent<AudioSource>();
        var music = go.AddComponent<AudioSource>();
        var tension = go.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        ambience.playOnAwake = false;
        music.playOnAwake = false;
        tension.playOnAwake = false;
        go.GetComponent<SceneAudio>().ConfigureSources(effects, ambience, music, tension);
    }

    private static GameObject CreatePlayer(Vector2 position)
    {
        var player = new GameObject("Player", typeof(Rigidbody2D), typeof(CapsuleCollider2D),
            typeof(PlayerController2D), typeof(CourageSystem), typeof(CarryInventory), typeof(Animator),
            typeof(CharacterAnimationDriver));
        player.transform.position = position;
        player.transform.localScale = Vector3.one;
        var visual = new GameObject("Character Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(player.transform, false);
        var renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = blockSprite;
        renderer.color = new Color(0.7f, 0.78f, 1f);
        renderer.sortingOrder = 3;
        player.GetComponent<PlayerController2D>().ConfigureCharacterVisual(visual.transform);
        var body = player.GetComponent<Rigidbody2D>();
        body.gravityScale = 2.3f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = player.GetComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.75f, 1.3f);

        var glow = new GameObject("Courage Glow", typeof(SpriteRenderer));
        glow.transform.SetParent(player.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        var glowRenderer = glow.GetComponent<SpriteRenderer>();
        glowRenderer.sprite = glowSprite;
        glowRenderer.color = new Color(1f, 0.72f, 0.25f, 0.65f);
        glowRenderer.sortingOrder = 2;
        var serialized = new SerializedObject(player.GetComponent<CourageSystem>());
        serialized.FindProperty("innerLight").objectReferenceValue = glowRenderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var carriedVisualObject = CreateBlock("Carried Item", Vector2.zero, new Vector2(0.45f, 0.45f), Color.white, false, 8);
        carriedVisualObject.transform.SetParent(player.transform, false);
        carriedVisualObject.transform.localPosition = new Vector3(0.8f, 0.6f, 0f);
        player.GetComponent<CarryInventory>().Configure(carriedVisualObject.GetComponent<SpriteRenderer>());
        return player;
    }

    private static void SetPlayerArtSlot(GameObject player, string slotId)
    {
        var renderer = player.transform.Find("Character Visual").GetComponent<SpriteRenderer>();
        AddArtSlot(player, slotId, player.GetComponent<Animator>(), renderer);
    }

    private static HandConnection AddHandConnection(GameObject player, bool selfAnchorEnabled)
    {
        var lineObject = new GameObject("Hand Connection Light", typeof(LineRenderer));
        var line = lineObject.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = new Color(1f, 0.65f, 0.28f);
        line.startWidth = line.endWidth = 0.14f;
        line.sortingOrder = 8;
        line.enabled = false;
        var connection = player.AddComponent<HandConnection>();
        connection.Configure(line, selfAnchorEnabled);
        return connection;
    }

    private static HoldTarget CreateCoreHoldTarget(string name, Vector2 position, Vector2 size, Color color,
        string artSlot, string targetId, HoldTargetMode mode, string prompt, float interactionRadius,
        float maxDistance, int prerequisite = 0, float companionOffset = 1.1f, float companionSpeed = 4f,
        float companionMaxX = 999f, string group = "", string category = "", string value = "")
    {
        var go = CreateBlock(name, position, size, color, false, 3);
        AddArtSlot(go, artSlot);
        var target = go.AddComponent<HoldTarget>();
        target.Configure(targetId, mode, prompt, interactionRadius, maxDistance, prerequisite,
            companionOffset, companionSpeed, companionMaxX, group, category, value);
        return target;
    }

    private static HoldSocket CreateCoreSocket(string name, Vector2 position, string targetId, string group,
        float radius, int prerequisite, string completionLine, Color color)
    {
        var go = CreateBlock(name, position, new Vector2(1.4f, 1.8f), color, false, 0);
        var socket = go.AddComponent<HoldSocket>();
        socket.Configure(targetId, group, radius, prerequisite, completionLine);
        return socket;
    }

    private static SpriteRenderer[] CreateFearEyes(Transform parent, Vector2 center, int count)
    {
        var renderers = new SpriteRenderer[count];
        for (var i = 0; i < count; i++)
        {
            var x = center.x + (i - count * 0.5f) * 1.35f;
            var y = center.y + Mathf.Sin(i * 1.7f) * 0.5f;
            var eye = CreateBlock($"Audience Eye {i + 1}", new Vector2(x, y), new Vector2(0.55f, 0.22f),
                new Color(0.85f, 0.86f, 1f, 0.55f), false, 0);
            AddArtSlot(eye, "audience_eyes");
            eye.transform.SetParent(parent, true);
            renderers[i] = eye.GetComponent<SpriteRenderer>();
        }
        return renderers;
    }

    private static SpriteRenderer[] CreateAudienceFear(Transform parent, Vector2 center)
    {
        var crowd = CreateBlock("Audience Eyes Artwork", center, new Vector2(20f, 4.4f),
            new Color(0.85f, 0.86f, 1f, 0.55f), false, 0);
        AddArtSlot(crowd, "audience_eyes");
        crowd.transform.SetParent(parent, true);
        return new[] { crowd.GetComponent<SpriteRenderer>() };
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

    private static HoldObjectiveStation CreateHoldObjective(string name, Vector2 position, string prompt, string line,
        float seconds, int prerequisite, Color color, float interactionRadius = 1.5f,
        string lockedPrompt = "先完成前面的步骤")
    {
        var station = CreateInteractable<HoldObjectiveStation>(name, position, new Vector2(1.25f, 1.25f), color);
        station.Configure(prompt, line, seconds, prerequisite, interactionRadius, lockedPrompt);
        CreateLabel(name, position + Vector2.up * 1.1f, 0.26f, new Color(0.85f, 0.86f, 0.95f));
        return station;
    }

    private static CarryItem CreateCarryItem(string name, string itemId, Vector2 position, string prompt, string line,
        string artSlot, Color color)
    {
        var item = CreateInteractable<CarryItem>(name, position, new Vector2(0.8f, 0.8f), color);
        item.Configure(itemId, prompt, line);
        AddArtSlot(item.gameObject, artSlot);
        CreateLabel(name, position + Vector2.up * 0.85f, 0.24f, new Color(0.8f, 0.84f, 0.94f));
        return item;
    }

    private static DeliveryStation CreateDelivery(string name, string itemId, Vector2 position, string prompt,
        string completionLine, string missingLine, string category, string choiceValue, string artSlot, Color color)
    {
        var station = CreateInteractable<DeliveryStation>(name, position, new Vector2(1.2f, 1.2f), color);
        station.Configure(itemId, prompt, completionLine, missingLine, category, choiceValue);
        AddArtSlot(station.gameObject, artSlot);
        CreateLabel(name, position + Vector2.up * 1.1f, 0.25f, new Color(0.82f, 0.86f, 0.95f));
        return station;
    }

    private static InspectPoint CreateInspect(string name, Vector2 position, string prompt, string line, string artSlot, Color color)
    {
        var point = CreateInteractable<InspectPoint>(name, position, new Vector2(0.65f, 0.65f), color);
        point.Configure(prompt, line);
        AddArtSlot(point.gameObject, artSlot);
        return point;
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

    private static void AddArtSlot(GameObject target, string slotId, Animator animator = null, SpriteRenderer renderer = null)
    {
        var slot = target.GetComponent<ArtSlot>();
        if (slot == null) slot = target.AddComponent<ArtSlot>();
        slot.Configure(slotId, renderer != null ? renderer : target.GetComponent<SpriteRenderer>(), animator);
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

    private static void CreateTransition(string name, float x, Vector3 scale, Color color, string line, string artSlotId)
    {
        var go = new GameObject(name, typeof(BoxCollider2D), typeof(FinalAgeTransition));
        go.transform.position = new Vector3(x, -1f, 0f);
        go.GetComponent<BoxCollider2D>().size = new Vector2(1f, 5f);
        go.GetComponent<FinalAgeTransition>().Configure(scale, color, line, artSlotId);
    }

    private static void CreateEyes(Vector2 center)
    {
        for (var i = 0; i < 10; i++)
        {
            var x = center.x - 6f + i * 1.3f;
            var y = center.y + (i % 2) * 0.55f;
            var eye = CreateBlock($"Audience Eye {i + 1}", new Vector2(x, y), new Vector2(0.28f, 0.12f), new Color(0.9f, 0.9f, 1f, 0.55f), false, 0);
            AddArtSlot(eye, "audience_eyes");
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

    private static void CreateInvisibleGround(Vector2 position, Vector2 size)
    {
        var ground = new GameObject("Ground", typeof(BoxCollider2D));
        ground.transform.position = position;
        ground.transform.localScale = new Vector3(size.x, size.y, 1f);
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
        var courageBackground = new GameObject("Courage", typeof(RectTransform), typeof(Image));
        courageBackground.transform.SetParent(canvasObject.transform, false);
        var courageRect = courageBackground.GetComponent<RectTransform>();
        courageRect.anchorMin = new Vector2(0.04f, 0.91f);
        courageRect.anchorMax = new Vector2(0.25f, 0.945f);
        courageRect.offsetMin = courageRect.offsetMax = Vector2.zero;
        courageBackground.GetComponent<Image>().sprite = blockSprite;
        courageBackground.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.1f, 0.8f);
        var courageFillObject = new GameObject("Courage Fill", typeof(RectTransform), typeof(Image), typeof(CourageHUD));
        courageFillObject.transform.SetParent(courageBackground.transform, false);
        var courageFillRect = courageFillObject.GetComponent<RectTransform>();
        courageFillRect.anchorMin = new Vector2(0.03f, 0.18f);
        courageFillRect.anchorMax = new Vector2(0.97f, 0.82f);
        courageFillRect.offsetMin = courageFillRect.offsetMax = Vector2.zero;
        var courageFill = courageFillObject.GetComponent<Image>();
        courageFill.sprite = blockSprite;
        courageFill.color = new Color(1f, 0.7f, 0.24f, 0.95f);
        courageFill.type = Image.Type.Filled;
        courageFill.fillMethod = Image.FillMethod.Horizontal;
        var fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeObject.transform.SetParent(canvasObject.transform, false);
        var fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
        var fade = fadeObject.GetComponent<Image>();
        fade.sprite = blockSprite;
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

    private static Sprite EnsureGlowSprite()
    {
        var existing = AssetDatabase.LoadAllAssetsAtPath(GlowSpritePath).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Courage Glow Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[size * size];
        var center = (size - 1) * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                var alpha = 1f - Mathf.SmoothStep(0.05f, 1f, distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, GlowSpritePath);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = "Courage Glow Sprite";
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
