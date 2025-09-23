using System.Runtime.CompilerServices;
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
    [Tooltip("Per-frame logs of inputs and velocity (very verbose).")]
    public bool logFrames = false;
    [Tooltip("Logs when modes change (walk/sprint/air/glide/climb) and key one-shots (jump/cutjump).")]
    public bool logTransitions = true;
    [Tooltip("Logs whenever SetGravity is called or gravity/terminal is changed.")]
    public bool logGravityChanges = true;
    [Tooltip("Extra logs of horizontal input caps and movement.")]
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

    // ------------------------------------------------------------------

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        Mode_Walk(); // default baseline
        Debug.Log($"[Motor] Awake -> {DumpState()}");
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    // ------------------------------------------------------------------

    public void Tick(float dt)
    {
        if (logFrames)
            Debug.Log($"[Motor] Tick START dt={dt:F4}  {DumpState()}");

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

        if (logInputs)
            Debug.Log($"[Motor] HORIZ desiredX={_desiredX:F2} cap={_curMaxSpeedX:F2} -> vel.x/z=({_velocity.x:F2},{_velocity.z:F2})");

        // Vertical
        float beforeY = _velocity.y;

        if (_climbMode)
        {
            _velocity.y = _desiredY * climbSpeed;
            if (logFrames)
                Debug.Log($"[Motor] CLIMB vertical: desiredY={_desiredY:F2} climbSpeed={climbSpeed:F2} -> velY {beforeY:F2}→{_velocity.y:F2}");
        }
        else
        {
            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f; // stick to ground
                if (logFrames)
                    Debug.Log($"[Motor] GROUNDED vertical: velY {beforeY:F2}→{_velocity.y:F2}");

                // touching ground ends glide mode implicitly
                if (_glideMode)
                {
                    _glideMode = false;
                    if (logTransitions) Debug.Log("[Motor] Glide auto-ended because grounded.");
                }

                // ensure baseline gravity selected when grounded
                if (_curGravity != normalGravity || _curTerminal != terminalFallSpeed)
                {
                    if (logGravityChanges)
                        Debug.Log($"[Motor] Grounded -> resetting gravity to NORMAL (g={normalGravity}, term={terminalFallSpeed})");
                    SetGravity(normalGravity, terminalFallSpeed, "Grounded");
                }
            }
            else
            {
                // Safety: detect unexpected gravity while gliding
                if (_glideMode && (Mathf.Abs(_curGravity - glideGravity) > 0.001f || Mathf.Abs(_curTerminal - glideFallSpeed) > 0.001f))
                {
                    Debug.LogWarning($"[Motor] WARNING: In glideMode but gravity/terminal do not match glide. g={_curGravity}, term={_curTerminal} (expected g={glideGravity}, term={glideFallSpeed})");
                }

                _velocity.y += _curGravity * dt;
                if (_velocity.y < _curTerminal) _velocity.y = _curTerminal;

                if (logFrames)
                    Debug.Log($"[Motor] AIR vertical: g={_curGravity:F2} term={_curTerminal:F2} -> velY {beforeY:F2}→{_velocity.y:F2}");
            }
        }

        // Move
        Vector3 delta = _velocity * dt;
        CollisionFlags flags = _cc.Move(delta);

        if (logFrames)
            Debug.Log($"[Motor] Move delta={delta} flags={flags} grounded={_cc.isGrounded}");

        if (logFrames)
            Debug.Log($"[Motor] Tick END   {DumpState()}");
    }

    // ---------------- 3 knobs (per-frame from states) ----------------
    public void SetHorizontalInput(float x01)
    {
        _desiredX = Mathf.Clamp(x01, -1f, 1f);
        if (logInputs) Debug.Log($"[Motor] SetHorizontalInput -> {_desiredX:F2}");
    }

    public void SetVerticalClimbInput(float y01)
    {
        _desiredY = Mathf.Clamp(y01, -1f, 1f);
        if (logInputs) Debug.Log($"[Motor] SetVerticalClimbInput -> {_desiredY:F2}");
    }

    public void SetGravity(float gravity, float terminal, [CallerMemberName] string caller = null)
    {
        if (logGravityChanges)
            Debug.Log($"[Motor] SetGravity by '{caller ?? "unknown"}'  g: {_curGravity:F2}→{gravity:F2}  term: {_curTerminal:F2}→{terminal:F2}  (glideMode={_glideMode}, grounded={_cc.isGrounded})");

        _curGravity = gravity;
        _curTerminal = terminal;
    }

    // ---------------- Mode setters (Enter/Exit in states) -------------
    public void Mode_Walk()
    {
        _climbMode = false; _glideMode = false;
        _curMaxSpeedX = walkSpeed;
        SetGravity(normalGravity, terminalFallSpeed);
        if (logTransitions) Debug.Log($"[Motor] Mode_Walk -> {DumpState()}");
    }

    public void Mode_Sprint()
    {
        _climbMode = false; _glideMode = false;
        _curMaxSpeedX = sprintSpeed;
        SetGravity(normalGravity, terminalFallSpeed);
        if (logTransitions) Debug.Log($"[Motor] Mode_Sprint -> {DumpState()}");
    }

    public void Mode_Crouch()
    {
        _climbMode = false; _glideMode = false;
        _curMaxSpeedX = crouchSpeed;
        SetGravity(normalGravity, terminalFallSpeed);
        if (logTransitions) Debug.Log($"[Motor] Mode_Crouch -> {DumpState()}");
        // TODO: adjust collider size here later, if desired
    }

    public void Mode_AirMove()
    {
        _climbMode = false; _glideMode = false;
        _curMaxSpeedX = airMoveSpeed;  // horizontal cap in air
        // Keep current gravity/terminal (normally normalGravity set by jump)
        if (logTransitions) Debug.Log($"[Motor] Mode_AirMove -> {DumpState()}");
    }

    public void Mode_Glide()
    {
        if (_cc.isGrounded)
        {
            if (logTransitions) Debug.Log("[Motor] Mode_Glide requested but grounded. Ignored.");
            return;
        }

        _climbMode = false;
        _glideMode = true;

        _curMaxSpeedX = glideHorizontalSpeed;
        SetGravity(glideGravity, glideFallSpeed, nameof(Mode_Glide));

        // Immediate feel: clamp current vertical speed into the glide envelope now
        float beforeY = _velocity.y;
        if (_velocity.y < glideFallSpeed) _velocity.y = glideFallSpeed;
        else if (_velocity.y > 0f) _velocity.y = 0f;

        if (logTransitions)
            Debug.Log($"[Motor] Mode_Glide ENTER  velY {beforeY:F2}→{_velocity.y:F2}  (g={_curGravity}, term={_curTerminal})  {DumpState()}");
    }

    public void End_Glide()
    {
        _glideMode = false;
        SetGravity(normalGravity, terminalFallSpeed, nameof(End_Glide));
        if (logTransitions) Debug.Log($"[Motor] End_Glide -> {DumpState()}");
    }

    public void Mode_Climb()
    {
        _glideMode = false;
        _climbMode = true;
        float beforeY = _velocity.y;
        _velocity.y = 0f; // stabilize when attaching
        if (logTransitions) Debug.Log($"[Motor] Mode_Climb ENTER  velY {beforeY:F2}→{_velocity.y:F2}  {DumpState()}");
    }

    public void End_Climb()
    {
        _climbMode = false;
        SetGravity(normalGravity, terminalFallSpeed, nameof(End_Climb));
        _desiredY = 0f;
        if (logTransitions) Debug.Log($"[Motor] End_Climb -> {DumpState()}");
    }

    // ---------------- One-shots --------------------------------------
    public void DoJump()
    {
        if (_climbMode)
        {
            if (logTransitions) Debug.Log("[Motor] DoJump while climbing -> End_Climb first.");
            End_Climb();
        }
        if (!_cc.isGrounded)
        {
            if (logTransitions) Debug.Log("[Motor] DoJump ignored (not grounded).");
            return;
        }

        float jumpV = Mathf.Sqrt(Mathf.Abs(2f * normalGravity * jumpHeight));
        float beforeY = _velocity.y;
        _velocity.y = jumpV;

        _glideMode = false; // jumping cancels glide
        SetGravity(normalGravity, terminalFallSpeed, nameof(DoJump));

        if (logTransitions)
            Debug.Log($"[Motor] DoJump -> velY {beforeY:F2}→{_velocity.y:F2}  (jumpHeight={jumpHeight}, normalG={normalGravity})  {DumpState()}");
    }

    public void CutJump()
    {
        float beforeY = _velocity.y;
        if (_velocity.y > 0f) _velocity.y *= 0.5f;
        if (logTransitions) Debug.Log($"[Motor] CutJump velY {beforeY:F2}→{_velocity.y:F2}");
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
        if (logInputs) Debug.Log("[Motor] ZeroHorizontal");
    }

    private string DumpState()
    {
        return $"state[g={_curGravity:F2} term={_curTerminal:F2} vel=({_velocity.x:F2},{_velocity.y:F2},{_velocity.z:F2}) " +
               $"modes: climb={_climbMode} glide={_glideMode} grounded={_cc.isGrounded}]";
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (logFrames)
            Debug.Log($"[Motor] OnControllerColliderHit: {hit.collider.name} normal={hit.normal} moveDir={hit.moveDirection}");
    }
}
