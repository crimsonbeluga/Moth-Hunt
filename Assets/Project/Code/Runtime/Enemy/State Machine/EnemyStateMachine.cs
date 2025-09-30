using UnityEngine;

public class EnemyStateMachine
{
    public EnemyState CurrentEnemyState {  get; private set; }

    public void Initialize(EnemyState startingState)
    {
        CurrentEnemyState = startingState;
        CurrentEnemyState?.EnterState();
    }

    public void ChangeState(EnemyState newState)
    {
        if (newState == null || newState == CurrentEnemyState) return;
        CurrentEnemyState?.ExitState();
        CurrentEnemyState = newState;
        CurrentEnemyState.EnterState();
        Debug.Log($"[FSM] -> {CurrentEnemyState.GetType().Name}");
    }
}
