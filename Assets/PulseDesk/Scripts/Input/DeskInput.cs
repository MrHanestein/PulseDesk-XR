using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PulseDesk
{
    public enum DeskKey { Left, Right, Up, Down, Space, B, R }

    /// <summary>
    /// Mouse and keyboard for testing in the Editor without a headset.
    /// Works with either the new Input System or the legacy Input Manager. Never required on device.
    /// </summary>
    public static class DeskInput
    {
        public static bool HasMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePresent;
#else
                return false;
#endif
            }
        }

        public static bool MouseHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(0);
#else
                return false;
#endif
            }
        }

        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }

        public static bool KeyDown(DeskKey key)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case DeskKey.Left: return k.leftArrowKey.wasPressedThisFrame;
                case DeskKey.Right: return k.rightArrowKey.wasPressedThisFrame;
                case DeskKey.Up: return k.upArrowKey.wasPressedThisFrame;
                case DeskKey.Down: return k.downArrowKey.wasPressedThisFrame;
                case DeskKey.Space: return k.spaceKey.wasPressedThisFrame;
                case DeskKey.B: return k.bKey.wasPressedThisFrame;
                case DeskKey.R: return k.rKey.wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (key)
            {
                case DeskKey.Left: return Input.GetKeyDown(KeyCode.LeftArrow);
                case DeskKey.Right: return Input.GetKeyDown(KeyCode.RightArrow);
                case DeskKey.Up: return Input.GetKeyDown(KeyCode.UpArrow);
                case DeskKey.Down: return Input.GetKeyDown(KeyCode.DownArrow);
                case DeskKey.Space: return Input.GetKeyDown(KeyCode.Space);
                case DeskKey.B: return Input.GetKeyDown(KeyCode.B);
                case DeskKey.R: return Input.GetKeyDown(KeyCode.R);
            }
            return false;
#else
            return false;
#endif
        }
    }
}
