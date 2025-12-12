using UnityEngine;

public class EnemyShootState : EnemyState
{
    public EnemyShootState(ref EnemyMotor motor, EnemyStateMachine stateMachine) : base(ref motor, stateMachine)
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

