using UnityEngine;

[RequireComponent(typeof(EnemyMotor))]
public class EnemyBrain : MonoBehaviour
{
    //Variables
    public EnemyStateMachine StateMachine { get; private set; }

    public EnemyMotor _motor;
    public CharacterController playerCharacterController;

    //Variables for States
    public bool isIdleToStart = false;
    //pathfinding
    public Transform[] patrolRoute;
    private int patrolInt = 0;
    //suspicion
    public float suspicion = 0;
    public float suspicionThreshold = 100f;


    //States
    private EnemyIdleState _idle;
    private EnemyChaseState _chase;
    private EnemyAlertedState _alerted;
    private EnemyAttackState _attack;
    private EnemyReturningState _returning;
    private EnemyPatrolState _patrol;
    private EnemySearchState _search;


    [Header("Debug")]
    public bool logBrainFrames = true;
    public bool logDecisions = true;
    public bool logTransitions = true;

    private string CurStateName => StateMachine?.CurrentEnemyState?.GetType().Name ?? "(null)";
    private void DBG(string msg) { if (logBrainFrames || logDecisions || logTransitions) Debug.Log($"[Brain] {msg}"); }
    private void DEC(string msg) { if (logDecisions) Debug.Log($"[Brain/DEC] {msg}"); }
    private void TRN(string msg) { if (logTransitions) Debug.Log($"[Brain/TRN] {msg}"); }

    private void Awake()
    {
        _motor = GetComponent<EnemyMotor>();

        //create a state machine for the enemy
        StateMachine = new EnemyStateMachine();
        //fill in all relevant states
        _idle = new EnemyIdleState(_motor, StateMachine);
        _patrol = new EnemyPatrolState(_motor, StateMachine);
        _chase = new EnemyChaseState(_motor,StateMachine);
        _alerted = new EnemyAlertedState(_motor,StateMachine);
        _attack = new EnemyAttackState(_motor,StateMachine);
        _returning = new EnemyReturningState(_motor,StateMachine);
        _search = new EnemySearchState(_motor,StateMachine);

    }


    void Start()
    {
        if (isIdleToStart)
        {
            TRN("Initialize -> Idle");
            StateMachine.Initialize(_idle);
        }
        else
        {
            TRN("Initialize -> Patrol");
            StateMachine.Initialize(_patrol);
        }

    }

    // Update is called once per frame
    void Update()
    {
        StateMachine.CurrentEnemyState?.FrameUpdate();

    }
    
    //Unknown purpose to Shan
    private bool Is<T>() where T : EnemyState => StateMachine.CurrentEnemyState is T;
    
    



}
