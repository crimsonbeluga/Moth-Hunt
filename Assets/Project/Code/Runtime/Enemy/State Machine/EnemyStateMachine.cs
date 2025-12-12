using UnityEngine;

public class EnemyStateMachine
{
    public EnemyState CurrentEnemyState {  get; private set; }
    private EnemyState PreviousEnemyState { get;  set; }

    public void Initialize(EnemyState startingState)
    {
        PreviousEnemyState = null;
        CurrentEnemyState = startingState;
        CurrentEnemyState?.EnterState();
    }

    public void ChangeState(EnemyState newState)
    {
        if (newState == null || newState == CurrentEnemyState) return;
        CurrentEnemyState?.ExitState();
        PreviousEnemyState = CurrentEnemyState;
        CurrentEnemyState = newState;
        CurrentEnemyState.EnterState();
        Debug.Log($"[FSM] -> {CurrentEnemyState.GetType().Name}");
    }
}
