using UnityEngine;

namespace LetGo
{
    /// <summary>Roll to the child, or leave a reachable toy and let the child take the first step.</summary>
    [DefaultExecutionOrder(60)]
    public sealed class ComfortToyPlay : MonoBehaviour
    {
        private HoldTarget toy, child;
        private HoldSocket receiver;
        private PlayerController2D player;
        private HandConnection hand;
        private StorySceneDirector director;
        private LineRenderer aim;
        private float speed;
        private float leftLimit, rightLimit;
        private bool playerThrow;
        private bool practicing, previousReplaceable;
        private float steadySeconds, returnWait, pendingReturnSpeed;
        private string activePrompt;
        private readonly KindergartenPlayLesson lesson = new();
        public int Exchanges { get; private set; }
        public int IndependentRetrievals => lesson.IndependentRetrievals;
        public bool CanWalkTogether => lesson.Learned && !practicing;
        public bool ChildTrying => lesson.Trying;
        public bool IsPracticing => practicing;
        public bool ChildWinding => returnWait > 0f;
        public float CopiedRollSpeed => lesson.CopiedRollSpeed;
        public float CopiedWaitSeconds => lesson.CopiedWaitSeconds;
        public bool IsRolling => Mathf.Abs(speed) > 0.05f;
        public float Winding01 => toy == null || !toy.IsHeld ? 0f : Mathf.Clamp01(toy.HeldDuration / 1f);
        public float PredictedStopX => toy == null ? 0f : Mathf.Clamp(toy.transform.position.x +
            player.FacingDirection * KindergartenPlayLesson.RollDistance(toy.HeldDuration), leftLimit, rightLimit);
        public System.Action ChildReturnedToy;
        public System.Action SharedPlayCompleted;

        public void Initialize(StorySceneDirector owner, HoldTarget item, HoldTarget companion, HoldSocket socket, JourneyVisuals visuals)
        {
            director = owner;
            player = owner.Player.GetComponent<PlayerController2D>();
            hand = owner.Player.GetComponent<HandConnection>();
            toy = item;
            child = companion;
            receiver = socket;
            leftLimit = toy.transform.position.x - 0.6f;
            rightLimit = child.transform.position.x + 2f;
            aim = visuals.GameplayLine("Toy rolling direction", JourneyVisuals.Warm);
            receiver.Acceptance = value => Exchanges >= 2 && !practicing;
            toy.Released += Roll;
        }

        private void Roll(HoldTarget value)
        {
            if (Exchanges >= 2 && !practicing) return;
            if (toy.IsPlaced) return;
            playerThrow = true;
            lesson.CancelAttempt();
            lesson.ObserveRoll(toy.HeldDuration, steadySeconds);
            steadySeconds = returnWait = 0f;
            speed = player.FacingDirection * KindergartenPlayLesson.RollSpeed(toy.HeldDuration);
        }

