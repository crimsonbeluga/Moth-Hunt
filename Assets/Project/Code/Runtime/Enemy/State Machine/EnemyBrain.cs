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
    public Transform[] _patrolRoute;
    private int _patrolInt = 0;
    private Transform _lastKnownPlayerLocation;
    //suspicion
    public float _suspicion = 0;
    public float _suspicionThreshold = 100f;
    public float _alertedThreshold = 15f;
    public float _chaseThreshold = 75f;
    public float _searchTime = 0f;

    //booleans for states
    private bool _isAlertedThreshold = false;
    private bool _isChasing = false;

    //distance variables
    public float _lineOfSightRange = 5f;
    public float _attackRange = 1f;

    //Line of Sight
    private RaycastHit _hit;


    //reference to player
    public Transform PlayerTransform;


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

        //if player within line of sight or recieved noise
        //suspicion++
        

        //set suspicion levels
        if(_suspicion > _alertedThreshold )
        {
            //if above alerted threshold
            _isAlertedThreshold = true;

        }
        else
        {
            //failed to reach alerted
            _isAlertedThreshold = false;
        }

        //if player enter line of sight
        //set _isChasing = true
        Physics.Raycast(this.transform.position, this.transform.right, out _hit, _lineOfSightRange);

        

        // if reach alerted state
        if (_isAlertedThreshold)
        {
            
            //if reach chase state
            if (_isChasing) 
            {             
                //checks distance to player
                float distance = Vector3.Distance(this.transform.position, PlayerTransform.position);
                //if in attack range
                if (distance < _attackRange )
                {
                    if (!Is<EnemyAttackState>())
                    {
                        //set state to attack
                        TRN($"ChangeState -> Attack (from {CurStateName}) via Distance.");
                        StateMachine.ChangeState(_attack);
                        return;
                    }
                }

                if (!Is<EnemyChaseState>())
                {
                    //set state to chase
                    TRN($"ChangeState -> Chase (from {CurStateName}) via Suspicion.");
                    StateMachine.ChangeState(_chase);
                    return;
                }
            }

            if (!Is<EnemyAlertedState>())
            {
                //set state to alerted
                TRN($"ChangeState -> Alerted (from {CurStateName}) via Suspicion.");
                StateMachine.ChangeState(_alerted);
                return;
            }
        }
        
        //if no suspicion
        if (_suspicion == 0)
        {
            //if not returning
            if(!Is<EnemyReturningState>())
            {



            }
        }

        //decide which state to check
        if (isIdleToStart)
        {
            //if not idle
            if (!Is<EnemyIdleState>())
            {
                //set state idle
                TRN($"ChangeState -> Idle (from {CurStateName}) via Lack of Suspicion.");
                StateMachine.ChangeState(_idle);
                return;
            }
            else
            {
                //if idle, return
                return;
            }
        }
        else//if patrol to start
        {
            //if not patrolling
            if (!Is<EnemyPatrolState>())
            {
                //set state to patrol
                TRN($"ChangeState -> Patrol (from {CurStateName}) via Lack of Suspicion.");
                StateMachine.ChangeState(_patrol);
                return;
            }
            else
            {
                //if patrolling, return
                return;
            }

        }

    }
    
    //Unknown purpose to Shan
    private bool Is<T>() where T : EnemyState => StateMachine.CurrentEnemyState is T;
    
    



}
