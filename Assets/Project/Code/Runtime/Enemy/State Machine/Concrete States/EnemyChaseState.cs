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
        motor.SetHorizontalInput(motor.chaseSpeed);

    }

    public override void ExitState() 
    {
        motor.SetHorizontalInput(0f);
        motor.ZeroHorizontal();
    }
}
