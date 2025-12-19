using UnityEngine;

public class EnemyAlertedState : EnemyState
{
    public EnemyAlertedState(EnemyMotor motor, EnemyStateMachine stateMachine, EnemyAnimator anim) : base(ref motor, stateMachine, anim)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Patrol();
        motor.SetHorizontalInput(0f);
        anim.PlaySurpised();
    }

    public override void FrameUpdate()
    {
        //motor.SetHorizontalInput(motor.patrolSpeed / 2);
    }

    public override void ExitState() 
    {
        motor.SetHorizontalInput(0f);
    }
}
