// PlayerWalkState.cs
using MothHunt.Input;
using Unity.VisualScripting;

public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(PlayerMotor motor, PlayerStateMachine sm, PlayerAnimator anim) : base(motor, sm, anim) { }

    public override void EnterState()
    {
        var col = motor.GetComponent<SimpleCapsuleResizer>();
        if (col) col.Stand();

        motor.Mode_Walk();
        motor.SetHorizontalInput(0f);
        anim.PlayWalk();
    }

    public override void FrameUpdate()
    {
        var mv = PlayerInputRouter.Move;
        motor.SetHorizontalInput(mv.x);
        motor._noise.MakeNoise(motor._noise._walkSuspicionRange);
        //
        
    }

    public override void ExitState()
    {
        motor.SetHorizontalInput(0f);
        motor.ZeroHorizontal();
    }
}
