using UnityEngine;

public class EnemyState 
{
    protected EnemyMotor motor;
    protected EnemyStateMachine stateMachine;

    //variables each state can access
    //pathfinding
    public Transform[] patrolRoute;
    //suspicion
    public float suspicionThreshold;


    public EnemyState(EnemyMotor motor, EnemyStateMachine stateMachine)
    {
        this.motor = motor;
        this.stateMachine = stateMachine;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
}
