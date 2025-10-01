using UnityEngine;
using MothHunt.Input;

public class PlayerAirState : PlayerState
{
    private float _smoothVel;

    public PlayerAirState(PlayerMotor motor, PlayerStateMachine sm) : base(motor, sm) { }

    public override void EnterState()
    {
        motor.Mode_AirMove();
    }

    public override void FrameUpdate()
    {
        float m = motor.SprintMomentum;
        float target = Mathf.Lerp(motor.walkSpeed, motor.sprintSpeed, m);

        motor.airMoveSpeed = Mathf.SmoothDamp(motor.airMoveSpeed, target, ref _smoothVel, 0.08f);
        motor.ApplyAirMoveCap();

        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
    }
}
