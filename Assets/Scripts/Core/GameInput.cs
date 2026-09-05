using UnityEngine.InputSystem;

namespace LetGo
{
    public static class GameInput
    {
        public static float Horizontal
        {
            get
            {
                var keyboard = Keyboard.current;
                if (keyboard == null) return 0f;
                var value = 0f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value += 1f;
                return value;
            }
        }

        public static bool JumpPressed => Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame);

        public static bool InteractPressed => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool UsePressed => Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        public static bool InteractHeld => Keyboard.current != null && Keyboard.current.eKey.isPressed;
        public static bool InteractReleased => Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame;
        public static bool RestartPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool ConfirmPressed => Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
    }
}
