using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Axes")]
    [Tooltip("If true, horizontal movement uses Z instead of X (for scenes where Z is left/right).")]
    public bool useZForHorizontal = false;

    [Header("Speeds")]
    [Min(0f)] public float walkSpeed = 4f;
    [Min(0f)] public float sprintSpeed = 7f;
    [Min(0f)] public float crouchSpeed = 2f;
    [Min(0f)] public float airMoveSpeed = 4f;        // horizontal cap while airborne
    [Min(0f)] public float glideHorizontalSpeed = 3f;
    [Min(0f)] public float climbSpeed = 3f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 2.2f;                  // meters
    public float normalGravity = -30f;               // negative
    public float glideGravity = -6f;                 // negative
    public float terminalFallSpeed = -40f;           // negative
    public float glideFallSpeed = -8f;               // negative

    [Header("Debug")]
    public bool logInputs = false;

    // --- Internals (the 3 knobs) ---
    private float _desiredX;           // -1..1 input set by states each frame
    private float _desiredY;           // -1..1 (used by climb only)
    private float _curMaxSpeedX;       // set by state on Enter
    private float _curGravity;         // set by state on Enter
    private float _curTerminal;        // set by state on Enter

    // Execution state
    private CharacterController _cc;
    private Vector3 _velocity;         // current velocity (x,y,z)
    private bool _climbMode;           // disables gravity; uses _desiredY * climbSpeed
    private bool _glideMode;           // affects gravity + horizontal cap

    // Climbable target from triggers
    private Climbable _climbCandidate;
    public bool HasClimbCandidate => _climbCandidate != null;
    public Climbable CurrentClimbable => _climbCandidate;

    // Public query for Brain (used to gate glide on descent)
    public float VerticalSpeed => _velocity.y;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        Mode_Walk(); // default baseline
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        // Horizontal
        float horiz = _desiredX * _curMaxSpeedX;
        if (useZForHorizontal)
        {
            _velocity.x = 0f;
            _velocity.z = horiz;
        }
        else
        {
            _velocity.x = horiz;
            _velocity.z = 0f;
        }

        // Vertical
        if (_climbMode)
        {
            _velocity.y = _desiredY * climbSpeed;
        }
        else
        {
            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f; // stick to ground
            }
            else
            {
                _velocity.y += _curGravity * dt;
                if (_velocity.y < _curTerminal) _velocity.y = _curTerminal;
            }
        }

        // Move
        Vector3 delta = _velocity * dt;
        _cc.Move(delta);

        if (logInputs && Mathf.Abs(_desiredX) > 0.01f)
            Debug.Log($"[Motor] desiredX={_desiredX} cap={_curMaxSpeedX} vel={_velocity}");
    }

    // ---------------- 3 knobs (per-frame from states) ----------------
    public void SetHorizontalInput(float x01) => _desiredX = Mathf.Clamp(x01, -1f, 1f);
    public void SetVerticalClimbInput(float y01) => _desiredY = Mathf.Clamp(y01, -1f, 1f);
    public void SetGravity(float gravity, float terminal) { _curGravity = gravity; _curTerminal = terminal; }

    // ---------------- Mode setters (Enter/Exit in states) -------------
    public void Mode_Walk()
    {
        _climbMode = false; _glideMode = false;
        _curMaxSpeedX = walkSpeed;
        SetGravity(normalGravity, terminalFallSpeed);
    }

    public void Mode_Sprint()
    {
        _climbMode = false; _glideMode = false;
        _curMaxSpeedX = sprintSpeed;
        SetGravity(normalGravity, terminalFallSpeed);
    }

    public void Mode_Crouch()
    {
        _climbMode = false; _glideMode = false;
        _curMaxSpeedX = crouchSpeed;
        SetGravity(normalGravity, terminalFallSpeed);
        // TODO: adjust collider size here later, if desired
    }

    public void Mode_AirMove()
    {
        _climbMode = false; _glideMode = false;
        _curMaxSpeedX = airMoveSpeed;  // horizontal cap in air
        // Keep current gravity/terminal (normally normalGravity set by jump)
    }

    public void Mode_Glide()
    {
        if (_cc.isGrounded) return;
        _climbMode = false; _glideMode = true;
        _curMaxSpeedX = glideHorizontalSpeed;
        SetGravity(glideGravity, glideFallSpeed);
    }

    public void End_Glide()
    {
        _glideMode = false;
        SetGravity(normalGravity, terminalFallSpeed);
    }

    public void Mode_Climb()
    {
        _glideMode = false;
        _climbMode = true;
        _velocity.y = 0f; // stabilize when attaching
    }

    public void End_Climb()
    {
        _climbMode = false;
        SetGravity(normalGravity, terminalFallSpeed);
        _desiredY = 0f;
    }

    // ---------------- One-shots --------------------------------------
    public void DoJump()
    {
        if (_climbMode) End_Climb();
        if (!_cc.isGrounded) return;

        float jumpV = Mathf.Sqrt(Mathf.Abs(2f * normalGravity * jumpHeight));
        _velocity.y = jumpV;

        _glideMode = false;
        SetGravity(normalGravity, terminalFallSpeed);
    }

    public void CutJump()
    {
        if (_velocity.y > 0f) _velocity.y *= 0.5f;
    }

    // ---------------- Climbable candidate from triggers --------------
    public void SetClimbCandidate(Climbable c)
    {
        _climbCandidate = c;
        Debug.Log($"[Motor] ClimbCandidate = {(_climbCandidate ? _climbCandidate.name : "null")}");
    }
    public void ClearClimbCandidate(Climbable c)
    {
        if (_climbCandidate == c)
        {
            _climbCandidate = null;
            Debug.Log("[Motor] ClimbCandidate cleared");
        }
    }


    // ---------------- Queries / helpers ------------------------------
    public bool IsGrounded() => _cc.isGrounded;
    public bool IsClimbing() => _climbMode;
    public bool IsGliding() => _glideMode;

    public void ZeroHorizontal()
    {
        if (useZForHorizontal) _velocity.z = 0f;
        else _velocity.x = 0f;
    }
}
