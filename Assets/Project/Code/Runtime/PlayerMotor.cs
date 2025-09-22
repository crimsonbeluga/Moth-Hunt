using UnityEngine;
using MothHunt.Input; // only used if you flip useRouterInput on

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Speeds")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float crouchSpeed = 2f;
    public float glideHorizontalSpeed = 3f;
    public float climbSpeed = 3f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 2.2f;        // meters
    public float normalGravity = -30f;     // negative
    public float glideGravity = -6f;       // negative (weaker)
    public float terminalFallSpeed = -40f; // negative
    public float glideFallSpeed = -8f;     // negative

    [Header("Testing (optional)")]
    public bool useRouterInput = false;    // enable to test without your FSM

    // --- internals ---
    private CharacterController _cc;
    private Vector3 _velocity;             // our running velocity (x,y,z)
    private float _inputX;                 // -1..1 (left/right)
    private float _inputY;                 // -1..1 (up/down for climb)

    private float _maxSpeedX;              // current horizontal speed cap
    private float _currentGravity;         // current gravity (negative)
    private float _currentTerminal;        // current terminal fall (negative)

    private bool _isGliding;
    private bool _isClimbing;
    private bool _isCrouching;
    private bool _isSprinting;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        BeginWalk();                       // default mode
    }

    private void Update()
    {
        if (useRouterInput)
        {
            _inputX = PlayerInputRouter.Move.x;
            _inputY = PlayerInputRouter.Move.y; // used for climbing
        }

        Tick(Time.deltaTime);
    }

    // -------------------------------------------------
    //                MAIN MOTOR LOOP
    // -------------------------------------------------
    public void Tick(float dt)
    {
        // --- Horizontal (X) ---
        float targetX = _inputX * _maxSpeedX;
        _velocity.x = targetX;

        // --- Vertical (Y) ---
        if (_isClimbing)
        {
            // Climb ignores gravity; uses vertical input
            _velocity.y = _inputY * climbSpeed;
        }
        else
        {
            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f)
                {
                    // small downward bias to keep grounded
                    _velocity.y = -2f;
                }
            }
            else
            {
                // gravity while in air
                _velocity.y += _currentGravity * dt;
                if (_velocity.y < _currentTerminal)
                {
                    _velocity.y = _currentTerminal;
                }
            }
        }

        // --- Apply movement ---
        Vector3 delta = _velocity * dt;
        _cc.Move(delta);
    }

    // -------------------------------------------------
    //               INPUT FROM FSM (per-frame)
    // -------------------------------------------------
    // Call this each frame from your FSM with your left/right axis.
    public void SetMoveInput(float x)
    {
        _inputX = Mathf.Clamp(x, -1f, 1f);
    }

    // For climb states (optional): call this each frame with both axes.
    public void SetMoveInput(float x, float y)
    {
        _inputX = Mathf.Clamp(x, -1f, 1f);
        _inputY = Mathf.Clamp(y, -1f, 1f);
    }

    // -------------------------------------------------
    //               STATE ENTER/EXIT METHODS
    // -------------------------------------------------
    // WALK (enter)
    public void BeginWalk()
    {
        _isSprinting = false;
        _isCrouching = false;
        _isGliding = false;

        _maxSpeedX = walkSpeed;
        _currentGravity = normalGravity;
        _currentTerminal = terminalFallSpeed;
    }

    // SPRINT (enter)
    public void BeginSprint()
    {
        _isSprinting = true;
        _isCrouching = false;
        _isGliding = false;

        _maxSpeedX = sprintSpeed;
        _currentGravity = normalGravity;
        _currentTerminal = terminalFallSpeed;
    }

    // CROUCH (enter)
    public void BeginCrouch()
    {
        _isCrouching = true;
        _isSprinting = false;
        _isGliding = false;

        _maxSpeedX = crouchSpeed;
        _currentGravity = normalGravity;
        _currentTerminal = terminalFallSpeed;
    }

    // CROUCH (exit)
    public void EndCrouch()
    {
        _isCrouching = false;
        // choose where you go next; walk is a safe default
        BeginWalk();
    }

    // GLIDE (enter) — only does something if airborne
    public void BeginGlide()
    {
        if (_cc.isGrounded) return;

        _isGliding = true;
        _maxSpeedX = glideHorizontalSpeed;
        _currentGravity = glideGravity;
        _currentTerminal = glideFallSpeed;
    }

    // GLIDE (exit)
    public void EndGlide()
    {
        _isGliding = false;
        _currentGravity = normalGravity;
        _currentTerminal = terminalFallSpeed;

        // pick a horizontal mode on exit; walk is safe
        if (_isSprinting) _maxSpeedX = sprintSpeed;
        else if (_isCrouching) _maxSpeedX = crouchSpeed;
        else _maxSpeedX = walkSpeed;
    }

    // CLIMB (enter)
    public void BeginClimb()
    {
        _isClimbing = true;
        _isGliding = false;
        _velocity.y = 0f; // reset vertical to avoid snap
    }

    // CLIMB (exit)
    public void EndClimb()
    {
        _isClimbing = false;
        _currentGravity = normalGravity;
        _currentTerminal = terminalFallSpeed;
    }

    // JUMP (one-shot)
    public void RequestJump()
    {
        if (_isClimbing)
        {
            EndClimb(); // optional: detach when jumping
        }

        if (_cc.isGrounded == false) return;

        // v = sqrt(2 * |g| * h)
        float jumpV = Mathf.Sqrt(Mathf.Abs(2f * normalGravity * jumpHeight));
        _velocity.y = jumpV;

        _isGliding = false;
        _currentGravity = normalGravity;
        _currentTerminal = terminalFallSpeed;
    }

    // Optional short-hop: call when jump button released while rising
    public void CutJumpEarly()
    {
        if (_velocity.y > 0f)
        {
            _velocity.y = _velocity.y * 0.5f;
        }
    }

    // -------------------------------------------------
    //               SMALL HELPERS / GETTERS
    // -------------------------------------------------
    public Vector3 GetVelocity() { return _velocity; }
    public bool IsGrounded() { return _cc.isGrounded; }
    public bool IsGliding() { return _isGliding; }
    public bool IsClimbing() { return _isClimbing; }
    public bool IsSprinting() { return _isSprinting; }
    public bool IsCrouching() { return _isCrouching; }

    // If your FSM wants to zero horizontal instantly (cutscenes etc.)
    public void StopHorizontal()
    {
        _velocity.x = 0f;
    }
}
