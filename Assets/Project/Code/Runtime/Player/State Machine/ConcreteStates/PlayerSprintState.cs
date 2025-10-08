// PlayerSprintState.cs
using MothHunt.Input;

public class PlayerSprintState : PlayerState
{
    public PlayerSprintState(PlayerMotor motor, PlayerStateMachine sm, PlayerAnimator anim) : base(motor, sm, anim) { }

    public override void EnterState()
    {
        var col = motor.GetComponent<SimpleCapsuleResizer>();
        if (col) col.Crouch(); // per your spec: sprint uses crouch-sized collider

        motor.Mode_Sprint();
        motor.SetHorizontalInput(0f);
        anim.PlaySprint();
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
        }
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
    }
}
