using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class EnemyBrain : MonoBehaviour
{
    //Variables
    public EnemyStateMachine StateMachine { get; private set; }

    public EnemyMotor _motor;


    //States
    private EnemyIdleState _idle;
    private EnemyChaseState _chase;
    private EnemyAlertedState _alerted;
    private EnemyAttackState _attack;
    private EnemyReturningState _returning;


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



    }


    void Start()
    {
        TRN("Initialize -> Idle");
        StateMachine.Initialize(_idle);
    }

    // Update is called once per frame
    void Update()
    {
        StateMachine.CurrentEnemyState?.FrameUpdate();

    }
    
    //Unknown purpose to Shan
    private bool Is<T>() where T : EnemyState => StateMachine.CurrentEnemyState is T;
    




}
