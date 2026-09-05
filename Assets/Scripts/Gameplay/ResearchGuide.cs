using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    public enum ResearchGuideStep { EnterLab, StartTrial, Bridge, Comfort, Moving, BringReport, ReturnReport, Complete }

    [DefaultExecutionOrder(80)]
    public sealed class ResearchGuide : MonoBehaviour
    {
        private ResearchExpedition chapter;
        private HandConnection hand;
        private JourneyOverlay ui;
        private Text goal, situation, focus, tool, help, helpControl;
        private Image helpPanel;
        private LineRenderer reachable;
        private bool expanded;
        public bool HelpVisible { get; private set; }
        public ResearchGuideStep Step { get; private set; }
        public string GoalText => goal == null ? "" : goal.text;
        public string SituationText => situation == null ? "" : situation.text;
        public string FocusText => focus == null ? "" : focus.text;

        public void Initialize(ResearchExpedition expedition, HandConnection connection)
        {
            chapter = expedition;
            hand = connection;
            reachable = GetComponent<JourneyVisuals>().GameplayLine("Nearest usable tool", JourneyVisuals.Warm, 0.04f);
            ui = JourneyOverlay.Create("Research Goals And Object Guide");
            ui.Picture("Goal background", new Vector2(-225, 321), new Vector2(720, 54), null, new Color(0.025f, 0.04f, 0.055f, 0.72f));
            goal = ui.Label("Persistent research goal", new Vector2(-225, 321), new Vector2(680, 36), 20, TextAnchor.MiddleLeft);
            situation = ui.Label("What is happening", Vector2.zero, new Vector2(1, 1), 1);
            situation.gameObject.SetActive(false);
            helpControl = ui.Label("Optional hint key", new Vector2(535, 321), new Vector2(130, 34), 17);
            focus = ui.Label("Current destination", Vector2.zero, new Vector2(350, 65), 21);
            focus.gameObject.SetActive(false);
            tool = ui.Label("Nearby reusable object", Vector2.zero, new Vector2(290, 40), 20);
            tool.gameObject.SetActive(false);
            helpPanel = ui.Picture("Optional hint background", new Vector2(0, 184), new Vector2(1200, 104), null, new Color(0.025f, 0.04f, 0.055f, 0.93f));
            help = ui.Label("Available approaches", new Vector2(0, 184), new Vector2(1140, 96), 19, TextAnchor.MiddleLeft);
            Draw();
        }

        private void LateUpdate()
        {
            if (chapter == null || ui == null) return;
            if (GameInput.HelpPressed)
            {
                expanded = !HelpVisible;
            }
            Draw();
        }

        private void Draw()
        {
            var next = chapter.ReportReturned ? ResearchGuideStep.Complete : chapter.ShortcutOpen ?
                hand.CurrentTarget == chapter.Report ? ResearchGuideStep.ReturnReport : ResearchGuideStep.BringReport :
                !chapter.GateLatched ? ResearchGuideStep.EnterLab : chapter.State == LearnerState.Waiting ? ResearchGuideStep.StartTrial :
                chapter.State == LearnerState.NeedsBridge ? ResearchGuideStep.Bridge :
                chapter.State == LearnerState.NeedsComfort || chapter.State == LearnerState.Returning ? ResearchGuideStep.Comfort : ResearchGuideStep.Moving;
            if (next != Step) { Step = next; expanded = false; }
            goal.text = "目标 · 帮助实验小人到达出口";
            var destination = chapter.Learner.transform.position;
            string hint;
            switch (Step)
            {
                case ResearchGuideStep.EnterLab:
                    goal.text = chapter.GateOpen ? "门开了 · 进入后按 F 拉开内侧门闩" : "帮助实验小人 · 先用压力板打开右侧机关门";
                    situation.text = chapter.GateOpen ? "门开了。进入实验区后，拉开内侧门闩就能自由往返。" : "先进入右侧实验区。地上的压力板控制机关门。";
                    destination = new Vector3(chapter.GateOpen ? 10.5f : 6.5f, -1.9f);
                    focus.text = chapter.GateOpen ? "内侧门闩 · F 打开" : "压力板 → 机关门";
                    hint = "试试把箱子留在压力板上，再跳过箱子进门。也可以去左侧上层档案找另一件工具。";
                    break;
                case ResearchGuideStep.StartTrial:
                    goal.text = "靠近实验小人 · F 让它试走，E 牵手";
                    situation.text = "实验小人在等你。先让它试走，看看哪里走不过去。";
                    focus.text = "实验小人\nF 试走 · 按住 E 牵手";
                    hint = "可以先观察它停在哪里，再搬工具；也可以牵着一起试。成功没有唯一解法。";
                    break;
                case ResearchGuideStep.Bridge:
                    goal.text = "小人停在缺口前 · 找一件能垫脚的工具";
                    situation.text = "小人停在缺口前：这里没有落脚点。";
                    destination = new Vector3(16f, -2.2f);
                    focus.text = "缺口 · 需要落脚点";
                    hint = "把箱子或折叠板搬到缺口中央，松开 E 放下。门闩打开后，原来压门的工具可以拿回来。";
                    break;
                case ResearchGuideStep.Comfort:
                    var blownOut = !chapter.LampWorking;
                    goal.text = blownOut ? "灯被风吹灭 · 到右侧窗口按 F 关窗" : "它不敢走进暗处 · 光或陪伴能让它前进";
                    situation.text = blownOut ? "灯被风吹灭了，小人退回了亮处。右侧窗户还开着。" : "小人不敢独自进入暗处，需要看得见或感到有人陪伴。";
                    destination = blownOut ? new Vector3(21f, -1.5f) : chapter.Learner.transform.position;
                    focus.text = blownOut ? "漏风的窗 · F 关窗" : "暗区前的小人 · E 可以牵住";
                    hint = "可以搬灯照路、牵手陪它走；带来了童年玩具，也可以上弦后留在暗区。灯怕这里的风。";
                    break;
                case ResearchGuideStep.Moving:
                    goal.text = "帮助它走到右侧出口 · 可以随时调整帮助方式";
                    situation.text = "它正在往出口走。留意前方，也可以随时调整工具和帮助方式。";
                    destination = new Vector3(23f, -2.1f);
                    focus.text = "小人的目的地 →";
                    hint = "牵着时要等小人真正走出暗区；放手后，环境中的灯或玩具可以继续提供帮助。";
                    break;
                case ResearchGuideStep.BringReport:
                    goal.text = "小人到了 · 按住 E 拿报告，F 返回工作台";
                    situation.text = "小人到了！拿起新出现的报告，把这次的办法带回工作台。";
                    destination = chapter.Report.transform.position;
                    focus.text = "报告 · 按住 E 拿起\n旁边 F 可走回程通道";
                    hint = "出口附近按 F 会回到入口工作台；忘带报告也能沿已打开的路回来取。";
                    break;
                case ResearchGuideStep.ReturnReport:
                    goal.text = "把报告带回左侧工作台 · 靠近后松开 E";
                    situation.text = "把报告带回工作台，靠近后松开 E 放下。";
                    destination = new Vector3(1f, -1.9f);
                    focus.text = hand.transform.position.x > 21.5f ? "F · 带着报告回工作台" : "工作台 · 松开 E 放报告";
                    if (hand.transform.position.x > 21.5f) destination = new Vector3(23f, -2.1f);
                    hint = "出口附近的 F 是回程捷径。回到桌边再松手，报告才会留在工作台上。";
                    break;
                default:
                    goal.text = "报告已放好 · 在工作台按 F 继续";
                    situation.text = "准备好后，在工作台按 F 去汇报。";
                    destination = new Vector3(1f, -1.9f);
                    focus.text = "工作台 · F 继续";
                    hint = "这次选择的工具和陪伴方式，会带到最后的告别。";
                    break;
            }
            DrawTool();
            var show = expanded;
            HelpVisible = show;
            helpControl.text = show ? "H 收起" : "H 提示";
            helpPanel.gameObject.SetActive(show);
            help.gameObject.SetActive(show);
            var note = JourneyChoices.RepairedNoteDestination == "home" ? "回信上的图：开窗的风会让灯熄灭，关窗后灯会重新亮起来。" :
                JourneyChoices.RepairedNoteDestination == "notebook" ? "你留下的笔记：同一个箱子可以垫脚、压住机关，也可以搭落脚点。" : "左侧上层档案可以调查，那里有一块折叠板。";
            help.text = situation.text + "\n" + hint + "\n" + note;
        }

        private void DrawTool()
        {
            tool.gameObject.SetActive(false);
            reachable.enabled = false;
        }

        private void Place(Text label, Vector3 point, bool clamp)
        {
            if (Camera.main == null) return;
            var p = ui.ScreenPoint(point);
            if (clamp)
            {
                if (p.x > 505f) label.text += " →";
                else if (p.x < -505f) label.text = "← " + label.text;
                p.x = Mathf.Clamp(p.x, -465f, 465f);
                p.y = Mathf.Clamp(p.y, -230f, 210f);
            }
            label.rectTransform.anchoredPosition = p;
        }

        private void OnDestroy() { if (ui != null) Destroy(ui.gameObject); }
    }
}
