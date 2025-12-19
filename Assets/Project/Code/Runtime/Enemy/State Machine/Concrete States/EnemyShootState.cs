using UnityEngine;

public class EnemyShootState : EnemyState
{
    public EnemyShootState(EnemyMotor motor, EnemyStateMachine stateMachine, EnemyAnimator anim) : base(ref motor, stateMachine, anim)
    {
    }
    public override void EnterState()
    {
        motor.Mode_Search();
        motor.SetHorizontalInput(0f);
        //add anim walk enter
    }
    public override void FrameUpdate()
    {
        motor.SetHorizontalInput(0f);

        /*
         * 
         */

    }

    public override void ExitState()
    {
        //motor.SetHorizontalInput(0f);
        motor.ZeroHorizontal();
    }
}

