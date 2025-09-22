using UnityEngine;

public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(PlayerController controller, PlayerStateMachine stateMachine) : base(controller, stateMachine)
    {


    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("WALKING");
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
    }
}

