using MothHunt.Input;

public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(PlayerMotor motor, PlayerStateMachine sm) : base(motor, sm) { }

    public override void EnterState()
    {
        motor.Mode_Walk();
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;   // expects "Move" action in your input map
        motor.SetHorizontalInput(mv.x);    // set useZForHorizontal=true if your scene uses Z
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
        motor.ZeroHorizontal();
    }
}
