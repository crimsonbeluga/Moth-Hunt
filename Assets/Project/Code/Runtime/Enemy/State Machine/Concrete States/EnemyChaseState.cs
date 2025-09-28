using UnityEngine;

public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(motor, stateMachine)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Sprint();
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
        /*  ** PSEUDO CODE ** 
         *  Find line/distance to Player.CharacterController(?) and return a direction
         *  Set motor.SetHorizontalInput(Direction of Player.CharacterController)
         */
    }

    public override void ExitState() 
    {
        motor.SetHorizontalInput(0f);
    }
}
