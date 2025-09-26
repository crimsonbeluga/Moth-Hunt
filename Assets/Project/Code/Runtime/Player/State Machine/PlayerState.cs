public class PlayerState
{
    protected PlayerMotor motor;
    protected PlayerStateMachine stateMachine;

    public PlayerState(PlayerMotor motor, PlayerStateMachine stateMachine)
    {
        this.motor = motor;
        this.stateMachine = stateMachine;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
}
