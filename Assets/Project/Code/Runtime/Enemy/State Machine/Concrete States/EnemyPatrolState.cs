using UnityEngine;

public class EnemyPatrolState : EnemyState
{
    public EnemyPatrolState(EnemyMotor motor, EnemyStateMachine stateMachine, EnemyAnimator anim) : base(ref motor, stateMachine, anim)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Patrol();
        motor.SetHorizontalInput(0f);
        //add anim walk enter
        anim.PlayWalk();
    }
    public override void FrameUpdate()
    {
       // base.FrameUpdate();//unknown if needed, shouldnt hurt?

        
        motor.SetHorizontalInput(motor.patrolSpeed);

       


    }

    public override void ExitState()
    {
        //motor.SetHorizontalInput(0f);
        motor.ZeroHorizontal();
    }


}
