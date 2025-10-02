using MothHunt.Input;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(PlayerMotor motor, PlayerStateMachine sm) : base(motor, sm) { }

    public override void EnterState()
    {
        motor.DoJump();        // fixed-height jump
        motor.Mode_AirMove();  // horizontal cap while airborne
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);
        // No CutJump -> holding space doesn't change jump height
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
    }
}
