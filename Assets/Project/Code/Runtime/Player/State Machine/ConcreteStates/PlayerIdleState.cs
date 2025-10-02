// States/PlayerIdleState.cs
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerMotor motor, PlayerStateMachine sm, PlayerAnimator anim) : base(motor, sm, anim) { }

    public override void EnterState()
    {
        anim.PlayIdle();
    }

    public override void FrameUpdate()
    {
       
    }

    public override void ExitState() { }
}
