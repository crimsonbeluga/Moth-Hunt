using UnityEngine;
using MothHunt.Input;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerBrain : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }

    private PlayerMotor _motor;
    private MothHuntInput _input;

    // States
    private PlayerIdleState _idle;
    private PlayerWalkState _walk;
    private PlayerSprintState _sprint;
    private PlayerCrouchState _crouch;
    private PlayerJumpState _jump;
    private PlayerGlideState _glide;
    private PlayerClimbState _climb;
    private PlayerAirState _air;

    // Edge flags
    private bool _jumpPressedThisFrame;
    private bool _climbPressedThisFrame;   // NEW

    private void OnJumpPressed() => _jumpPressedThisFrame = true;
    private void OnClimbPressed() => _climbPressedThisFrame = true;   // NEW

    private void Awake()
    {
        _motor = GetComponent<PlayerMotor>();

        _input = new MothHuntInput();
        PlayerInputRouter.Bind(_input.Player);
        _input.Player.Enable();

        PlayerInputRouter.OnJumpPressed += OnJumpPressed;
        PlayerInputRouter.OnClimbPressed += OnClimbPressed;           // NEW

        StateMachine = new PlayerStateMachine();

        _idle = new PlayerIdleState(_motor, StateMachine);
        _walk = new PlayerWalkState(_motor, StateMachine);
        _sprint = new PlayerSprintState(_motor, StateMachine);
        _crouch = new PlayerCrouchState(_motor, StateMachine);
        _jump = new PlayerJumpState(_motor, StateMachine);
        _glide = new PlayerGlideState(_motor, StateMachine);
        _climb = new PlayerClimbState(_motor, StateMachine);
        _air = new PlayerAirState(_motor, StateMachine);
    }

    private void Start()
    {
        StateMachine.Initialize(_idle);
    }

    private void Update()
    {
        StateMachine.CurrentPlayerState?.FrameUpdate();

        // ===== AIRBORNE transitions =====
        if (!_motor.IsGrounded())
        {
            if (PlayerInputRouter.GlideHeld && !Is<PlayerGlideState>()) { StateMachine.ChangeState(_glide); return; }
            if (!PlayerInputRouter.GlideHeld && Is<PlayerGlideState>()) { StateMachine.ChangeState(_air); return; }
            if (!Is<PlayerGlideState>() && !Is<PlayerJumpState>() && !Is<PlayerAirState>() && !Is<PlayerClimbState>())
            { StateMachine.ChangeState(_air); return; }
        }

        // ===== CLIMB attach/detach =====
        // Attach (press while in range & not already climbing)
        if (_climbPressedThisFrame)
        {
            _climbPressedThisFrame = false;

            if (!Is<PlayerClimbState>())
            {
                // only attach if we have a candidate
                if (_motor.HasClimbCandidate) { StateMachine.ChangeState(_climb); return; }

            }
            else
            {
                // toggle OFF: detach and fall
                StateMachine.ChangeState(_air);   // ExitState() on climb will call EndClimb() to restore gravity
                return;
            }
        }

        // ===== JUMP (edge) =====
        if (_jumpPressedThisFrame)
        {
            _jumpPressedThisFrame = false;
            if (_motor.IsGrounded() && (Is<PlayerIdleState>() || Is<PlayerWalkState>() || Is<PlayerSprintState>() || Is<PlayerCrouchState>()))
            {
                StateMachine.ChangeState(_jump);
                return;
            }
        }

        // ===== GROUNDED locomotion =====
        bool hasMove = PlayerInputRouter.IsMoving;
        bool sprint = PlayerInputRouter.SprintHeld;
        bool crouch = PlayerInputRouter.CrawlHeld;

        if (_motor.IsGrounded() || Is<PlayerWalkState>() || Is<PlayerSprintState>() || Is<PlayerCrouchState>())
        {
            if (crouch && !Is<PlayerCrouchState>()) { StateMachine.ChangeState(_crouch); return; }
            if (!crouch && hasMove && sprint && !Is<PlayerSprintState>()) { StateMachine.ChangeState(_sprint); return; }
            if (!crouch && hasMove && !sprint && !Is<PlayerWalkState>()) { StateMachine.ChangeState(_walk); return; }
            if (!hasMove && !crouch && !Is<PlayerIdleState>()) { StateMachine.ChangeState(_idle); return; }
        }
    }

    private bool Is<T>() where T : PlayerState => StateMachine.CurrentPlayerState is T;

    private void OnDisable()
    {
        PlayerInputRouter.OnJumpPressed -= OnJumpPressed;
        PlayerInputRouter.OnClimbPressed -= OnClimbPressed;   // NEW
        _input?.Player.Disable();
    }
}
