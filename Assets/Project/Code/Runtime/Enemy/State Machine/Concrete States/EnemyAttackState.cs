using UnityEngine;

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(EnemyMotor motor, EnemyStateMachine stateMachine, EnemyAnimator anim) : base(ref motor, stateMachine, anim  )
    {
    }

    public override void EnterState()
    {
        motor.Mode_Chase();
        motor.SetHorizontalInput(0f);
        //Set movement to 0f
        /* ** PSEUDO CODE **
         * 
         * Set player motor velocity to 0f
         * Call enemy animator to change to grab animation
         * 
         * Call player death system (Unknown if static call or reference needed)
         * 
         * 
         */

    }

    public override void FrameUpdate()
    {


    }

    public override void ExitState() 
    {

    }
}
