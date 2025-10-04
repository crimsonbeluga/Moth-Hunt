using UnityEngine;

public class EnemyPatrolState : EnemyState
{
    public EnemyPatrolState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(motor, stateMachine)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Walk();
        motor.SetHorizontalInput(motor.walkSpeed);
    }
    public override void FrameUpdate()
    {
        base.FrameUpdate();//unknown if needed, shouldnt hurt?

        motor.SetHorizontalInput(motor.walkSpeed);

        /*
         * 
         */

    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
    }


}
