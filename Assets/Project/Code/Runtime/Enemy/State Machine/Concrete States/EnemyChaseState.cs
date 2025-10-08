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
    }

    public override void FrameUpdate()
    {
        /*  ** PSEUDO CODE ** 
         *  //move to last player location
         *  
         */
    }

    public override void ExitState() 
    {
        
    }
}
