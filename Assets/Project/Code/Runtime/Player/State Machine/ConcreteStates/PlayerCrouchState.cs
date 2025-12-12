// PlayerCrouchState.cs
using MothHunt.Input;

public class PlayerCrouchState : PlayerState
{
    public PlayerCrouchState(PlayerMotor motor, PlayerStateMachine sm, PlayerAnimator anim) : base(motor, sm, anim) { }

    public override void EnterState()
    {
        var col = motor.GetComponent<SimpleCapsuleResizer>();
        if (col) col.Crouch();

        motor.Mode_Crouch();
        motor.SetHorizontalInput(0f);
        anim.PlayCrouch();
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);

        if (!motor.IsMovingHorizontally())
        {
            anim.SetSpeed(0f);

        }
        else
        {
            anim.SetSpeed(1f);
            //add noise
            motor._noise.MakeNoise(motor._noise._crouchSuspicionRange);
        }
    }

    public override void ExitState()
    {

        motor.SetHorizontalInput(0f);
    }
}
