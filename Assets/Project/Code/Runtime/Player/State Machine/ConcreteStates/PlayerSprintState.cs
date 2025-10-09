using MothHunt.Input;

public class PlayerSprintState : PlayerState
{
    public PlayerSprintState(PlayerMotor motor, PlayerStateMachine sm) : base(motor, sm) { }

    public override void EnterState()
    {
        motor.Mode_Sprint();
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
    }
}
