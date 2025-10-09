using MothHunt.Input;
using UnityEngine;

public class PlayerGlideState : PlayerState
{
    public PlayerGlideState(PlayerMotor motor, PlayerStateMachine sm) : base(motor, sm) { }

    public override void EnterState()
    {
        Debug.Log("[GlideState] EnterState -> calling Mode_Glide()");
        motor.Mode_Glide();            // weak gravity + glide terminal
        motor.SetHorizontalInput(0f);
        Debug.Log($"[GlideState] After Mode_Glide  isGliding={motor.IsGliding()} grounded={motor.IsGrounded()} vY={motor.VerticalSpeed:F2}");
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);

        // safety: if somehow grounded, brain should kick us out next frame,
        // but log it here too so we can see it earlier.
        if (motor.IsGrounded())
        {
            Debug.Log("[GlideState] Grounded while in Glide; expect Brain to switch to Air/ground state.");
        }
    }

    public override void ExitState()
    {
        Debug.Log("[GlideState] ExitState -> calling End_Glide()");
        motor.End_Glide();             // restores normal gravity/terminal
        motor.SetHorizontalInput(0f);
        Debug.Log($"[GlideState] After End_Glide  isGliding={motor.IsGliding()} grounded={motor.IsGrounded()} vY={motor.VerticalSpeed:F2}");
    }
}
