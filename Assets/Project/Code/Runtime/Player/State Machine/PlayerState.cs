using UnityEngine;

public class PlayerState
{
    protected PlayerMotor motor;
    protected PlayerStateMachine stateMachine;
    protected PlayerAnimator anim;

    public PlayerState(PlayerMotor motor, PlayerStateMachine stateMachine, PlayerAnimator anim)
    {
        this.motor = motor;
        this.stateMachine = stateMachine;
        this.anim = anim;
    }

    // --- Collider helpers (no headroom checks; always applies the profile) ---
    protected void UseStand()
    {
        var col = motor ? motor.GetComponent<SimpleCapsuleResizer>() : null;
        ColliderHelpers.ForceStand(col);
    }

    protected void UseCrouch()
    {
        var col = motor ? motor.GetComponent<SimpleCapsuleResizer>() : null;
        ColliderHelpers.ForceCrouch(col);
    }

    protected void UseGlide()
    {
        var col = motor ? motor.GetComponent<SimpleCapsuleResizer>() : null;
        ColliderHelpers.ForceGlide(col);
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
}
