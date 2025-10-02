using UnityEngine;
using MothHunt.Input;

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

    [Header("Locomotion smoothing")]
    public float moveDeadzone = 0.06f;       // ignore micro inputs around 0
    public float idleEnterDelay = 0.08f;     // must be still for this long before Idle
    public float idleSpeedThreshold = 0.05f; // also be basically not moving

    private float _lastNonZeroMoveTime = -999f;

    private string CurStateName => StateMachine?.CurrentPlayerState?.GetType().Name ?? "(null)";
    private void DBG(string msg) { if (logBrainFrames || logDecisions || logTransitions) Debug.Log($"[Brain] {msg}"); }
    private void DEC(string msg) { if (logDecisions) Debug.Log($"[Brain/DEC] {msg}"); }
    private void TRN(string msg) { if (logTransitions) Debug.Log($"[Brain/TRN] {msg}"); }

    private void OnJumpPressed() { _jumpPressedThisFrame = true; _lastJumpPressTime = Time.time; DEC($"Jump PRESSED at t={_lastJumpPressTime:F3}"); }
    private void OnClimbPressed() { _climbPressedThisFrame = true; DEC("Climb PRESSED"); }

    private PlayerAnimator _anim;

    private void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        _anim = GetComponent<PlayerAnimator>(); // Animator wrapper driven by states

        _input = new MothHuntInput();
        PlayerInputRouter.Bind(_input.Player);
        _input.Player.Enable();

        PlayerInputRouter.OnJumpPressed += OnJumpPressed;
        PlayerInputRouter.OnClimbPressed += OnClimbPressed;

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

    private void Start()
    {
        TRN("Initialize -> Idle");
        StateMachine.Initialize(_idle);
    }

    private bool HoldQualifiesForGlide()
    {
        if (_motor.IsGrounded()) { DEC("Glide check: grounded."); return false; }
        if (!PlayerInputRouter.JumpHeld) { DEC("Glide check: JumpHeld=false."); return false; }

        float heldFor = Time.time - _lastJumpPressTime;
        if (heldFor < glideHoldThreshold)
        {
            DEC($"Glide check: heldFor {heldFor:F3}s < threshold {glideHoldThreshold:F3}s.");
            return false;
        }

        if (glideRequireDescent && _motor.VerticalSpeed > 0f)
        {
            DEC($"Glide check: ascending vY={_motor.VerticalSpeed:F2} (require descent).");
            return false;
        }

        DEC($"Glide check PASSED: heldFor={heldFor:F3}s, vY={_motor.VerticalSpeed:F2}.");
        return true;
    }

    private void Update()
    {
        if (logBrainFrames)
        {
            DBG($"State={CurStateName} grounded={_motor.IsGrounded()} vY={_motor.VerticalSpeed:F2} move={PlayerInputRouter.Move} IsMoving={PlayerInputRouter.IsMoving} JumpHeld={PlayerInputRouter.JumpHeld}");
        }

        // Track whether input is meaningfully moving this frame (deadzone aware)
        var mvNow = PlayerInputRouter.Move;
        bool inputMoving = Mathf.Abs(mvNow.x) > moveDeadzone;
        if (inputMoving)
            _lastNonZeroMoveTime = Time.time;

        StateMachine.CurrentPlayerState?.FrameUpdate();

        // ===== AIRBORNE transitions =====
        if (!_motor.IsGrounded())
        {
            DEC("Airborne block entered.");

            if (HoldQualifiesForGlide() && !Is<PlayerGlideState>())
            {
                TRN($"ChangeState -> Glide (from {CurStateName}) via HOLD.");
                StateMachine.ChangeState(_glide);
                return;
            }

            if (!PlayerInputRouter.JumpHeld && Is<PlayerGlideState>())
            {
                TRN("ChangeState -> Air (from Glide) because JumpHeld released.");
                StateMachine.ChangeState(_air);
                return;
            }

            if (!Is<PlayerGlideState>() && !Is<PlayerJumpState>() && !Is<PlayerAirState>() && !Is<PlayerClimbState>())
            {
                TRN($"ChangeState -> Air (from {CurStateName}) fallback airborne.");
                StateMachine.ChangeState(_air);
                return;
            }
        }
        else
        {
            DEC("Grounded: skipping airborne checks.");
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
                    StateMachine.ChangeState(_climb);
                    return;
                }
                else DEC("Climb press ignored: no candidate.");
            }
            else
            {
                TRN("Climb toggled off -> Air.");
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
                return;
            }

            if (_motor.IsGrounded() && (Is<PlayerIdleState>() || Is<PlayerWalkState>() || Is<PlayerSprintState>() || Is<PlayerCrouchState>()))
            {
                TRN($"ChangeState -> Jump (from {CurStateName}) because jump pressed while grounded.");
                StateMachine.ChangeState(_jump);
                return;
            }
            else DEC("Jump press ignored (not grounded or wrong state).");
        }

        // ===== GROUNDED locomotion =====
        bool sprint = PlayerInputRouter.SprintHeld;
        bool crouch = PlayerInputRouter.CrawlHeld;

        // Use deadzone-aware intent for movement
        bool hasMove = inputMoving;

        // Only allow Idle if we've been still long enough AND we are basically not moving
        bool readyToIdle = !hasMove
                           && (Time.time - _lastNonZeroMoveTime) > idleEnterDelay
                           && _motor.CurrentPlanarSpeed < idleSpeedThreshold;

        if (_motor.IsGrounded() || Is<PlayerWalkState>() || Is<PlayerSprintState>() || Is<PlayerCrouchState>())
        {
            if (crouch && !Is<PlayerCrouchState>()) { TRN($"-> Crouch (from {CurStateName})"); StateMachine.ChangeState(_crouch); return; }
            if (!crouch && hasMove && sprint && !Is<PlayerSprintState>()) { TRN($"-> Sprint (from {CurStateName})"); StateMachine.ChangeState(_sprint); return; }
            if (!crouch && hasMove && !sprint && !Is<PlayerWalkState>()) { TRN($"-> Walk (from {CurStateName})"); StateMachine.ChangeState(_walk); return; }
            if (readyToIdle && !Is<PlayerIdleState>()) { TRN($"-> Idle (from {CurStateName})"); StateMachine.ChangeState(_idle); return; }
        }
    }

    private bool Is<T>() where T : PlayerState => StateMachine.CurrentPlayerState is T;

    private void OnDisable()
    {
        PlayerInputRouter.OnJumpPressed -= OnJumpPressed;
        PlayerInputRouter.OnClimbPressed -= OnClimbPressed;
        _input?.Player.Disable();
        DBG("OnDisable: input unbound and map disabled.");
    }
}
