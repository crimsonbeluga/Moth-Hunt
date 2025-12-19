using UnityEngine;

public class EnemyReturningState : EnemyState
{
    public EnemyReturningState(EnemyMotor motor, EnemyStateMachine stateMachine, EnemyAnimator anim) : base(ref motor, stateMachine, anim   )
    {
    }

    public override void EnterState()
    {
        motor.Mode_Patrol();//Potentially change to new motor mode
        motor.SetHorizontalInput(0f);
        anim.PlayWalk();
    }

    public override void FrameUpdate()
    {
        motor.SetHorizontalInput(motor.patrolSpeed);
        /* ** PSEUDO CODE **
         * Get vector/direction of last patrol point
         * Set motor.SetHorizontalInput(Direction of LastPatrolPoint(TO BE ADDED LATER))
         */
    }

    public override void ExitState() 
    {
        motor.SetHorizontalInput(0f);
    }
}
