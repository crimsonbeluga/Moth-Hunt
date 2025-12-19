using UnityEngine;

public class EnemyState 
{
    protected EnemyMotor motor;
    protected EnemyStateMachine stateMachine;
    protected EnemyAnimator anim;


    public EnemyState(ref EnemyMotor motor, EnemyStateMachine stateMachine, EnemyAnimator anim)
    {
        this.motor = motor;
        this.stateMachine = stateMachine;
        this.anim = anim;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
}
