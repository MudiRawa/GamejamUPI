using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VertigoShift.Input
{
    /// <summary>
    /// Input abstraction for the prototype.
    /// The project is configured for the new Input System (Active Input Handling = "Input System Package (New)"),
    /// but the legacy path is kept behind #if so the prototype survives a settings switch.
    /// </summary>
    public static class ProtoInput
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard Kb { get { return Keyboard.current; } }
        static Mouse Ms { get { return Mouse.current; } }
#endif

        /// <summary>x = strafe (A/D), y = forward (W/S).</summary>
        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                if (k == null) return Vector2.zero;
                float x = 0f, y = 0f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
                return new Vector2(x, y);
#else
                return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
            }
        }

        /// <summary>Mouse delta in pixels (x = right, y = up).</summary>
        public static Vector2 Look
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var m = Ms;
                if (m == null) return Vector2.zero;
                return m.delta.ReadValue();
#else
                return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
            }
        }

        public static bool FireHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var m = Ms;
                return m != null && m.leftButton.isPressed;
#else
                return Input.GetMouseButton(0);
#endif
            }
        }

        public static bool FirePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var m = Ms;
                return m != null && m.leftButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }

        public static bool JumpPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                return k != null && k.spaceKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Space);
#endif
            }
        }

        public static bool CrouchHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                if (k == null) return false;
                return k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed || k.cKey.isPressed;
#else
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.C);
#endif
            }
        }

        /// <summary>True on the frame the crouch button was released.</summary>
        public static bool CrouchReleased
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                if (k == null) return false;
                return k.leftCtrlKey.wasReleasedThisFrame || k.rightCtrlKey.wasReleasedThisFrame || k.cKey.wasReleasedThisFrame;
#else
                return Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl) || Input.GetKeyUp(KeyCode.C);
#endif
            }
        }

        public static bool RunHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                if (k == null) return false;
                return k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
#else
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
            }
        }

        public static bool ReloadPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                return k != null && k.rKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
            }
        }

        public static bool EscapePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                return k != null && k.escapeKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        /// <summary>Top row number keys 1..4 (used for the 4 sided gravity shift).</summary>
        public static bool DigitPressed(int digit)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Kb;
            if (k == null) return false;
            switch (digit)
            {
                case 1: return k.digit1Key.wasPressedThisFrame;
                case 2: return k.digit2Key.wasPressedThisFrame;
                case 3: return k.digit3Key.wasPressedThisFrame;
                case 4: return k.digit4Key.wasPressedThisFrame;
            }
            return false;
#else
            switch (digit)
            {
                case 1: return Input.GetKeyDown(KeyCode.Alpha1);
                case 2: return Input.GetKeyDown(KeyCode.Alpha2);
                case 3: return Input.GetKeyDown(KeyCode.Alpha3);
                case 4: return Input.GetKeyDown(KeyCode.Alpha4);
            }
            return false;
#endif
        }

        public static bool QPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                return k != null && k.qKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Q);
#endif
            }
        }

        public static bool EPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Kb;
                return k != null && k.eKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.E);
#endif
            }
        }
    }
}
