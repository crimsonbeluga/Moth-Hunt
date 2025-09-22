using MothHunt.Input;

public class PlayerGlideState : PlayerState
{
    public PlayerGlideState(PlayerMotor motor, PlayerStateMachine sm) : base(motor, sm) { }

    public override void EnterState()
    {
        motor.Mode_Glide();            // weak gravity + glide horizontal cap
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);
        // Release → Brain switches to Air, Exit restores gravity
    }

    public override void ExitState()
    {
        motor.End_Glide();             // restores normal gravity/terminal
        motor.SetHorizontalInput(0f);
    }
}
