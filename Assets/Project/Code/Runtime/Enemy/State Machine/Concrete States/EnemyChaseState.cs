using UnityEngine;

public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyMotor motor, EnemyStateMachine stateMachine) : base(motor, stateMachine)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Chase();
        motor.SetHorizontalInput(0f);
        Debug.Log("Chase Entered.");
    }

    public override void FrameUpdate()
    {
        motor.SetHorizontalInput(motor.chaseSpeed);
        /*  ** PSEUDO CODE ** 
         *  //move to last player location
         * 
         *  
         */
    }

    public override void ExitState() 
    {
        motor.SetHorizontalInput(0f);
        motor.ZeroHorizontal();
    }
}
