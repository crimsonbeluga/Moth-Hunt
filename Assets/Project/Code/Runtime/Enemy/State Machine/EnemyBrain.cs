using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(EnemyMotor))]
public class EnemyBrain : MonoBehaviour
{
    [Header("References")]
    public EnemyStateMachine StateMachine { get; private set; }
    public EnemyMotor _motor;
    public EnemyAnimator _animator;
    public Transform playerTransform;

    [Header("Initialization")]
    public bool _isIdleToStart = false;
    public bool _isIdleFacingRight = true;

    [Header("Enemy Type")]
    public bool _IsDefaultEnemy = false;
    public bool _IsCrystalEnemy = false;
    public bool _IsReporterEnemy = false;
    public bool _IsGrabberEnemy = false;
    public bool _IsRangerEnemy = false;

    private bool _isUnaware = true;
    private float _timeDelayed = 0.0f;
    private bool _isAlertedThreshold = false;
    private bool _isChasing = false;

    [Header("Pathfinding")]
    private Transform _startingTransform;
    public Transform[] _patrolRoute;
    public int _patrolInt = 0;
    private Vector3 _lastKnownPlayerLocation;

    [Header("Suspicion")]
    public float _suspicion = 0;
    public float _suspicionThreshold = 100f;
    public float _alertedThreshold = 15f;
    public float _chaseThreshold = 75f;
    public float _searchTime = 0f;
    public float _hearingRange = 0f;

    [Header("Distance")]
    public float _lineOfSightRange = 5f;
    public float _attackRange = 1f;

