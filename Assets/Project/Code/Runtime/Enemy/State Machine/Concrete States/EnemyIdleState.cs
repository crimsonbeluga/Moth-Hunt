using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(ref motor, stateMachine) { }

    public override void EnterState()
    {

    }

    public override void FrameUpdate()
    {

    }

    public override void ExitState() { }
}
