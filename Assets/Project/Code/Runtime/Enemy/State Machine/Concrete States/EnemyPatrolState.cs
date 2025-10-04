using UnityEngine;

public class EnemyPatrolState : EnemyState
{
    public EnemyPatrolState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(motor, stateMachine)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Walk();
        motor.SetHorizontalInput(0f);
    }
    public override void FrameUpdate()
    {
        /*
         * 
         */
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
    }


}