    //Line of Sight
    private RaycastHit _hit;
    private bool _isFacingRight;


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
        _idle = new EnemyIdleState(_motor, StateMachine, _animator);
        _patrol = new EnemyPatrolState(_motor, StateMachine, _animator);
        _chase = new EnemyChaseState(_motor,StateMachine, _animator);
        _alerted = new EnemyAlertedState(_motor,StateMachine, _animator);
        _attack = new EnemyAttackState(_motor,StateMachine, _animator);
        _returning = new EnemyReturningState(_motor,StateMachine, _animator);
        _search = new EnemySearchState(_motor,StateMachine, _animator);

        

    }


    void Start()
    {
        //set starting state actions
        if (_isIdleToStart)
        {
            //if idle to start
            TRN("Initialize -> Idle");
            StateMachine.Initialize(_idle);
            //for idle enemies, get the starting transform to return to
            _startingTransform = this.transform;
            _timeDelayed = Time.time + _searchTime;
        }
        else
        {
            //if patrolling to start
            TRN("Initialize -> Patrol");
            StateMachine.Initialize(_patrol);
            SetPatrolDirection();
        }

        playerTransform = FindFirstObjectByType(typeof(PlayerBrain)).GetComponent<PlayerBrain>().gameObject.transform;

        _lastKnownPlayerLocation = new Vector3(0f,0f,0f);
    }

    // Update is called once per frame
    void Update()
    {
        StateMachine.CurrentEnemyState?.FrameUpdate();

        //_motor.velocityDirection(_isFacingRight);
        //Debug.Log(_motor.);


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

        //remove line of sight for enemies that do not need access
        if (!_IsCrystalEnemy)
        {

            //check line of sight based on current state
            if (Is<EnemyPatrolState>())
            {
                CheckLOS(ref _motor.patrolSpeed);
            }
            else if (Is<EnemyAlertedState>())
            {
                CheckLOS(ref _motor.patrolSpeed);
            }
            else if (Is<EnemyChaseState>())
            {
                CheckLOS(ref _motor.chaseSpeed);
            }
            else if (Is<EnemySearchState>())
            {
                CheckLOS(ref _motor.searchSpeed);
            }
            else if (Is<EnemyReturningState>())
            {
                CheckLOS(ref _motor.patrolSpeed);
            }
            else if (Is<EnemyIdleState>())
            {
                CheckLOS(ref _motor.patrolSpeed);
            }


            //if raycast hits the player transform (player is seen)
            if (_hit.transform == playerTransform.transform)
            {
                _suspicion = _suspicionThreshold;
                _isChasing = true;
            }
            else
            {
                SightLost();
            }
        }
        //always lose suspicion
        _suspicion -= 2;


        //set suspicion levels
        if (_suspicion > _alertedThreshold)
        {
            //if above alerted threshold
            _isAlertedThreshold = true;

            _isUnaware = false;

        }
        else
        {
            //failed to reach alerted
            _isAlertedThreshold = false;
            _isUnaware = true;
        }


        // if reach alerted state
        if (_isAlertedThreshold || _isChasing )
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
                    //Mathf.Abs(_motor.patrolSpeed);//set patrol speed positive, attempt to fix re entering state
                    SetChaseDirection();
                    //MathF.Abs(_motor.searchSpeed); //*****TESTING
                    return;
                }
                //_lastKnownPlayerLocation = playerTransform;
                //SearchLimiting();
            }

            
            //set state if not already in it
            if (!Is<EnemyAlertedState>() && Is<EnemyPatrolState>() || Is<EnemyIdleState>())
            {
                //set state to alerted
                TRN($"ChangeState -> Alerted (from {CurStateName}) via Suspicion.");
                StateMachine.ChangeState(_alerted);
                MathF.Abs(_motor.patrolSpeed);// ***TESTING
                return;
            }
        }
        
        if(Is<EnemySearchState>())
        {
            if (_isChasing) return;
            //set search direction
            if (this.transform.position.x <= _lastKnownPlayerLocation.x)
            {
                _motor.searchSpeed = -2f;
            }
            else if (this.transform.position.x >= _lastKnownPlayerLocation.x)
            {
                _motor.searchSpeed = 2f;
            }

            _timeDelayed = Time.time + _searchTime;
            SearchLimiting();

            
        }    

        //if no suspicion
        if (_suspicion <= 0 )
        {


            //if not returning
            if(!Is<EnemyReturningState>() && Is<EnemySearchState>())
            {

                TRN($"ChangeState -> Returning (from {CurStateName}) via lost Suspicion.");
                StateMachine.ChangeState(_returning);
                Mathf.Abs(_motor.chaseSpeed);// attempt to fix re entering state
                return;

            }
            //if returning   *****RETURN TO LATER, DO NOT REMEMBER PURPOSE
            if(Is<EnemyReturningState>())
            {
                // IF IDLE, RETURN TO START TRANSFORM
                if(_isIdleToStart)
                {
                    //check for return progress
                    //if speed is positive and position left of starting transform
                    if (_motor.patrolSpeed > 0 && this.transform.position.x <= _startingTransform.position.x)
                    {
                        
                        if (this.transform.position.x >= _startingTransform.position.x)
                        {
                            //if location is passed or at, set unaware
                            _isUnaware = true;
                        }
                        else
                        {
                            //else, move player in direciton
                            SetPatrolDirection();
                            return;
                            
                        }

                    }//speed is negative and position is right of starting transform
                    else if (_motor.patrolSpeed < 0 && this.transform.position.x >= _startingTransform.position.x)
                    {
                        if (this.transform.position.x <= _startingTransform.position.x)
                        {
                            //if location is passed or at, set unaware
                            _isUnaware = true;
                        }
                        else
                        {
                            //else, move player in correct direction
                            SetPatrolDirection();
                            return;

                        }
                    }
                   
                }
                else //IF PATROLLING, RETURN TO LAST PATROL TRANSFORM.
                {
                    _isUnaware = true;

                    _motor.ZeroHorizontal();
                }
            }

        }

        if (_isUnaware)//final check to return to unaware
        {

            //decide which state to check
            if (_isIdleToStart)
            {
                //if not idle
                if (!Is<EnemyIdleState>())
                {
                    //set state idle
                    TRN($"ChangeState -> Idle (from {CurStateName}) via Lack of Suspicion.");
                    StateMachine.ChangeState(_idle);
                    _timeDelayed = Time.time + _searchTime;
                    return;
                }
                else
                {
                    

                    //set a delay to turn around
                    if(Time.time >= _timeDelayed)
                    {
                        if(_isIdleFacingRight)
                        {
                            _isIdleFacingRight = false;
                            _motor.SetHorizontalInput(-.25f);
                            _motor.patrolSpeed *= -1;
                        }
                        else
                        {
                            _isIdleFacingRight = true;
                            _motor.SetHorizontalInput(.25f);
                            Mathf.Abs(_motor.patrolSpeed);
                            
                        }
                        _timeDelayed = Time.time + _searchTime;
                    }
                    else
                    {
                        _motor.SetHorizontalInput(0f);
                    }

                    //if idle, return
                    return;
                }
            }
            else//if patrol to start
            {


                //if not patrolling
                if (!Is<EnemyPatrolState>() && Is<EnemyAlertedState>() || Is<EnemyReturningState>())
                {
                    //_motor.SetHorizontalInput(10f);
                    //set state to patrol
                    TRN($"ChangeState -> Patrol (from {CurStateName}) via Loss of Player.");
                    StateMachine.ChangeState(_patrol);
                    SetPatrolDirection();
                    return;
                }
                else
                {
                    //Debug.Log("Patrolling at " + _motor.patrolSpeed + " units. ");
                    //if patrolling, return
                    //SetPatrolDirection();
                    
                    if(_isFacingRight)
                    {

                    }
                    if(!_isFacingRight)
                    {

                    }

                    SetPatrolDirection();

                    return;
                }

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
            //if(!_isFacingRight)
            {
                _motor.patrolSpeed *= -1f;
                
            }
            
        }
        Debug.Log("Patrol direction is " + _motor.patrolSpeed + " Units");

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
        if (playerTransform.position.x < this.transform.position.x)
        {
            _motor.chaseSpeed *= -1;

        }
        else if (playerTransform.position.x > this.transform.position.x)
        {
            Mathf.Abs(_motor.chaseSpeed);
        }
        else
        {
            //caught player
            if (playerTransform.position.y == this.transform.position.y)
            {
                //player is caught, set attack state


            }
        }

    }

    public void SearchLimiting()
    {

        if (Time.time >= _timeDelayed)
        {

            if (_motor.searchSpeed > 0f)
            {
                if (this.transform.position.x > _lastKnownPlayerLocation.x && _suspicion <= 0)
                {
                    TRN($"ChangeState -> Returning (from {CurStateName}) via Loss of Suspicion.");
                    StateMachine.ChangeState(_returning);
                    return;
                }
            }
            else
            {
                if (this.transform.position.x < _lastKnownPlayerLocation.x && _suspicion <= 0)
                {
                    TRN($"ChangeState -> Searching (from {CurStateName}) via Loss of Suspicion.");
                    StateMachine.ChangeState(_returning);
                    return;
                }
            }
        }
    }

    public void SightLost()
    {
        _lastKnownPlayerLocation = playerTransform.position;
        _isChasing = false;
        if (!Is<EnemySearchState>() && Is<EnemyChaseState>())//safety check
        {
            TRN($"ChangeState -> Searching (from {CurStateName}) via Loss of Sight.");
            StateMachine.ChangeState(_search);
            return;
        }
    }

    public void CheckLOS(ref float input)
    {
        //if player enter line of sight
        //
        if (input < 0f)
        {
            Physics.Raycast(this.transform.position, -this.transform.right, out _hit, _lineOfSightRange);
           
        }
        else
        {
            Physics.Raycast(this.transform.position, this.transform.right, out _hit, _lineOfSightRange);
        }
    }

    //  **  LISTENER CODE **
    public void listen(float input) 
    {
        _suspicion += input;
    }



}
