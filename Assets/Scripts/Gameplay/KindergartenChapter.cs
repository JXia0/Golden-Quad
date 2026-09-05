using UnityEngine;

namespace LetGo
{
    public sealed class KindergartenChapter : MonoBehaviour
    {
        private HandConnection hand;
        private HoldTarget child;
        private HoldSocket teacher;
        private LineRenderer childBreath;
        private LineRenderer destination;
        private StorySceneDirector director;
        private Transform chair;
        private HoldTarget bag;
        private HoldTarget toy;
        private const string WaitPrompt = "靠近一点 · 陪他等一会儿";
        private const string PlacePrompt = "松开 E · 留在这里";

        public void Initialize(StorySceneDirector sceneDirector, JourneyVisuals visuals)
        {
            director = sceneDirector;
            hand = director.Player.GetComponent<HandConnection>();
            child = EmotionalJourney.Target("crying-child");
            chair = EmotionalJourney.Named("Oversized Chair");
            var gate = FindAnyObjectByType<ReleaseGate>();
            var oldToy = FindAnyObjectByType<CarryItem>();
            teacher = EmotionalJourney.Named("Teacher Safe Area")?.GetComponent<HoldSocket>();
            if (child == null || chair == null || gate == null || oldToy == null || teacher == null)
            { Debug.LogError("[LetGo] Kindergarten chapter is missing a required scene object."); enabled = false; return; }
            var chairTask = chair.GetComponent<ObjectiveStation>();
            if (chairTask != null) chairTask.enabled = false;
            foreach (var inspect in FindObjectsByType<InspectPoint>()) inspect.enabled = false;
            var oldDelivery = child.GetComponent<DeliveryStation>();
            if (oldDelivery != null) oldDelivery.enabled = false;
            oldToy.enabled = false;

            var bagVisual = visuals.Prop("Bag from home", gate.transform.position + new Vector3(0.85f, 0, 0),
                new Vector2(0.55f, 0.7f), JourneyVisuals.Warm, "prop_child_backpack");
            bag = bagVisual.gameObject.AddComponent<HoldTarget>();
            bag.Configure("home-bag", HoldTargetMode.Carryable, "按住 E · 抱起书包", 1.8f, 3f, 1);
            var bagSocket = chair.gameObject.AddComponent<HoldSocket>();
            bagSocket.Configure("home-bag", "", 1.8f, 1, "");
            bagSocket.Placed += value => value.transform.position = chair.position + Vector3.up * 0.6f;
            toy = oldToy.gameObject.AddComponent<HoldTarget>();
            toy.Configure("comfort-toy", HoldTargetMode.Carryable, "按住 E · 拾起玩具", 1.8f, 3f, 2);
            var toySocket = child.gameObject.AddComponent<HoldSocket>();
            toySocket.Configure("comfort-toy", "", 1.8f, 2, "");
            toySocket.Placed += value =>
            {
                value.transform.SetParent(child.transform, true);
                value.transform.position = child.transform.position + new Vector3(0.35f, -0.15f, 0);
                if (teacher.Completed) value.AllowReclaim = true;
                JourneyChoices.Record("kindergarten", "shared-comfort", false);
            };
            gameObject.AddComponent<ComfortToyPlay>().Initialize(director, toy, child, toySocket, visuals);
            child.GentleCompanion = true;
            teacher.Configure("crying-child", "", 1.8f, 3, "");
            teacher.Placed += value =>
            {
                value.WalkAway(teacher.transform.position + Vector3.right * 1.4f);
                toy.AllowReclaim = true;
                toySocket.Replaceable = true;
                toy.DropFloorY = -2.35f;
                toy.Configure("comfort-toy", HoldTargetMode.Carryable, "E · 取回熟悉的玩具，也可以留给他", 1.5f, 3f);
                var environment = FindAnyObjectByType<EmotionalEnvironment>();
                if (environment != null) environment.Resolution01 = 1f;
            };
            visuals.Track(bag.transform, JourneyVisuals.Warm);
            visuals.Track(toy.transform, JourneyVisuals.Warm);
            visuals.Track(child.transform, JourneyVisuals.Warm);
            childBreath = visuals.Line("Child needs time", JourneyVisuals.Warm);
            destination = visuals.Line("A place for what you carry", JourneyVisuals.Warm);
        }

        private void Update()
        {
            if (hand == null || child == null) return;
            childBreath.enabled = child.IsWaiting && hand.CurrentTarget == child;
            JourneyVisuals.Ring(childBreath, child.transform.position + Vector3.up * 0.3f,
                Mathf.Lerp(0.22f, 0.65f, child.Reassurance01), Mathf.Max(0.08f, child.Reassurance01));
            if (childBreath.enabled) director.ShowPrompt(WaitPrompt);
            else director.ClearPrompt(WaitPrompt);
            Transform receiver = null;
            if (hand.CurrentTarget == bag) receiver = chair;
            else if (hand.CurrentTarget == toy && !teacher.Completed) receiver = child.transform;
            else if (hand.CurrentTarget == child) receiver = teacher.transform;
            destination.enabled = receiver != null;
            if (receiver != null)
            {
                JourneyVisuals.Ring(destination, receiver.position, 0.8f + Mathf.Sin(Time.time * 2) * 0.06f);
                // Socket acceptance is measured from the held object, including the trailing child.
                if (Vector2.Distance(hand.CurrentTarget.transform.position, receiver.position) < 1.7f)
                    director.ShowPrompt(PlacePrompt);
                else director.ClearPrompt(PlacePrompt);
            }
            else director.ClearPrompt(PlacePrompt);
        }
    }
}
