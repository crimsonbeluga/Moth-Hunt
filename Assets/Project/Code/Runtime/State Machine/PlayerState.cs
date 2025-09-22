using UnityEngine;

public class PlayerState 
{
    protected PlayerController controller;
    protected PlayerStateMachine stateMachine;


    public PlayerState (PlayerController controller, PlayerStateMachine stateMachine)
    {
        this.controller = controller;
        this.stateMachine = stateMachine;
    }

    // To override within the states
    public virtual void EnterState() { }
    public virtual void ExitState() { }

    //In video, but unknown if needed
    public virtual void FrameUpdate() { }



}
