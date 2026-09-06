using UnityEngine;
using UnityEngine.InputSystem;

namespace LetGo
{
    // Frame-based so every gameplay reader sees the same release, regardless of Update order.
    internal sealed class PauseInputLatch
    {
        private bool paused, pausedHeld, releasedWhilePaused, ignoreHeldUntilUp, sampled, sampledHeld;
        private int sampledFrame = -1;
        private int resumeBlockedThrough = -1;
        private int releaseFrame = -1;
        private int ignoredReleaseFrame = -1;

        public bool IsBlocked(int frame) => paused || frame <= resumeBlockedThrough;

        public void Begin(int frame, bool physicalHeld)
        {
            // Esc and E-up may arrive together. Preserve the hold the world actually saw last frame.
            pausedHeld = sampled && sampledFrame >= frame - 1 ? sampledHeld : physicalHeld;
            releasedWhilePaused = pausedHeld && !physicalHeld;
            paused = true;
            resumeBlockedThrough = releaseFrame = -1;
        }

        public void ObservePaused(bool physicalHeld)
        {
            if (paused && pausedHeld && !physicalHeld) releasedWhilePaused = true;
        }

        public void End(int frame, bool physicalHeld)
        {
            ObservePaused(physicalHeld);
            paused = false;
            resumeBlockedThrough = frame;
            releaseFrame = pausedHeld && releasedWhilePaused ? frame + 1 : -1;
            // A key pressed in the menu must not turn into a new world interaction on resume.
            ignoreHeldUntilUp = physicalHeld && (!pausedHeld || releasedWhilePaused);
        }

        public bool Held(int frame, bool physicalHeld)
        {
            if (IsBlocked(frame)) return pausedHeld;
            ObserveActive(frame, physicalHeld);
            var held = frame != releaseFrame && !ignoreHeldUntilUp && physicalHeld;
            sampled = true;
            sampledHeld = held;
            sampledFrame = frame;
            return held;
        }

        public bool Pressed(int frame, bool physicalHeld, bool physicalPressed)
        {
            if (IsBlocked(frame)) return false;
            ObserveActive(frame, physicalHeld);
            return frame != releaseFrame && !ignoreHeldUntilUp && physicalPressed;
        }

        public bool Released(int frame, bool physicalHeld, bool physicalReleased)
        {
            if (IsBlocked(frame)) return false;
            ObserveActive(frame, physicalHeld);
            return frame == releaseFrame || !ignoreHeldUntilUp && frame != ignoredReleaseFrame && physicalReleased;
        }

        private void ObserveActive(int frame, bool physicalHeld)
        {
            if (!ignoreHeldUntilUp || physicalHeld) return;
            ignoreHeldUntilUp = false;
            ignoredReleaseFrame = frame;
        }
    }

    public static class GameInput
    {
        private static PauseInputLatch pause = new PauseInputLatch();
        private static bool PhysicalInteractHeld => Keyboard.current != null && Keyboard.current.eKey.isPressed;
        public static bool IsPaused => pause.IsBlocked(Time.frameCount);
        public static void BeginPause() => pause.Begin(Time.frameCount, PhysicalInteractHeld);
        public static void ObservePauseInput() => pause.ObservePaused(PhysicalInteractHeld);
        public static void EndPause() => pause.End(Time.frameCount, PhysicalInteractHeld);
        public static void ClearPause() => pause = new PauseInputLatch();

        public static float Horizontal
        {
            get
            {
                var keyboard = Keyboard.current;
                if (IsPaused || keyboard == null) return 0f;
                var value = 0f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value += 1f;
                return value;
            }
        }

        public static bool JumpPressed => !IsPaused && Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame);

        public static bool InteractPressed => pause.Pressed(Time.frameCount, PhysicalInteractHeld,
            Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        public static bool UsePressed => !IsPaused && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        public static bool HelpPressed => !IsPaused && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
        public static bool ReconsiderPressed => !IsPaused && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        public static bool DemonstratePressed => !IsPaused && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
        public static int SelectionPressed => IsPaused || Keyboard.current == null ? 0 :
            (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame ? 1 : 0) -
            (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame ? 1 : 0);
        public static bool InteractHeld => pause.Held(Time.frameCount, PhysicalInteractHeld);
        public static bool InteractReleased => pause.Released(Time.frameCount, PhysicalInteractHeld,
            Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame);
        public static bool RestartPressed => !IsPaused && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool ConfirmPressed => !IsPaused && Keyboard.current != null &&
            (InteractPressed || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
    }
}
