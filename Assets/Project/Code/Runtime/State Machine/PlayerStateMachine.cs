using UnityEngine;

public class PlayerStateMachine
{
    //reference to the current player state
    public PlayerState CurrentPlayerState { get; set; }

    public void Initialize(PlayerState startingState)
    {
        //loads the current state to be the chosen starting state
        CurrentPlayerState = startingState;
        //enters the chosen state
        CurrentPlayerState.EnterState();

    }

    public void ChangeState(PlayerState newState)
    {
        //ends the current player state
        CurrentPlayerState.ExitState();
        //set the current state to be the chosen one
        CurrentPlayerState = newState;
        //enters the new state chosen
        CurrentPlayerState.EnterState();

        //FOR DEBUGGING, REMOVE LATER
        Debug.Log(CurrentPlayerState.ToString());

    }


}
