using MothHunt.Input;

public class PlayerCrouchState : PlayerState
{
    public PlayerCrouchState(PlayerMotor motor, PlayerStateMachine sm, PlayerAnimator anim) : base(motor, sm, anim) { }

    public override void EnterState()
    {
        motor.Mode_Crouch();
        motor.SetHorizontalInput(0f);
        anim.PlayCrouch();
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
        // If you later shrink collider in Enter, restore it here
    }
}
