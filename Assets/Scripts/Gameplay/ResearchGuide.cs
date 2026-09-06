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
        private Text goal, situation, focus, tool, help, helpControl, lesson;
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
            lesson = ui.Label("What the learner remembers", new Vector2(-225, 275), new Vector2(720, 34), 18, TextAnchor.MiddleLeft);
            situation = ui.Label("What is happening", Vector2.zero, new Vector2(1, 1), 1);
            situation.gameObject.SetActive(false);
            helpControl = ui.Label("Optional hint key", new Vector2(535, 321), new Vector2(130, 34), 17);
            focus = ui.Label("Current destination", Vector2.zero, new Vector2(350, 65), 21);
            focus.gameObject.SetActive(false);
            tool = ui.Label("Nearby reusable object", Vector2.zero, new Vector2(290, 40), 20);
            tool.gameObject.SetActive(false);
            helpPanel = ui.Picture("Optional hint background", new Vector2(0, 177), new Vector2(1200, 152), null, new Color(0.025f, 0.04f, 0.055f, 0.93f));
            help = ui.Label("Available approaches", new Vector2(0, 177), new Vector2(1140, 136), 19, TextAnchor.MiddleLeft);
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
                    goal.text = chapter.HasDemonstration ? "它记住了 · 回到它身边，F 看它试走" : "它在等你 · T 示范，F 让它自己试走";
                    situation.text = chapter.HasDemonstration ? "它会在起点等你回来，再试试你教它的办法。" : "你可以替它铺好路，也可以让它学会你的办法。";
                    focus.text = chapter.HasDemonstration ? "F · 看它试走\nT · 重新示范   按住 E · 牵手" : "T · 走一遍给它看\nF · 试走   按住 E · 牵手";
                    hint = chapter.HasDemonstration ? "回到小人身边按 F，让它在你眼前试走；也可以先换好工具，再观察结果。T 可以重新示范。" : "T 开始示范：从缺口前起跳；到暗处前空手按住 E 稳住自己，或带着灯走。再按 T 收尾，回到它身边按 F 看它试走。";
                    break;
                case ResearchGuideStep.Bridge:
                    goal.text = "它停在缺口前 · 搭一条路，或回来教它跳";
                    situation.text = "小人停在缺口前：这里没有落脚点。";
                    destination = new Vector3(16f, -2.2f);
                    focus.text = "缺口 · 需要落脚点";
                    hint = "箱子和折叠板都能当落脚点。也可空手 Q 召回，在起点按 T，从缺口左边起跳并落到另一边给它看。";
                    break;
                case ResearchGuideStep.Comfort:
                    var blownOut = !chapter.LampWorking;
                    goal.text = blownOut ? "灯被风吹灭了 · F 关窗，或教它另一种办法" : chapter.HasDemonstration ? "它还在等帮助 · 让它看见光，或回来重新教" : "它停在暗处前 · 光或陪伴能让它前进";
                    situation.text = blownOut ? "灯被风吹灭了，小人退回了亮处。右侧窗户还开着。" : "小人不敢独自进入暗处，需要看得见或感到有人陪伴。";
                    destination = blownOut ? new Vector3(21f, -1.5f) : chapter.Learner.transform.position;
                    focus.text = blownOut ? "漏风的窗 · F 关窗" : "暗区前的小人 · E 可以牵住";
                    hint = "可以搬灯、牵手或留下上弦的玩具。也可 Q 召回再示范：在暗处前空手按住 E 约两秒，教它先稳住自己。";
                    break;
                case ResearchGuideStep.Moving:
                    goal.text = chapter.HasDemonstration ? "它在照着你做 · 留意它在哪里停下" : "它在试走 · 可调整工具，空手 Q 召回再试";
                    situation.text = "它正在往出口走。留意前方，也可以随时调整工具和帮助方式。";
                    destination = new Vector3(23f, -2.1f);
                    focus.text = "小人的目的地 →";
                    hint = chapter.HasDemonstration ? "跳过缺口、停下来安定自己、等待帮助，都会影响这次能走多远。空手 Q 召回，回到起点按 T 可以重新教。" : "牵着时要等它真正走出暗处。灯和玩具可以留下来，继续陪它走一段。";
                    break;
                case ResearchGuideStep.BringReport:
                    goal.text = "它到出口了 · 把报告带回工作台";
                    situation.text = "小人到了！拿起新出现的报告，把这次的办法带回工作台。";
                    destination = chapter.Report.transform.position;
                    focus.text = "报告 · 按住 E 拿起\n旁边 F 可走回程通道";
                    hint = "可以带报告回工作台。也可以空手按 Q，召回小人重新实验；它会记住上一轮走过的路。";
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
                    hint = "桌上的报告留下了这一次的办法。准备好了，就带着它继续往前。";
                    break;
            }
            if (chapter.State == LearnerState.Recalling)
            {
                goal.text = "它正在回到起点 · 可以重新摆放工具";
                situation.text = "走过的经验会保留，实验结果可以重新改变。";
            }
            if (chapter.State == LearnerState.Pausing)
            {
                goal.text = "它也停了下来 · 像你刚才那样";
                situation.text = "它记住了你的停顿，正在按自己的节奏继续。";
            }
            if (chapter.IsDemonstrating)
            {
                goal.text = "它在看你 · 走过缺口后，T 示范到这里";
                situation.text = "这次，让它看你怎样走过这段路。";
                hint = "从缺口左边起跳并落到另一边。在暗处前空手按住 E 约两秒，它会学着安定自己。Q 可以取消这次示范。";
            }
            DrawLesson();
            DrawTool();
            var show = expanded;
            HelpVisible = show;
            helpControl.text = show ? "H 收起" : "H 提示";
            helpPanel.gameObject.SetActive(show);
            help.gameObject.SetActive(show);
            var note = JourneyChoices.PaperBridgeIndependent ? "你折出的桥留了下来。折叠板在工作台右边，可以拿去做落脚点。" :
                JourneyChoices.RepairedNoteDestination == "home" ? "回信上的图：开窗的风会让灯熄灭，关窗后灯会重新亮起来。" :
                JourneyChoices.RepairedNoteDestination == "notebook" ? "你留下的笔记：同一个箱子可以垫脚、压住机关，也可以搭落脚点。" : "左侧上层档案可以调查，那里有一块折叠板。";
            help.text = situation.text + "\n" + hint + "\n" + note;
        }

        private void DrawLesson()
        {
            lesson.gameObject.SetActive(chapter.GateLatched && !chapter.ReportReturned && !chapter.IsDemonstrating);
            if (!chapter.HasDemonstration)
            {
                lesson.text = "T · 走一遍给它看";
                return;
            }
            var jump = false;
            var calm = false;
            var pause = false;
            var source = "";
            foreach (var action in chapter.DemonstratedActions)
            {
                jump |= action.CrossesGap;
                calm |= action.SteadiesInDark;
                pause |= action.Kind == LearnedHabitKind.Pause;
                if (action.Kind == LearnedHabitKind.SeekHelp) source = action.Help;
            }
            var words = jump ? "跳过缺口" : "寻找落脚点";
            words += calm ? " · 先稳住自己" : source == "lamp" ? " · 等待灯光" : source == "toy" ? " · 等待熟悉的声音" : pause ? " · 停一会儿" : " · 暗处仍需照应";
            lesson.text = "它记住了：" + words;
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
