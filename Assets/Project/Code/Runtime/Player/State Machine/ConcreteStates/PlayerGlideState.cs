// PlayerGlideState.cs
using MothHunt.Input;
using UnityEngine;

public class PlayerGlideState : PlayerState
{
    public PlayerGlideState(PlayerMotor motor, PlayerStateMachine sm, PlayerAnimator anim) : base(motor, sm, anim) { }

    public override void EnterState()
    {
        var col = motor.GetComponent<SimpleCapsuleResizer>();
        if (col) col.Glide();

        anim.PlayGlide();
        motor.Mode_Glide();            // weak gravity + glide terminal
        motor.SetHorizontalInput(0f);

        // update camera
        CameraController.Instance.ApplyActionModifier(CameraController.Instance.glidCamModifiers);

        Debug.Log($"[GlideState] Enter isGliding={motor.IsGliding()} grounded={motor.IsGrounded()} vY={motor.VerticalSpeed:F2}");
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);

        if (motor.IsGrounded())
            Debug.Log("[GlideState] Grounded while in Glide; expect Brain to switch out.");
    }

    public override void ExitState()
    {
        //reset camera
        CameraController.Instance.RemoveActionModifier(CameraController.Instance.glidCamModifiers);

        motor.End_Glide();
        motor.SetHorizontalInput(0f);
    }
}
