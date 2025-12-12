using UnityEngine;

public class EnemyState 
{
    protected EnemyMotor motor;
    protected EnemyStateMachine stateMachine;


    public EnemyState(ref EnemyMotor motor, EnemyStateMachine stateMachine)
    {
        this.motor = motor;
        this.stateMachine = stateMachine;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
}
