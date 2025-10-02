using UnityEngine;

public class PlayerStateMachine
{
    public PlayerState CurrentPlayerState { get; private set; }

    public void Initialize(PlayerState startingState)
    {
        CurrentPlayerState = startingState;
        CurrentPlayerState?.EnterState();
    }

    public void ChangeState(PlayerState newState)
    {
        if (newState == null || newState == CurrentPlayerState) return;
        CurrentPlayerState?.ExitState();
        CurrentPlayerState = newState;
        CurrentPlayerState.EnterState();
        Debug.Log($"[FSM] -> {CurrentPlayerState.GetType().Name}");
    }
}
