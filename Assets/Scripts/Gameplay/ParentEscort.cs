using UnityEngine;

namespace LetGo
{
    /// <summary>The adult offers the first steps. Letting go returns movement to the child.</summary>
    [DefaultExecutionOrder(-20)]
    public sealed class ParentEscort : MonoBehaviour
    {
        private PlayerController2D player;
        private HandConnection hand;
        private HoldTarget parent;
        private StorySceneDirector director;
        private float doorwayX;
        private bool heldLast;
        private string prompt;
        public bool IsLeading { get; private set; }
        public bool WaitingAtDoor { get; private set; }
        public int ReturnsToHand { get; private set; }
        public bool HasBeenSupported { get; private set; }

        public void Initialize(StorySceneDirector owner)
        {
            director = owner;
            player = owner.Player.GetComponent<PlayerController2D>();
            hand = owner.Player.GetComponent<HandConnection>();
            parent = EmotionalJourney.Target("parent");
            var gate = FindAnyObjectByType<ReleaseGate>();
            if (parent == null || gate == null) { enabled = false; return; }
            doorwayX = gate.transform.position.x - 0.3f;
            parent.ExternallyGuided = true;
            parent.Configure("parent", HoldTargetMode.Companion, "按住 E · 牵住父母的手", 2.1f, 2.3f);
        }

        private void FixedUpdate()
        {
            if (player == null || parent == null) return;
            var held = player.ControlsEnabled && hand.CurrentTarget == parent;
            if (held && !heldLast)
            {
                if (HasBeenSupported) ReturnsToHand++;
                HasBeenSupported = true;
            }
            heldLast = held;
            IsLeading = held && !WaitingAtDoor;
            player.AssistedHorizontal = null;
            if (!held)
            {
                // Returning to the hand is allowed without erasing any progress.
                return;
            }
            var position = parent.transform.position;
            if (GameInput.Horizontal < -0.1f)
            {
                position.x = Mathf.MoveTowards(position.x, player.transform.position.x + 0.8f, 3.8f * Time.fixedDeltaTime);
                player.AssistedHorizontal = -0.75f;
                WaitingAtDoor = false;
            }
            else
            {
                // The adult stays close instead of stretching the hand and dragging the child.
                var destination = Mathf.Min(doorwayX, player.transform.position.x + 1.15f);
                position.x = Mathf.MoveTowards(position.x, destination, 2.3f * Time.fixedDeltaTime);
                var following = Mathf.Clamp((position.x - 0.8f - player.transform.position.x) * 2f, 0f, 0.7f);
                player.AssistedHorizontal = following;
                WaitingAtDoor = position.x >= doorwayX - 0.03f && following < 0.06f;
            }
            parent.transform.position = position;
            IsLeading = !WaitingAtDoor;
        }

        private void LateUpdate()
        {
            if (parent == null) return;
            if (prompt != null) director.ClearPrompt(prompt);
            prompt = null;
            if (hand.CurrentTarget == parent)
                prompt = WaitingAtDoor ? "到门口了。\n松开 E · 自己走进去" : "松开 E · 自己走　A · 回头";
            else if (HasBeenSupported && director.CompletedObjectives == 0 &&
                     Mathf.Abs(player.transform.position.x - parent.transform.position.x) < 2.1f)
                prompt = "D · 往前走　按住 E · 再牵一会儿";
            if (prompt != null) director.ShowPrompt(prompt);
        }

        private void OnDisable()
        {
            if (player != null) player.AssistedHorizontal = null;
            if (parent != null) parent.ExternallyGuided = false;
            if (prompt != null && director != null) director.ClearPrompt(prompt);
        }
    }
}
