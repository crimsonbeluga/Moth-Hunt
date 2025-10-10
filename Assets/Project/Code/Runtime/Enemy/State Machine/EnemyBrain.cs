using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(EnemyMotor))]
public class EnemyBrain : MonoBehaviour
{
    //Variables
    public EnemyStateMachine StateMachine { get; private set; }

    public EnemyMotor _motor;
    public Transform playerTransform;

    //Variables for States
    public bool isIdleToStart = false;
    //pathfinding
    private Transform _startingTransform;
    public Transform[] _patrolRoute;
    public int _patrolInt = 0;
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
        //set starting state actions
        if (isIdleToStart)
        {
            //if idle to start
            TRN("Initialize -> Idle");
            StateMachine.Initialize(_idle);
            //for idle enemies, get the starting transform to return to
            _startingTransform = this.transform;

        }
        else
        {
            //if patrolling to start
            TRN("Initialize -> Patrol");
            StateMachine.Initialize(_patrol);
            SetPatrolDirection();
        }

    }

    // Update is called once per frame
    void Update()
    {
        StateMachine.CurrentEnemyState?.FrameUpdate();

        //suspicion cap to prevent overflow
        if(_suspicion > _suspicionThreshold) _suspicion = _suspicionThreshold;
        if(_suspicion < 0) _suspicion = 0;

        //Deciding patrol 
        //if in patrol state
        if (Is<EnemyPatrolState>())
        {
            //check what direction the enemy is moving
            //if moving right (+)
            if(_motor.patrolSpeed > 0f)
            {
                //at transform of patrol point
                if (this.transform.position.x >= _patrolRoute[_patrolInt].transform.position.x)
                {
                    //add pause
                    SetNextPatrolInt();
                    
                }

            }
            else //if moving left (-)
            {
                //at transform of patrol point
                if (this.transform.position.x <= _patrolRoute[_patrolInt].transform.position.x)
                {
                    
                    //add pause
                    SetNextPatrolInt();               
                    
                }
            }

        }


        //if player enter line of sight
        //
        if (_motor.patrolSpeed < 0f)
        {
            Physics.Raycast(this.transform.position, -this.transform.right, out _hit, _lineOfSightRange);
        }
        else
        {
            Physics.Raycast(this.transform.position, this.transform.right, out _hit, _lineOfSightRange);
        }

        if (_hit.transform == playerTransform.transform)
        {
            _suspicion = _suspicionThreshold;
            _isChasing = true;
        }
        else
        {
            _suspicion -= 2;
            SightLost();
        }


        //set suspicion levels
        if (_suspicion > _alertedThreshold)
        {
            //if above alerted threshold
            _isAlertedThreshold = true;



        }
        else
        {
            //failed to reach alerted
            _isAlertedThreshold = false;
        }


        // if reach alerted state
        if (_isAlertedThreshold )
        {
            
            //if reach chase state
            if ( _isChasing) 
            {             
                //checks distance to player
                float distance = Vector3.Distance(this.transform.position, playerTransform.position);
                //if in attack range
                if (distance <= _attackRange )
                {
                    if (!Is<EnemyAttackState>() && Is<EnemyChaseState>())
                    {
                        //set state to attack
                        TRN($"ChangeState -> Attack (from {CurStateName}) via Distance.");
                        StateMachine.ChangeState(_attack);
                        return;
                    }
                }


                //set state if not already in it
                if (!Is<EnemyChaseState>() && Is<EnemyAlertedState>())
                {
                    //set state to chase
                    TRN($"ChangeState -> Chase (from {CurStateName}) via Suspicion.");
                    StateMachine.ChangeState(_chase);
                    
                    SetChaseDirection();
                    return;
                }
                _lastKnownPlayerLocation = playerTransform;
                SearchLimiting();
            }

            //SetPatrolDirection();
            //set state if not already in it
            if (!Is<EnemyAlertedState>() && Is<EnemyPatrolState>() || Is<EnemyIdleState>())
            {
                //set state to alerted
                TRN($"ChangeState -> Alerted (from {CurStateName}) via Suspicion.");
                StateMachine.ChangeState(_alerted);
                SetPatrolDirection();
                return;
            }
        }
        
        

        //if no suspicion
        if (_suspicion == 0 )
        {
            //if not returning
            if(!Is<EnemyReturningState>() && Is<EnemySearchState>())
            {

                TRN($"ChangeState -> Returning (from {CurStateName}) via lost Suspicion.");
                StateMachine.ChangeState(_returning);
                return;

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
            if (!Is<EnemyPatrolState>() && Is<EnemyAlertedState>() || Is<EnemyReturningState>())
            {
                //set state to patrol
                TRN($"ChangeState -> Patrol (from {CurStateName}) via Lack of Suspicion.");
                StateMachine.ChangeState(_patrol);
                SetPatrolDirection();
                return;
            }
            else
            {
                //if patrolling, return
                return;
            }

        }




    }
    
    //Determine the current state and checks against which states are inactive
    private bool Is<T>() where T : EnemyState => StateMachine.CurrentEnemyState is T;

    //pseudo decision making, ensuring the sprite flips properly.
    public void SetPatrolDirection()
    {
        _motor.patrolSpeed = Mathf.Abs(_motor.patrolSpeed);
        if (_patrolRoute[_patrolInt].transform.position.x < this.transform.position.x)
        {
            _motor.patrolSpeed *= -1;
        }

    }

    private void SetNextPatrolInt()
    {

     
        //if next int is not null
        if (_patrolInt == _patrolRoute.Length-1)
        {
            _patrolInt = 0;
            SetPatrolDirection();
        }
        else //if next is null
        {
            _patrolInt++;
            SetPatrolDirection();
        }
    }

    public void SetChaseDirection()
    {
        _motor.chaseSpeed = Mathf.Abs(_motor.chaseSpeed);
        if(playerTransform.position.x < this.transform.position.x)
        {
            _motor.chaseSpeed *= -1;
            
        }
    }

    public void SearchLimiting()
    {
        if (_motor.chaseSpeed > 0f)
        {
            if (this.transform.position.x > _lastKnownPlayerLocation.transform.position.x && _suspicion <= 0 )
            {
                TRN($"ChangeState -> Returning (from {CurStateName}) via Loss of Suspicion.");
                StateMachine.ChangeState(_returning);
                return;
            }
        }
        else
        {
            if(this.transform.position.x < _lastKnownPlayerLocation.transform.position.x && _suspicion <= 0)
            {
                TRN($"ChangeState -> Searching (from {CurStateName}) via Loss of Suspicion.");
                StateMachine.ChangeState(_returning);
                return;
            }
        }
    }

    public void SightLost()
    {
        _lastKnownPlayerLocation = playerTransform;
        _isChasing = false;
        if (!Is<EnemySearchState>() && Is<EnemyChaseState>())//safety check
        {
            TRN($"ChangeState -> Searching (from {CurStateName}) via Loss of Sight.");
            StateMachine.ChangeState(_search);
            return;
        }
    }


}
