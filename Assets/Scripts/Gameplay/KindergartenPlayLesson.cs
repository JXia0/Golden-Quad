using UnityEngine;

namespace LetGo
{
    /// <summary>The child tries a reachable toy when the player leaves room for the attempt.</summary>
    public sealed class KindergartenPlayLesson
    {
        public const float RollingFriction = 1.3f;
        private float watchingSeconds;
        private float tryingSeconds;
        private float firstStepX;
        public bool Trying { get; private set; }
        public bool Learned { get; private set; }
        public int IndependentRetrievals { get; private set; }
        public int Demonstrations { get; private set; }
        public float CopiedRollSpeed { get; private set; } = 2.2f;
        public float CopiedWaitSeconds { get; private set; } = 0.15f;

        public void ObserveRoll(float heldSeconds, float steadySeconds)
        {
            Demonstrations++;
            CopiedRollSpeed = RollSpeed(heldSeconds);
            // The child repeats the demonstrated pause as well as its force.
            CopiedWaitSeconds = Mathf.Lerp(0.08f, 0.6f, Mathf.Clamp01(steadySeconds / 0.8f));
        }

        public static float RollSpeed(float heldSeconds) => Mathf.Lerp(1f, 4.6f, Mathf.Clamp01(heldSeconds));
        public static float RollDistance(float heldSeconds)
        {
            var speed = RollSpeed(heldSeconds);
            return speed * speed / (2f * RollingFriction);
        }

        public void CancelAttempt()
        {
            watchingSeconds = tryingSeconds = 0f;
            Trying = false;
        }

        public bool TickRetrieval(float dt, ref float childX, float toyX, float playerX, bool playerEmptyHanded, bool playerStill)
        {
            if (Learned || dt <= 0f) return false;
            var distance = Mathf.Abs(childX - toyX);
            var hasRoom = Mathf.Abs(playerX - toyX) > 0.3f;
            var supported = playerEmptyHanded && playerStill && Mathf.Abs(playerX - childX) <= 5.5f;
            if (!Trying)
            {
                if (!supported || !hasRoom || distance < 0.4f || distance > 3.2f)
                {
                    watchingSeconds = 0f;
                    return false;
                }
                watchingSeconds += dt;
                if (watchingSeconds < 0.25f) return false;
                Trying = true;
                firstStepX = childX;
            }
            // A rushed attempt pauses in place. Coming back and waiting resumes it.
            if (!supported || !hasRoom) return false;
            tryingSeconds += dt;
            var stepSpeed = tryingSeconds < 0.3f ? 0.65f : 1.5f;
            childX = Mathf.MoveTowards(childX, toyX, stepSpeed * dt);
            if (Mathf.Abs(childX - toyX) > 0.12f || Mathf.Abs(childX - firstStepX) < 0.25f) return false;
            IndependentRetrievals++;
            Learned = true;
            Trying = false;
            return true;
        }
    }
}
