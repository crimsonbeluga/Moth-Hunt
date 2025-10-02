// PlayerIdleState.cs
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerMotor motor, PlayerStateMachine stateMachine, PlayerAnimator anim) : base(motor, stateMachine, anim) { }

    public override void EnterState()
    {
        var col = motor.GetComponent<SimpleCapsuleResizer>();
        if (col) col.Stand();

        anim.PlayIdle();
    }

    public override void FrameUpdate() { }
    public override void ExitState() { }
}
