using System.Collections.Generic;
using UnityEngine;

namespace LetGo
{
    /// <summary>Observe, revise, try the selected model, then commit what actually worked.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class ResearchDraft : MonoBehaviour
    {
        private HandConnection hand;
        private StorySceneDirector director;
        private HoldSocket question, evidence, conclusion;
        private HoldTarget photo, data;
        private LineRenderer destination, observation;
        private LineRenderer trialPath, trialResponse;
        private Transform trialFigure;
        private HoldTarget bridge;
        private readonly LineRenderer[] gaps = new LineRenderer[2];
        private readonly ResearchTrial trial = new();
        private ResearchTrialState previousTrialState;
        private PlayerController2D player;
        private string trialPrompt;
        private readonly HashSet<string> observed = new();
        private float observationTime;
        private HoldTarget inspecting;
        public HoldTarget DraftEvidence => evidence == null ? null : evidence.Occupant;
        public bool IsCommitted => conclusion != null && conclusion.Completed;
        public float Observation01 => Mathf.Clamp01(observationTime / 1.1f);
        public System.Action<HoldTarget> EvidenceObserved;
        public ResearchTrialState TrialState => trial.State;
        public float TrialProgress01 => trial.Progress01;
        public float TrialResponse01 => trial.Response01;
        public Transform TrialFigure => trialFigure;
        public HoldTarget TrialBridge => bridge;
        public bool TrialNeedsBridge => trial.WaitingForBridge;
        public float RequiredBridgeX => trial.RequiredBridgeX;
        public System.Action<ResearchTrialState> TrialStateChanged;
        private const string ObservePrompt = "拿到灯下 · 停下来看看";
        private const string PlacePrompt = "松开 E · 放上工作板";
        private const string RevisePrompt = "还可以换一份 · 提交结论后定稿";
        private const string CommitPrompt = "松开 E · 带着缺口定稿";
        private const string PendingCommitPrompt = "先让模型走到终点 · 可以暂时放下结论";

        public void Initialize(StorySceneDirector sceneDirector, JourneyVisuals visuals)
        {
            director = sceneDirector;
            hand = director.Player.GetComponent<HandConnection>();
            player = director.Player.GetComponent<PlayerController2D>();
            question = EmotionalJourney.Named("Question Board")?.GetComponent<HoldSocket>();
            evidence = EmotionalJourney.Named("Evidence Board")?.GetComponent<HoldSocket>();
            conclusion = EmotionalJourney.Named("Conclusion Board")?.GetComponent<HoldSocket>();
            photo = EmotionalJourney.Target("evidence-photo");
            data = EmotionalJourney.Target("evidence-data");
            if (question == null || evidence == null || conclusion == null || photo == null || data == null)
            { Debug.LogError("[LetGo] Research is missing a board or evidence card."); enabled = false; return; }
            evidence.Replaceable = true;
            evidence.Acceptance = target => observed.Contains(target.TargetId);
            evidence.Placed += BeginTrial;
            conclusion.Acceptance = target => trial.State == ResearchTrialState.Arrived;
            conclusion.Placed += target =>
            {
                evidence.Locked = true;
                JourneyChoices.RememberTest(evidence.Occupant.TargetId);
                // The unused possibility remains visible on the table, with its own gaps.
                var unused = evidence.Occupant == photo ? data : photo;
                unused.MarkUnavailable();
                var environment = FindAnyObjectByType<EmotionalEnvironment>();
                if (environment != null) environment.Resolution01 = 0.85f;
            };
            destination = visuals.GameplayLine("Draft destination", JourneyVisuals.Warm);
            observation = visuals.GameplayLine("Reading under the lamp", JourneyVisuals.Warm);
            foreach (var target in FindObjectsByType<HoldTarget>()) visuals.Track(target.transform, JourneyVisuals.Cool);
            trialPath = visuals.GameplayLine("Selected experiment route", JourneyVisuals.Cool);
            trialResponse = visuals.GameplayLine("Experiment response", JourneyVisuals.Warm);
            // This is a gameplay actor. The art slot can replace its placeholder independently of guides.
            trialFigure = visuals.Prop("Research Trial Figure", evidence.transform.position,
                new Vector2(0.42f, 0.65f), JourneyVisuals.Warm, "prop_research_model").transform;
            trialFigure.gameObject.SetActive(false);
            var bridgeVisual = visuals.Prop("Movable Experiment Bridge", evidence.transform.position + new Vector3(0.2f, -1.2f),
                new Vector2(1.4f, 0.24f), JourneyVisuals.Warm, "prop_research_bridge");
            bridge = bridgeVisual.gameObject.AddComponent<HoldTarget>();
            bridge.Configure("experiment-bridge", HoldTargetMode.Carryable, "按住 E · 搬动支撑板", 1.5f, 3f, 2);
            bridge.Released += value =>
            {
                var position = value.transform.position;
                var gap = Mathf.Abs(position.x - trial.FirstGapX) < Mathf.Abs(position.x - trial.SecondGapX) ? trial.FirstGapX : trial.SecondGapX;
                if (Mathf.Abs(position.x - gap) <= 1.1f) position.x = gap;
                value.transform.position = position;
            };
            visuals.Track(bridge.transform, JourneyVisuals.Warm);
            for (var i = 0; i < gaps.Length; i++) gaps[i] = visuals.GameplayLine("Experiment gap " + i, JourneyVisuals.Cool, 0.08f);
        }

        private void BeginTrial(HoldTarget card)
        {
            trial.Begin(evidence.transform.position.x + 1.2f, conclusion.transform.position.x - 1.4f, card == photo);
            trialFigure.gameObject.SetActive(true);
            trialFigure.position = new Vector3(trial.PositionX, -2.35f, evidence.transform.position.z);
            NotifyTrialState();
        }

        private void Update()
        {
            if (hand == null) return;
            director.ClearPrompt(ObservePrompt);
            director.ClearPrompt(PlacePrompt);
            director.ClearPrompt(RevisePrompt);
            director.ClearPrompt(CommitPrompt);
            director.ClearPrompt(PendingCommitPrompt);
            director.ClearPrompt(trialPrompt);
            trialPrompt = null;
            var target = hand.CurrentTarget;
            var isEvidence = target != null && (target == photo || target == data);
            if (target != inspecting) { inspecting = target; observationTime = 0f; }
            var underLamp = Vector2.Distance(hand.transform.position, evidence.transform.position) < 2.4f;
            observation.enabled = isEvidence && !observed.Contains(target.TargetId);
            if (observation.enabled)
            {
                if (underLamp && director.Player.GetComponent<PlayerController2D>().Velocity.sqrMagnitude < 0.12f)
                    observationTime += Time.deltaTime;
                JourneyVisuals.Ring(observation, target.transform.position, 0.65f, Mathf.Max(0.04f, observationTime / 1.1f));
                director.ShowPrompt(ObservePrompt);
                if (observationTime >= 1.1f)
                {
                    observed.Add(target.TargetId);
                    EvidenceObserved?.Invoke(target);
                    SceneAudio.Instance?.PlayItemMove();
                }
            }
            Transform receiver = null;
            if (target != null)
            {
                if (target.TargetId == "question") receiver = question.transform;
                else if (isEvidence) receiver = evidence.transform;
                else if (target.TargetId == "conclusion") receiver = conclusion.transform;
            }
            destination.enabled = receiver != null;
            if (receiver != null)
            {
                JourneyVisuals.Ring(destination, receiver.position, 0.9f + Mathf.Sin(Time.time * 2) * 0.04f);
                if ((!isEvidence || observed.Contains(target.TargetId)) && Vector2.Distance(target.transform.position, receiver.position) < 1.7f)
                    director.ShowPrompt(target.TargetId == "conclusion" ?
                        trial.State == ResearchTrialState.Arrived ? CommitPrompt : PendingCommitPrompt : PlacePrompt);
            }
            else if (evidence.Completed && !conclusion.Completed && underLamp) director.ShowPrompt(RevisePrompt);
            UpdateTrial();
        }

        private void UpdateTrial()
        {
            if (trial.State == ResearchTrialState.NotStarted) return;
            if (player.ControlsEnabled)
                trial.Tick(Time.deltaTime, player.transform.position.x, Mathf.Abs(player.Velocity.x) < 0.15f,
                    hand.CurrentTarget == null && !GameInput.InteractHeld, bridge.transform.position.x, !bridge.IsHeld);
            trialFigure.position = new Vector3(trial.PositionX, trialFigure.position.y, trialFigure.position.z);
            NotifyTrialState();
            trialPath.positionCount = 3;
            trialPath.SetPosition(0, new Vector3(evidence.transform.position.x + 1.2f, trialFigure.position.y - 0.3f));
            trialPath.SetPosition(1, new Vector3(conclusion.transform.position.x - 1.4f, trialFigure.position.y - 0.3f));
            trialPath.SetPosition(2, new Vector3(conclusion.transform.position.x - 1.4f, trialFigure.position.y + 0.5f));
            for (var i = 0; i < gaps.Length; i++)
            {
                var x = i == 0 ? trial.FirstGapX : trial.SecondGapX;
                gaps[i].positionCount = 4;
                gaps[i].SetPosition(0, new Vector3(x - 0.45f, -2.65f));
                gaps[i].SetPosition(1, new Vector3(x - 0.45f, -2.95f));
                gaps[i].SetPosition(2, new Vector3(x + 0.45f, -2.95f));
                gaps[i].SetPosition(3, new Vector3(x + 0.45f, -2.65f));
            }
            var waiting = trial.State == ResearchTrialState.NeedsCompany || trial.State == ResearchTrialState.NeedsSpace;
            trialResponse.enabled = waiting;
            if (waiting) JourneyVisuals.Ring(trialResponse, trialFigure.position, 0.6f, Mathf.Max(0.04f, trial.Response01));
            if (!IsCommitted && (hand.CurrentTarget == null || hand.CurrentTarget == bridge) && Mathf.Abs(player.transform.position.x - trial.PositionX) < 5f)
            {
                trialPrompt = trial.WaitingForBridge ? "把支撑板放在缺口上 · 走过去后，还能搬到下一处" : trial.State switch
                {
                    ResearchTrialState.NeedsCompany => "停在它身旁 · 空着手，陪它等一会儿",
                    ResearchTrialState.NeedsSpace => "空出双手 · 向右走，给它迈步的空间",
                    ResearchTrialState.Arrived => "走到了 · 可以带结论去定稿，也可以换个方案再试",
                    _ => "跟着模型 · 看它在哪一步停下"
                };
                director.ShowPrompt(trialPrompt);
            }
        }

        private void NotifyTrialState()
        {
            if (previousTrialState == trial.State) return;
            previousTrialState = trial.State;
            TrialStateChanged?.Invoke(trial.State);
        }
    }
}
