using UnityEngine;

public class PlayerStateMachine
{
    [Header("States")]
    public PlayerState CurrentPlayerState { get; private set; }
    public PlayerState PreviousPlayerState { get; private set; }    

    public void Initialize(PlayerState startingState)
    {
        PreviousPlayerState = null;
        CurrentPlayerState = startingState;
        CurrentPlayerState?.EnterState();
    }

    public void ChangeState(PlayerState newState)
    {
        if (newState == null || newState == CurrentPlayerState) return;
        CurrentPlayerState?.ExitState();
        PreviousPlayerState = CurrentPlayerState;
        CurrentPlayerState = newState;
        CurrentPlayerState.EnterState();
        Debug.Log($"[FSM] -> {CurrentPlayerState.GetType().Name}");
    }
}
