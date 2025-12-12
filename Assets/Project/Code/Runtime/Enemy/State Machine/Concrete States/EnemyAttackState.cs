using UnityEngine;

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(motor, stateMachine)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Walk();
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
        /* ** PSEUDO CODE **
         * Trigger attack code
         * Consider player distance
         * Move toward player
         */

    }

    public override void ExitState() 
    {
        motor.SetHorizontalInput(0f);
    }
}
