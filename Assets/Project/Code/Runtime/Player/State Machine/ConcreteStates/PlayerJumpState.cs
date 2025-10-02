using UnityEngine;
using MothHunt.Input;

public class PlayerJumpState : PlayerState
{
    private float _rampElapsed;
    private float _rampDuration;
    private float _targetAirSpeed;
    private float _startAirSpeed;

    public PlayerJumpState(PlayerMotor motor, PlayerStateMachine sm, PlayerAnimator anim) : base(motor, sm, anim) { }

    public override void EnterState()
    {
        float m = motor.SprintMomentum; // 0..1 from actual movement speed
        _startAirSpeed = motor.walkSpeed;
        _targetAirSpeed = Mathf.Lerp(motor.walkSpeed, motor.sprintSpeed, m);

        motor.airMoveSpeed = _startAirSpeed;
        motor.DoJump();
        motor.Mode_AirMove();

        _rampElapsed = 0f;
        _rampDuration = Mathf.Max(0.0001f, motor.airSpeedRampTime);

        motor.SetHorizontalInput(0f);

        anim.PlayJump();

    }

    public override void FrameUpdate()
    {
        if (_rampElapsed < _rampDuration)
        {
            _rampElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_rampElapsed / _rampDuration);
            motor.airMoveSpeed = Mathf.Lerp(_startAirSpeed, _targetAirSpeed, t);
            motor.ApplyAirMoveCap();
        }

        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
        // no reset of airMoveSpeed; Air state manages it
    }
}
