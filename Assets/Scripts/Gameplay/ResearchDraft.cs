using System.Collections.Generic;
using UnityEngine;

namespace LetGo
{
    /// <summary>A working draft can change; only releasing the conclusion makes it final.</summary>
    public sealed class ResearchDraft : MonoBehaviour
    {
        private HandConnection hand;
        private StorySceneDirector director;
        private HoldSocket question, evidence, conclusion;
        private HoldTarget photo, data;
        private LineRenderer destination, observation;
        private readonly LineRenderer[] models = new LineRenderer[2];
        private readonly SpriteRenderer[] walkers = new SpriteRenderer[2];
        private readonly SpriteRenderer[] doors = new SpriteRenderer[2];
        private readonly HashSet<string> observed = new();
        private float observationTime;
        private HoldTarget inspecting;
        public HoldTarget DraftEvidence => evidence == null ? null : evidence.Occupant;
        public bool IsCommitted => conclusion != null && conclusion.Completed;
        public float Observation01 => Mathf.Clamp01(observationTime / 1.1f);
        public System.Action<HoldTarget> EvidenceObserved;
        private const string ObservePrompt = "拿到灯下 · 停下来看看";
        private const string PlacePrompt = "松开 E · 放上工作板";
        private const string RevisePrompt = "还可以换一份 · 提交结论后定稿";
        private const string CommitPrompt = "松开 E · 带着缺口定稿";

        public void Initialize(StorySceneDirector sceneDirector, JourneyVisuals visuals)
        {
            director = sceneDirector;
            hand = director.Player.GetComponent<HandConnection>();
            question = EmotionalJourney.Named("Question Board")?.GetComponent<HoldSocket>();
            evidence = EmotionalJourney.Named("Evidence Board")?.GetComponent<HoldSocket>();
            conclusion = EmotionalJourney.Named("Conclusion Board")?.GetComponent<HoldSocket>();
            photo = EmotionalJourney.Target("evidence-photo");
            data = EmotionalJourney.Target("evidence-data");
            if (question == null || evidence == null || conclusion == null || photo == null || data == null)
            { Debug.LogError("[LetGo] Research is missing a board or evidence card."); enabled = false; return; }
            evidence.Replaceable = true;
            evidence.Acceptance = target => observed.Contains(target.TargetId);
            conclusion.Placed += target =>
            {
                evidence.Locked = true;
                // The unused possibility remains visible on the table, with its own gaps.
                var unused = evidence.Occupant == photo ? data : photo;
                unused.MarkUnavailable();
                var environment = FindAnyObjectByType<EmotionalEnvironment>();
                if (environment != null) environment.Resolution01 = 0.85f;
            };
            destination = visuals.Line("Draft destination", JourneyVisuals.Warm);
            observation = visuals.Line("Reading under the lamp", JourneyVisuals.Warm);
            foreach (var target in FindObjectsByType<HoldTarget>()) visuals.Track(target.transform, JourneyVisuals.Cool);
            for (var i = 0; i < 2; i++)
            {
                models[i] = visuals.Line(i == 0 ? "Observed path - with pauses" : "Measured path - direct", i == 0 ? JourneyVisuals.Warm : JourneyVisuals.Cool);
                walkers[i] = visuals.Prop("First step in draft " + i, Vector3.zero, new Vector2(0.2f, 0.32f), JourneyVisuals.Warm);
                doors[i] = visuals.Prop("Door in draft " + i, Vector3.zero, new Vector2(0.25f, 0.65f), JourneyVisuals.Cool);
            }
        }

        private void Update()
        {
            if (hand == null) return;
            director.ClearPrompt(ObservePrompt);
            director.ClearPrompt(PlacePrompt);
            director.ClearPrompt(RevisePrompt);
            director.ClearPrompt(CommitPrompt);
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
                    director.ShowPrompt(target.TargetId == "conclusion" ? CommitPrompt : PlacePrompt);
            }
            else if (evidence.Completed && !conclusion.Completed && underLamp) director.ShowPrompt(RevisePrompt);
            DrawModel(0, photo);
            DrawModel(1, data);
        }

        private void DrawModel(int index, HoldTarget card)
        {
            var visible = question.Completed;
            models[index].enabled = visible;
            walkers[index].enabled = visible;
            doors[index].enabled = visible;
            if (!visible) return;
            var known = observed.Contains(card.TargetId);
            var selected = evidence.Occupant == card;
            var center = evidence.transform.position + new Vector3(index == 0 ? -1.8f : 1.8f, 1.35f, 0);
            var color = index == 0 ? JourneyVisuals.Warm : JourneyVisuals.Cool;
            color.a = known ? (selected ? 1f : 0.5f) : 0.14f;
            models[index].startColor = models[index].endColor = color;
            models[index].positionCount = 18;
            for (var i = 0; i < 18; i++)
            {
                var t = i / 17f;
                // The observational route has resting places but stops short of the door.
                models[index].SetPosition(i, center + new Vector3(-1.1f + t * (index == 0 ? 1.6f : 2.2f),
                    index == 0 ? Mathf.Sin(t * Mathf.PI * 2) * 0.22f : 0, 0));
            }
            doors[index].transform.position = center + new Vector3(1.1f, 0.3f, 0);
            doors[index].color = color;
            var travel = Mathf.PingPong(Time.time * (index == 0 ? 0.24f : 0.4f), 1f);
            // A hesitation is kept in the direct model; neither route erases uncertainty.
            if (index == 1) travel = Mathf.Min(travel, 0.8f);
            walkers[index].transform.position = center + new Vector3(-1.1f + travel * (index == 0 ? 1.6f : 2.2f), 0.25f, 0);
            walkers[index].color = color;
        }
    }
}
