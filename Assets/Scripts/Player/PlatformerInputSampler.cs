using UnityEngine;
using UnityEngine.InputSystem;

// Capture short Space taps between network ticks; consume the queued press once in OnInput.
public sealed class PlatformerInputSampler
{
    bool jumpQueued;

    public void Capture(Keyboard keyboard)
    {
        if (keyboard == null) { Clear(); return; }
        jumpQueued |= keyboard.spaceKey.wasPressedThisFrame;
    }

    public void Read(Keyboard keyboard, out Vector2 moveDirection, out float climbDirection)
    {
        moveDirection = Vector2.zero;
        climbDirection = 0;
        if (keyboard != null)
        {
            moveDirection.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            moveDirection.y = keyboard.spaceKey.isPressed || jumpQueued ? 1 : 0;
            climbDirection = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
        }
        Clear();
    }

    public void Clear() => jumpQueued = false;
}
