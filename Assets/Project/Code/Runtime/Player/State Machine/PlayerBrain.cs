using UnityEngine;
using MothHunt.Input;
using MothHunt.Throwing; // for cancel helper

[RequireComponent(typeof(PlayerMotor))]
public class PlayerBrain : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }

    private PlayerMotor _motor;
    private MothHuntInput _input;

    private PlayerIdleState _idle;
    private PlayerWalkState _walk;
    private PlayerSprintState _sprint;
    private PlayerCrouchState _crouch;
    private PlayerJumpState _jump;
    private PlayerGlideState _glide;
    private PlayerClimbState _climb;
    private PlayerAirState _air;

    private bool _jumpPressedThisFrame;
    private bool _climbPressedThisFrame;

    [Header("Glide (hold Space)")]
    public float glideHoldThreshold = 0.08f;
    public bool glideRequireDescent = true;

    private float _lastJumpPressTime = -999f;

    [Header("Debug")]
    public bool logBrainFrames = true;
    public bool logDecisions = true;
    public bool logTransitions = true;
    public bool logLifecycle = true;

    [Header("Locomotion smoothing")]
    public float moveDeadzone = 0.06f;
    public float idleEnterDelay = 0.08f;
    public float idleSpeedThreshold = 0.05f;

    private float _lastNonZeroMoveTime = -999f;

    [Header("Suspicion")]
    public SphereCollider _noiseCollider;
    public NoiseMaker _noiseMaker;
    public float _suspicionVolume;
    public float _walkSuspicionRange;
    public float _sprintSuspicionRange;
    public float _jumpSuspicionRange;
    public float _crouchSuspicionRange;



    private string CurStateName => StateMachine?.CurrentPlayerState?.GetType().Name ?? "(null)";
    private void DBG(string msg) { if (logBrainFrames || logDecisions || logTransitions || logLifecycle) Debug.Log($"[Brain f{Time.frameCount} t{Time.time:0.000}] {msg}", this); }
    private void DEC(string msg) { if (logDecisions) Debug.Log($"[Brain/DEC f{Time.frameCount}] {msg}", this); }
    private void TRN(string msg) { if (logTransitions) Debug.Log($"[Brain/TRN f{Time.frameCount}] {msg}", this); }

    private void OnJumpPressed() { _jumpPressedThisFrame = true; _lastJumpPressTime = Time.time; DEC($"Jump PRESSED at t={_lastJumpPressTime:F3}"); }
    private void OnClimbPressed() { _climbPressedThisFrame = true; DEC("Climb PRESSED"); }

    private PlayerAnimator _anim;
    private PlayerThrowController _throw; // cached

    private void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        _anim = GetComponent<PlayerAnimator>();
        _throw = GetComponent<PlayerThrowController>();

        _input = new MothHuntInput();
        DBG("Awake: created MothHuntInput wrapper instance.");

        StateMachine = new PlayerStateMachine();

        _idle = new PlayerIdleState(_motor, StateMachine, _anim);
        _walk = new PlayerWalkState(_motor, StateMachine, _anim);
        _sprint = new PlayerSprintState(_motor, StateMachine, _anim);
        _crouch = new PlayerCrouchState(_motor, StateMachine, _anim);
        _jump = new PlayerJumpState(_motor, StateMachine, _anim);
        _glide = new PlayerGlideState(_motor, StateMachine, _anim);
        _climb = new PlayerClimbState(_motor, StateMachine, _anim);
        _air = new PlayerAirState(_motor, StateMachine, _anim);


    }

    private void OnEnable()
    {
        if (_input == null) _input = new MothHuntInput();

        // Router lifecycle
        PlayerInputRouter.Unbind();                    // clear stale flags
        PlayerInputRouter.Bind(_input.Player);
        _input.Player.Enable();

        // Subscribe edges
        PlayerInputRouter.OnJumpPressed += OnJumpPressed;
        PlayerInputRouter.OnClimbPressed += OnClimbPressed;

        // Dump map status
        DBG($"OnEnable: router bound + map enabled. mapEnabled={_input.Player.enabled} IsGrounded={PlayerInputRouter.IsGrounded} Move={PlayerInputRouter.Move}");
    }

    private void Start()
    {
        TRN("Initialize -> Idle");
        StateMachine.Initialize(_idle);
    }

    private bool HeadroomToStand()
    {
        var col = _motor ? _motor.GetComponent<SimpleCapsuleResizer>() : null;
        return col ? col.HasHeadroomForStand() : true;
    }

    private bool HoldQualifiesForGlide()
    {
        if (_motor.IsGrounded()) { DEC("Glide check: grounded."); return false; }
        if (!PlayerInputRouter.JumpHeld) { DEC("Glide check: JumpHeld=false."); return false; }

        float heldFor = Time.time - _lastJumpPressTime;
        if (heldFor < glideHoldThreshold) { DEC($"Glide check: heldFor {heldFor:F3}s < threshold {glideHoldThreshold:F3}s."); return false; }
        if (glideRequireDescent && _motor.VerticalSpeed > 0f) { DEC($"Glide check: ascending vY={_motor.VerticalSpeed:F2} (require descent)."); return false; }

        DEC($"Glide check PASSED: heldFor={heldFor:F3}s, vY={_motor.VerticalSpeed:F2}.");
        return true;
    }

    private void Update()
    {
        if (logBrainFrames)
            DBG($"State={CurStateName} grounded={_motor.IsGrounded()} vY={_motor.VerticalSpeed:F2} move={PlayerInputRouter.Move} IsMoving={PlayerInputRouter.IsMoving} JumpHeld={PlayerInputRouter.JumpHeld}");

        var mvNow = PlayerInputRouter.Move;
        bool inputMoving = Mathf.Abs(mvNow.x) > moveDeadzone;
        if (inputMoving) _lastNonZeroMoveTime = Time.time;

        StateMachine.CurrentPlayerState?.FrameUpdate();

        // ===== AIRBORNE transitions =====
        if (!_motor.IsGrounded())
        {
            DEC("Airborne block entered.");

            if (HoldQualifiesForGlide() && !Is<PlayerGlideState>())
            {
                TRN($"ChangeState -> Glide (from {CurStateName}) via HOLD.");
                CancelThrowOverlayIfAny();
                StateMachine.ChangeState(_glide);
                return;
            }

            if (!PlayerInputRouter.JumpHeld && Is<PlayerGlideState>())
            {
                TRN("ChangeState -> Air (from Glide) because JumpHeld released.");
                CancelThrowOverlayIfAny();
                StateMachine.ChangeState(_air);
                return;
            }

            if (!Is<PlayerGlideState>() && !Is<PlayerJumpState>() && !Is<PlayerAirState>() && !Is<PlayerClimbState>())
            {
                TRN($"ChangeState -> Air (from {CurStateName}) fallback airborne.");
                CancelThrowOverlayIfAny();
                StateMachine.ChangeState(_air);
                return;
            }
        }

        // ===== CLIMB attach/detach =====
        if (_climbPressedThisFrame)
        {
            _climbPressedThisFrame = false;

            if (!Is<PlayerClimbState>())
            {
                if (_motor.HasClimbCandidate)
                {
                    TRN($"ChangeState -> Climb (from {CurStateName}) because climb pressed & candidate.");
                    CancelThrowOverlayIfAny();
                    StateMachine.ChangeState(_climb);
                    return;
                }
                else DEC("Climb press ignored: no candidate.");
            }
            else
            {
                TRN("Climb toggled off -> Air.");
                CancelThrowOverlayIfAny();
                StateMachine.ChangeState(_air);
                return;
            }
        }

        // ===== JUMP / DROP (edge) =====
        if (_jumpPressedThisFrame)
        {
            _jumpPressedThisFrame = false;

            DEC($"Jump edge: DropChord={PlayerInputRouter.DropChord}, grounded={_motor.IsGrounded()} move={PlayerInputRouter.Move}");

            if (PlayerInputRouter.DropChord && _motor.TryDropThrough())
            {
                DEC("Jump edge became DROP (platform opened).");
                CancelThrowOverlayIfAny();
                return;
            }

            if (_motor.IsGrounded() && (Is<PlayerIdleState>() || Is<PlayerWalkState>() || Is<PlayerSprintState>() || Is<PlayerCrouchState>()))
            {
                if (!HeadroomToStand()) { DEC("Jump blocked: no headroom to stand."); return; }

                TRN($"ChangeState -> Jump (from {CurStateName}) because jump pressed while grounded.");
                CancelThrowOverlayIfAny();
                StateMachine.ChangeState(_jump);
                return;
            }
            else DEC("Jump press ignored (not grounded or wrong state).");
        }

        // ===== GROUNDED locomotion =====
        bool sprint = PlayerInputRouter.SprintHeld;
        bool crouch = PlayerInputRouter.CrawlHeld;
        bool hasMove = inputMoving;

        bool readyToIdle = !hasMove
                           && (Time.time - _lastNonZeroMoveTime) > idleEnterDelay
                           && _motor.CurrentPlanarSpeed < idleSpeedThreshold;

        _noiseMaker.onTick();


        if (_motor.IsGrounded() || Is<PlayerWalkState>() || Is<PlayerSprintState>() || Is<PlayerCrouchState>())
        {
            if (crouch && !Is<PlayerCrouchState>()) { TRN($"-> Crouch (from {CurStateName})"); CancelThrowOverlayIfAny(); StateMachine.ChangeState(_crouch); return; }
            if (!crouch && hasMove && sprint && !Is<PlayerSprintState>()) { TRN($"-> Sprint (from {CurStateName})"); CancelThrowOverlayIfAny(); StateMachine.ChangeState(_sprint); return; }

            bool exitingShortCapsule = Is<PlayerCrouchState>() || Is<PlayerSprintState>();

            if (!crouch && hasMove && !sprint)
            {
                if (exitingShortCapsule && !HeadroomToStand()) { DEC("Walk blocked: no headroom to stand yet."); return; }
                if (!Is<PlayerWalkState>()) { TRN($"-> Walk (from {CurStateName})"); StateMachine.ChangeState(_walk); return; } // Walk allows throw overlay
            }

            if (!crouch && readyToIdle)
            {
                if (exitingShortCapsule && !HeadroomToStand()) { DEC("Idle blocked: no headroom to stand yet."); return; }
                if (!Is<PlayerIdleState>()) { TRN($"-> Idle (from {CurStateName})"); StateMachine.ChangeState(_idle); return; } // Idle allows throw overlay
            }
        }
    }

    private bool Is<T>() where T : PlayerState => StateMachine.CurrentPlayerState is T;

    private void CancelThrowOverlayIfAny()
    {
        if (_throw && _throw.IsActive)
            _throw.SendMessage("ExitThrowMode", SendMessageOptions.DontRequireReceiver);
    }

    private void OnDisable()
    {
        PlayerInputRouter.OnJumpPressed -= OnJumpPressed;
        PlayerInputRouter.OnClimbPressed -= OnClimbPressed;

        _input?.Player.Disable();
        PlayerInputRouter.Unbind();   // clears all intent values

        DBG("OnDisable: router unbound, map disabled.");
    }
}
