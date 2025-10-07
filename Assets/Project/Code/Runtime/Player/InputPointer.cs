using UnityEngine;
using UnityEngine.InputSystem;

namespace MothHunt.Input
{
    /// <summary>
    /// Centralized pointer access for the new Input System (Mouse/Touch/Pen).
    /// Avoids any use of UnityEngine.Input in a New Input System project.
    /// </summary>
    public static class InputPointer
    {
        /// <summary>Current screen-space pointer position (pixels).</summary>
        public static Vector2 ScreenPosition
        {
            get
            {
                // Prefer Pointer.current (works for Mouse/Touch/Pen). Fallback: Mouse device.
                var pointer = Pointer.current ?? (InputSystem.GetDevice<Mouse>() as Pointer);
                return pointer != null
                    ? pointer.position.ReadValue()
                    : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
        }

        /// <summary>True on the frame the primary pointer is pressed (LMB/tap/pen).</summary>
        public static bool PrimaryPressedThisFrame =>
            (Pointer.current as Mouse)?.leftButton.wasPressedThisFrame
            ?? (Pointer.current as Pen)?.tip.wasPressedThisFrame
            ?? Touchscreen.current?.primaryTouch.press.wasPressedThisFrame
            ?? false;
    }
}
