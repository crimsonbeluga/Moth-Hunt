public class PlayerState
{
    protected PlayerMotor motor;
    protected PlayerStateMachine stateMachine;
    protected PlayerAnimator anim;

    public PlayerState(PlayerMotor motor, PlayerStateMachine stateMachine, PlayerAnimator anim)
    {
        this.motor = motor;
        this.stateMachine = stateMachine;
        this.anim = anim;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
}