        private void Update()
        {
            if (GameInput.IsPaused) return;
            director.ClearPrompt(activePrompt);
            activePrompt = null;
            if (toy == null) return;
            if (practicing && (child.IsHeld || child.IsPlaced ||
                (!toy.IsHeld && Mathf.Abs(player.transform.position.x - child.transform.position.x) > 5.5f))) EndPractice();
            if (Exchanges >= 2 && !practicing)
            {
                if (aim != null) aim.enabled = false;
                if (!child.IsHeld && !child.IsPlaced && toy.IsPlaced && hand.CurrentTarget == null &&
                    Mathf.Abs(player.transform.position.x - child.transform.position.x) < 2.8f)
                {
                    activePrompt = "按住 E · 牵着走　F · 再玩一会儿";
                    director.ShowPrompt(activePrompt);
                    if (GameInput.UsePressed) BeginPractice();
                }
                if (!practicing) return;
            }
            aim.enabled = toy.IsHeld;
            if (toy.IsPlaced) return;
            if (toy.IsHeld)
            {
                speed = 0f;
                returnWait = 0f;
                steadySeconds = player.Velocity.sqrMagnitude < 0.3f ? steadySeconds + Time.deltaTime : 0f;
                lesson.CancelAttempt();
                aim.positionCount = 2;
                aim.SetPosition(0, toy.transform.position);
                aim.SetPosition(1, new Vector3(PredictedStopX, toy.transform.position.y, toy.transform.position.z));
                activePrompt = "A / D · 朝向他　松开 E · 滚出\n短按轻滚，多按一会儿用力滚";
            }
            else if (returnWait > 0f)
            {
                returnWait = Mathf.Max(0f, returnWait - Time.deltaTime);
                if (returnWait <= 0f) speed = pendingReturnSpeed;
                activePrompt = "他也停了一下 · 正学着你的样子";
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
                speed = Mathf.MoveTowards(speed, 0f, KindergartenPlayLesson.RollingFriction * Time.deltaTime);
                var childX = child.transform.position.x;
                var crossesChild = Mathf.Min(before, position.x) <= childX + 0.4f && Mathf.Max(before, position.x) >= childX - 0.4f;
                if (playerThrow && crossesChild)
                {
                    if (!practicing) Exchanges++;
                    playerThrow = false;
                    if (Exchanges == 1 || practicing)
                    {
                        StartChildReturn();
                    }
                    else
                    {
                        FinishSharedPlay(false);
                    }
                }
            }
            else if (!practicing && (playerThrow || Exchanges == 1))
            {
                var childX = child.transform.position.x;
                var completed = lesson.TickRetrieval(Time.deltaTime, ref childX, toy.transform.position.x,
                    player.transform.position.x, hand.CurrentTarget == null, player.Velocity.sqrMagnitude < 0.3f);
                child.transform.position = new Vector3(childX, child.transform.position.y, child.transform.position.z);
                if (completed) FinishSharedPlay(true);
                else if (Mathf.Abs(childX - toy.transform.position.x) <= 3.2f)
                {
                    if (hand.CurrentTarget != null)
                        activePrompt = "松开 E · 空着手，陪他试一试";
                    else if (Mathf.Abs(player.transform.position.x - childX) > 5.5f)
                        activePrompt = "离他太远了 · 回到他身边，等他来拿";
                    else if (Mathf.Abs(player.transform.position.x - toy.transform.position.x) <= 0.3f)
                        activePrompt = "让出一步 · 给他留地方拿玩具";
                    else if (player.Velocity.sqrMagnitude >= 0.3f)
                        activePrompt = "停下来 · 等他自己来拿";
                    else
                        activePrompt = lesson.Trying ? "他迈出了第一步 · 停下来等他" : "先别替他捡 · 空着手，等一等";
                }
                else activePrompt = "离他有点远 · 拾起玩具，再试一次";
            }
            else if (practicing) activePrompt = "按住 E · 捡起来，换个力度给他看\n也可以牵起他的手，一起走";
            if (!toy.IsHeld && !toy.IsPlaced && (Exchanges == 1 || practicing) && !playerThrow && IsRolling &&
                Mathf.Abs(player.transform.position.x - toy.transform.position.x) < 3f)
                activePrompt = lesson.CopiedRollSpeed > 3.8f
                    ? "他也用了很大的力气 · 按住 E 接住\n可以靠近一点，轻轻滚给他看"
                    : "他学着轻轻滚了回来 · 按住 E 接住，再滚给他";
            if (activePrompt != null) director.ShowPrompt(activePrompt);
        }

        private void FinishSharedPlay(bool independent)
        {
            // An actual independent retrieval replaces the second exchange; it never adds an errand.
            Exchanges = 2;
            speed = returnWait = 0f;
            playerThrow = false;
            toy.transform.position = child.transform.position;
            JourneyChoices.RememberKindergartenPlay(independent);
            HoldSocket.TryPlace(toy);
            SharedPlayCompleted?.Invoke();
        }

        private void StartChildReturn()
        {
            playerThrow = false;
            speed = 0f;
            var direction = Mathf.Sign(player.transform.position.x - child.transform.position.x);
            if (direction == 0f) direction = -1f;
            pendingReturnSpeed = direction * lesson.CopiedRollSpeed;
            returnWait = lesson.CopiedWaitSeconds;
            toy.transform.position = new Vector3(child.transform.position.x + direction * 0.5f,
                toy.transform.position.y, toy.transform.position.z);
            ChildReturnedToy?.Invoke();
        }

        private void BeginPractice()
        {
            // Optional extra play keeps the earned objective and never replaces the walk onward.
            practicing = true;
            rightLimit = Mathf.Max(rightLimit, child.transform.position.x + 2f);
            leftLimit = Mathf.Min(leftLimit, child.transform.position.x - 3.2f);
            previousReplaceable = receiver.Replaceable;
            receiver.Replaceable = true;
            HoldSocket.Reclaim(toy);
            toy.ReopenInteraction();
            toy.transform.SetParent(null, true);
            toy.AllowReclaim = false;
            StartChildReturn();
        }

        private void EndPractice()
        {
            practicing = false;
            speed = returnWait = steadySeconds = 0f;
            playerThrow = false;
            toy.transform.position = child.transform.position;
            HoldSocket.TryPlace(toy);
            receiver.Replaceable = previousReplaceable || child.IsPlaced;
        }

        private void OnDestroy()
        {
            if (toy != null) toy.Released -= Roll;
            if (director != null) director.ClearPrompt(activePrompt);
        }
    }
}
