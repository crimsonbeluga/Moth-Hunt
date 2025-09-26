using MothHunt.Input;

public class PlayerClimbState : PlayerState
{
    public PlayerClimbState(PlayerMotor motor, PlayerStateMachine sm) : base(motor, sm) { }

    public override void EnterState()
    {
        motor.Mode_Climb();              // zero vertical, disable gravity
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetVerticalClimbInput(mv.y); // up/down only
        motor.SetHorizontalInput(0f);      // lock horizontal while climbing
    }

    public override void ExitState()
    {
        motor.End_Climb();                  // restores normal gravity + clears climb intent
        motor.SetVerticalClimbInput(0f);   // stop vertical drift
    }
}
