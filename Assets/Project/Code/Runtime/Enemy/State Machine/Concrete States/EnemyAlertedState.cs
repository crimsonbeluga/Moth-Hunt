using UnityEngine;

public class EnemyAlertedState : EnemyState
{
    public EnemyAlertedState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(motor, stateMachine)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Crouch();//logic is alerted is slower, so takes crawling values at approx. half walk speed
        motor.SetHorizontalInput(0f);
    }

    public override void FrameUpdate()
    {
        /* ** PSEUDO CODE **
         * Check for Player.CharacterController
         * if (spotted) {increase aggression}
         * else
         * {reduce agression}
         * 
         */
    }

    public override void ExitState() 
    {
        motor.SetHorizontalInput(0f);
    }
}
