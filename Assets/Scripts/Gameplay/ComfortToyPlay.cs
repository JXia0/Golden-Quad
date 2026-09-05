using UnityEngine;

namespace LetGo
{
    /// <summary>A first shared game: wind, roll, receive the child's return, and roll back.</summary>
    [DefaultExecutionOrder(60)]
    public sealed class ComfortToyPlay : MonoBehaviour
    {
        private HoldTarget toy, child;
        private HoldSocket receiver;
        private PlayerController2D player;
        private StorySceneDirector director;
        private LineRenderer aim;
        private float speed;
        private float leftLimit, rightLimit;
        private bool playerThrow;
        private string activePrompt;
        public int Exchanges { get; private set; }
        public bool IsRolling => Mathf.Abs(speed) > 0.05f;
        public float Winding01 => toy == null || !toy.IsHeld ? 0f : Mathf.Clamp01(toy.HeldDuration / 1f);
        public System.Action ChildReturnedToy;
        public System.Action SharedPlayCompleted;

        public void Initialize(StorySceneDirector owner, HoldTarget item, HoldTarget companion, HoldSocket socket, JourneyVisuals visuals)
        {
            director = owner;
            player = owner.Player.GetComponent<PlayerController2D>();
            toy = item;
            child = companion;
            receiver = socket;
            leftLimit = toy.transform.position.x - 0.6f;
            rightLimit = child.transform.position.x + 2f;
            aim = visuals.GameplayLine("Toy rolling direction", JourneyVisuals.Warm);
            receiver.Acceptance = value => Exchanges >= 2;
            toy.Released += Roll;
        }

        private void Roll(HoldTarget value)
        {
            if (Exchanges >= 2) return;
            if (toy.IsPlaced) return;
            playerThrow = true;
            speed = player.FacingDirection * Mathf.Lerp(2f, 4.6f, Mathf.Clamp01(toy.HeldDuration));
        }

        private void Update()
        {
            if (toy == null || Exchanges >= 2) { if (aim != null) aim.enabled = false; return; }
            director.ClearPrompt(activePrompt);
            activePrompt = null;
            aim.enabled = toy.IsHeld;
            if (toy.IsPlaced) return;
            if (toy.IsHeld)
            {
                speed = 0f;
                aim.positionCount = 2;
                aim.SetPosition(0, toy.transform.position);
                aim.SetPosition(1, toy.transform.position + Vector3.right * player.FacingDirection * (0.6f + Winding01));
                activePrompt = "A / D · 朝向   按住 E 上弦 · 松开滚过去";
            }
            else if (IsRolling)
            {
                var position = toy.transform.position;
                var before = position.x;
                position.x += speed * Time.deltaTime;
                // Keep a missed throw in the play area; no lost item or reset is needed.
                if (position.x < leftLimit) { position.x = leftLimit; speed = Mathf.Abs(speed) * 0.5f; }
                if (position.x > rightLimit) { position.x = rightLimit; speed = -Mathf.Abs(speed) * 0.5f; }
                toy.transform.position = position;
                speed = Mathf.MoveTowards(speed, 0f, 1.3f * Time.deltaTime);
                var childX = child.transform.position.x;
                var crossesChild = Mathf.Min(before, position.x) <= childX + 0.4f && Mathf.Max(before, position.x) >= childX - 0.4f;
                if (playerThrow && crossesChild)
                {
                    Exchanges++;
                    playerThrow = false;
                    if (Exchanges == 1)
                    {
                        speed = -3.2f;
                        toy.transform.position = new Vector3(childX - 0.5f, position.y, position.z);
                        ChildReturnedToy?.Invoke();
                    }
                    else
                    {
                        speed = 0f;
                        toy.transform.position = child.transform.position;
                        HoldSocket.TryPlace(toy);
                        SharedPlayCompleted?.Invoke();
                    }
                }
            }
            if (!toy.IsHeld && !toy.IsPlaced && Exchanges == 1 && Mathf.Abs(player.transform.position.x - toy.transform.position.x) < 3f)
                activePrompt = "他把玩具滚回来了 · 按住 E 接住，再滚给他";
            if (activePrompt != null) director.ShowPrompt(activePrompt);
        }

        private void OnDestroy() { if (toy != null) toy.Released -= Roll; }
    }
}
