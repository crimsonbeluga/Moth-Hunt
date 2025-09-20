using UnityEngine;


public class PlayerController : MonoBehaviour
{
    //done before game runs/in editor
    private void Awake()
    {
        //Connect states to state machine
        PlayerStateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, PlayerStateMachine);

    }

    void Start()
    {
        //initialize state machine with starting state
        PlayerStateMachine.Initialize(IdleState);
        
    }

    
    void Update()
    {
        PlayerStateMachine.CurrentPlayerState.FrameUpdate();

        //for debug
      //  if () { PlayerStateMachine.ChangeState(WalkState); }
       // else {  PlayerStateMachine.ChangeState(IdleState); }
    }



    #region State Machine Variables
    public PlayerStateMachine PlayerStateMachine { get; set; }
    public PlayerIdleState IdleState { get; set; }
    public PlayerWalkState WalkState { get; set; }

    #endregion

    #region Health Functions

    #endregion

    #region Movement Functions

    #endregion

}