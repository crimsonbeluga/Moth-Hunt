using UnityEngine;

public class EnemyReturningState : EnemyState
{
    public EnemyReturningState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(motor, stateMachine)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Search();//Potentially change to new motor mode
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
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
