using UnityEngine;

public class EnemySearchState : EnemyState
{
    public EnemySearchState(EnemyMotor motor, EnemyStateMachine stateMachine, EnemyAnimator anim) : base(ref motor, stateMachine, anim)
    {
    }

    public override void EnterState()
    {
        motor.Mode_Search();
        motor.SetHorizontalInput(0f);
        anim.PlayWalk();
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
