using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(EnemyMotor motor, EnemyStateMachine stateMachine, EnemyAnimator anim) : base(ref motor, stateMachine, anim)
    {
    }

    public override void EnterState()
    {
        anim.PlayIdle();
    }

    public override void FrameUpdate()
    {

    }

    public override void ExitState() { }
}
