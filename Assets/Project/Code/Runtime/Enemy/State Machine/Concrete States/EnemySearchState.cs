using UnityEngine;

public class EnemySearchState : EnemyState
{
    public EnemySearchState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(ref motor, stateMachine)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Search();
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
        motor.SetHorizontalInput(motor.searchSpeed);
        /*
         * 
         */
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
        motor.ZeroHorizontal();
    }


}
