// Runtime/Player/PlayerMotor.cs
using System.Runtime.CompilerServices;
using UnityEngine;
using MothHunt.Input;   // so we can set PlayerInputRouter.IsGrounded

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Axes")]
    public bool useZForHorizontal = false;

    [Header("Speeds")]
    [Min(0f)] public float walkSpeed = 4f;
    [Min(0f)] public float sprintSpeed = 7f;
    [Min(0f)] public float crouchSpeed = 2f;
    [Min(0f)] public float airMoveSpeed = 4f;
    [Min(0f)] public float glideHorizontalSpeed = 3f;
    [Min(0f)] public float climbSpeed = 3f;

    [Header("Sprint Momentum (speed-based)")]
    public float momentumRiseTime = 0.35f;
    public float momentumFallTime = 0.60f;
    public float airSpeedRampTime = 0.20f;

    [SerializeField, Range(0f, 1f)]
    private float _sprintMomentum; // 0..1, persistent
    public float SprintMomentum => _sprintMomentum;

    [Header("Jump / Gravity")]
    public float jumpHeight = 2.2f;
    public float normalGravity = -30f;
    public float glideGravity = -6f;
    public float terminalFallSpeed = -40f;
    public float glideFallSpeed = -8f;

    [Header("Debug")]
    public bool logFrames = false;
    public bool logTransitions = true;
    public bool logGravityChanges = true;
    public bool logInputs = false;

    [Header("Visual")]
    private SpriteRenderer _spriteRenderer;

    private int _faceDir = -1;

    private float _desiredX;
    private float _desiredY;
    private float _curMaxSpeedX;
    private float _curGravity;
    private float _curTerminal;

    private CharacterController _cc;
    private Vector3 _velocity;
    private bool _climbMode;
    private bool _glideMode;

    private bool _passGrantedThisFrame;

    // --- Input lock for external impulses (bounce pads, etc.) ---
    private float _inputLockUntil = -999f;
    public bool InputLocked => Time.time < _inputLockUntil;
    public void BeginInputLock(float seconds)
    {
        _inputLockUntil = Mathf.Max(_inputLockUntil, Time.time + Mathf.Max(0f, seconds));
        _desiredX = 0f; // clear residual steering while locked
    }

    private Climbable _climbCandidate;
    public bool HasClimbCandidate => _climbCandidate != null;
    public Climbable CurrentClimbable => _climbCandidate;

    public float VerticalSpeed => _velocity.y;
    public float CurrentPlanarSpeed => Mathf.Abs(useZForHorizontal ? _velocity.z : _velocity.x);
    public bool IsMovingHorizontally(float eps = 0.01f) => CurrentPlanarSpeed > eps;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        Mode_Walk();
        Debug.Log($"[Motor] Awake -> {DumpState()}");
    }

    void Update() => Tick(Time.deltaTime);

    public void Tick(float dt)
    {
        if (logFrames) Debug.Log($"[Motor] Tick START dt={dt:F4}  {DumpState()}");

        _passGrantedThisFrame = false;

        float horiz = _desiredX * _curMaxSpeedX;

        // ---> DO NOT overwrite horizontal when input-locked (so bounce push can stick)
        if (useZForHorizontal)
        {
            _velocity.x = 0f;
            if (!InputLocked) _velocity.z = horiz;
        }
        else
        {
            _velocity.z = 0f;
            if (!InputLocked) _velocity.x = horiz;
        }

        if (_climbMode)
        {
            _velocity.y = _desiredY * climbSpeed;
        }
        else
        {
            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f;
                if (_glideMode) { _glideMode = false; if (logTransitions) Debug.Log("[Motor] Glide auto-ended because grounded."); }
                if (_curGravity != normalGravity || _curTerminal != terminalFallSpeed)
                {
                    if (logGravityChanges) Debug.Log($"[Motor] Grounded -> resetting gravity to NORMAL");
                    SetGravity(normalGravity, terminalFallSpeed, "Grounded");
                }
            }
            else
            {
                if (_glideMode && (Mathf.Abs(_curGravity - glideGravity) > 0.001f || Mathf.Abs(_curTerminal - glideFallSpeed) > 0.001f))
                    Debug.LogWarning($"[Motor] WARNING: glideMode but gravity/terminal mismatch");

                _velocity.y += _curGravity * dt;
                if (_velocity.y < _curTerminal) _velocity.y = _curTerminal;
            }
        }

        Vector3 delta = _velocity * dt;
        CollisionFlags flags = _cc.Move(delta);

        if ((flags & CollisionFlags.Above) != 0 && _velocity.y > 0f)
        {
            if (_passGrantedThisFrame)
            {
                if (logTransitions) Debug.Log("[Motor] Head bump SUPPRESSED (pass-through granted this frame).");
            }
            else
            {
                _velocity.y = -2f;
                if (_glideMode) { _glideMode = false; SetGravity(normalGravity, terminalFallSpeed, "HeadBump"); }
                if (logTransitions) Debug.Log("[Motor] Head bump -> cancel jump, start falling.");
            }
        }

        TickSprintMomentumBySpeed(dt);
        if (_cc.isGrounded) SyncAirMoveSpeedWhileGrounded();

        // publish grounding each frame for other systems (Throw/BouncePad)
        PlayerInputRouter.IsGrounded = _cc.isGrounded;

        if (logFrames) Debug.Log($"[Motor] Tick END   {DumpState()}");
    }

    private bool CanBuildMomentum()
    {
        bool moving = Mathf.Abs(_desiredX) > 0.05f;
        bool sprintCap = _curMaxSpeedX >= sprintSpeed * 0.98f;
        return _cc.isGrounded && moving && sprintCap;
    }

    public void TickSprintMomentumBySpeed(float dt)
    {
        float denom = Mathf.Max(0.0001f, sprintSpeed);
        float speedRatio = Mathf.Clamp01(CurrentPlanarSpeed / denom);
        float walkRatio = Mathf.Clamp01(walkSpeed / denom);

        float rise = Mathf.Max(0.0001f, momentumRiseTime);
        float fall = Mathf.Max(0.0001f, momentumFallTime);

        bool moving = Mathf.Abs(_desiredX) > 0.05f;
        bool build = CanBuildMomentum();

        float target;
        if (build) target = Mathf.Max(walkRatio, speedRatio);
        else
        {
            target = moving ? walkRatio : 0f;
            if (_sprintMomentum > walkRatio)
                _sprintMomentum = Mathf.Max(walkRatio, _sprintMomentum - (dt / fall));
        }

        if (_sprintMomentum < target) _sprintMomentum = Mathf.Min(target, _sprintMomentum + (dt / rise));
        else if (_sprintMomentum > target) _sprintMomentum = Mathf.Max(target, _sprintMomentum - (dt / fall));
    }

    private void SyncAirMoveSpeedWhileGrounded()
    {
        if (_climbMode || _glideMode) return;
        if (CanBuildMomentum()) airMoveSpeed = Mathf.Lerp(walkSpeed, sprintSpeed, _sprintMomentum);
        else airMoveSpeed = walkSpeed;
    }

    public void ApplyAirMoveCap()
    {
        if (!_climbMode && !_glideMode) _curMaxSpeedX = airMoveSpeed;
    }

    // -------- Knobs --------
    public void SetHorizontalInput(float x01)
    {
        // Respect input lock from bounce pads or other systems
        if (InputLocked) x01 = 0f;

        _desiredX = Mathf.Clamp(x01, -1f, 1f);
        if (_spriteRenderer)
        {
            if (_desiredX > 0.05f) { _spriteRenderer.flipX = true; _faceDir = +1; }
            else if (_desiredX < -0.05f) { _spriteRenderer.flipX = false; _faceDir = -1; }
        }
    }
    public void SetVerticalClimbInput(float y01) { _desiredY = Mathf.Clamp(y01, -1f, 1f); }
    public void SetGravity(float g, float term, [CallerMemberName] string caller = null)
    { if (logGravityChanges) Debug.Log($"[Motor] SetGravity by '{caller}'  g:{_curGravity:F2}→{g:F2}  term:{_curTerminal:F2}→{term:F2}"); _curGravity = g; _curTerminal = term; }

    // -------- Modes --------
    public void Mode_Walk() { _climbMode = false; _glideMode = false; _curMaxSpeedX = walkSpeed; SetGravity(normalGravity, terminalFallSpeed); airMoveSpeed = walkSpeed; }
    public void Mode_Sprint() { _climbMode = false; _glideMode = false; _curMaxSpeedX = sprintSpeed; SetGravity(normalGravity, terminalFallSpeed); }
    public void Mode_Crouch() { _climbMode = false; _glideMode = false; _curMaxSpeedX = crouchSpeed; SetGravity(normalGravity, terminalFallSpeed); airMoveSpeed = walkSpeed; }
    public void Mode_AirMove() { _climbMode = false; _glideMode = false; _curMaxSpeedX = airMoveSpeed; }

    public void Mode_Glide()
    {
        if (_cc.isGrounded) { if (logTransitions) Debug.Log("[Motor] Mode_Glide requested but grounded."); return; }
        _climbMode = false; _glideMode = true; _curMaxSpeedX = glideHorizontalSpeed;
        SetGravity(glideGravity, glideFallSpeed, nameof(Mode_Glide));
        if (_velocity.y < glideFallSpeed) _velocity.y = glideFallSpeed;
        else if (_velocity.y > 0f) _velocity.y = 0f;
    }
    public void End_Glide() { _glideMode = false; SetGravity(normalGravity, terminalFallSpeed, nameof(End_Glide)); }
    public void Mode_Climb() { _glideMode = false; _climbMode = true; _velocity.y = 0f; }
    public void End_Climb() { _climbMode = false; SetGravity(normalGravity, terminalFallSpeed, nameof(End_Climb)); _desiredY = 0f; }

    // -------- One-shots --------
    public void DoJump()
    {
        if (_climbMode) End_Climb();
        if (!_cc.isGrounded) return;

        float jumpV = Mathf.Sqrt(Mathf.Abs(2f * normalGravity * jumpHeight));
        _velocity.y = jumpV;
        _glideMode = false;
        SetGravity(normalGravity, terminalFallSpeed, nameof(DoJump));

        if (TryGetComponent<PlayerPlatformPass>(out var pass))
        {
            float feetY = transform.position.y + _cc.center.y - (_cc.height * 0.5f) + _cc.skinWidth;
            Vector3 origin = new Vector3(transform.position.x, feetY + 0.05f, transform.position.z);
            float radius = _cc.radius * 0.95f;
            float distance = Mathf.Max(0.6f, _cc.height * 0.5f + 0.2f);

            if (Physics.SphereCast(origin, radius, Vector3.up, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                var plat = hit.collider ? hit.collider.GetComponentInParent<OneTwoWayPlatform>() : null;
                if (plat && plat.solid == hit.collider && plat.jumpUpThrough)
                {
                    pass.PassUpThrough(plat.solid, plat.TopYWorld);
                    _passGrantedThisFrame = true;
                }
            }
        }
    }

    public void CutJump()
    {
        if (_velocity.y > 0f) _velocity.y *= 0.5f;
    }

    public void DoInterect() { }

    // -------- External Impulses (bounce pads etc.) --------
    public void LaunchUpToHeight(float heightMeters, bool cancelGlide = true, bool cancelClimb = true,
                                 float? gravityOverride = null, float? terminalOverride = null)
    {
        if (cancelClimb && _climbMode) End_Climb();
        float g = gravityOverride ?? normalGravity; // negative
        float v = Mathf.Sqrt(Mathf.Abs(2f * g * Mathf.Max(0f, heightMeters)));
        _velocity.y = v;

        if (cancelGlide) _glideMode = false;
        SetGravity(g, terminalOverride ?? terminalFallSpeed, nameof(LaunchUpToHeight));
    }

    public void NudgeHorizontal(float preserveScale01, float injectMetersPerSec)
    {
        bool useZ = useZForHorizontal;
        float cur = useZ ? _velocity.z : _velocity.x;
        float next = (cur * Mathf.Clamp01(preserveScale01)) + injectMetersPerSec;
        if (useZ) _velocity.z = next; else _velocity.x = next;
    }

    public void ClampHorizontal(float maxMetersPerSec)
    {
        maxMetersPerSec = Mathf.Max(0f, maxMetersPerSec);
        if (maxMetersPerSec <= 0f) { ZeroHorizontal(); return; }

        bool useZ = useZForHorizontal;
        float val = useZ ? _velocity.z : _velocity.x;
        val = Mathf.Clamp(val, -maxMetersPerSec, maxMetersPerSec);
        if (useZ) _velocity.z = val; else _velocity.x = val;
    }

    // -------- Drop-through (robust) --------
    public bool TryDropThrough(float duration = 0.30f)
    {
        if (!_cc.isGrounded) return false;
        if (!TryGetComponent<PlayerPlatformPass>(out var pass)) return false;

        float feetY = transform.position.y + _cc.center.y - (_cc.height * 0.5f) + _cc.skinWidth;
        Vector3 feet = new Vector3(transform.position.x, feetY, transform.position.z);

        float r = _cc.radius * 0.98f;
        Vector3 p1 = feet + Vector3.up * 0.02f;
        Vector3 p2 = feet - Vector3.up * 0.06f;
        var overlaps = Physics.OverlapCapsule(p1, p2, r, ~0, QueryTriggerInteraction.Ignore);

        OneTwoWayPlatform foundPlat = null;
        Collider foundCol = null;

        if (overlaps != null && overlaps.Length > 0)
        {
            foreach (var c in overlaps)
            {
                if (!c) continue;
                var plat = c.GetComponentInParent<OneTwoWayPlatform>();
                if (plat && plat.solid == c && plat.jumpDownThrough)
                {
                    foundPlat = plat; foundCol = c; break;
                }
            }
        }

        if (!foundPlat)
        {
            Vector3 origin = feet + Vector3.up * 0.05f;
            float castDist = 0.6f;
            if (Physics.SphereCast(origin, r, Vector3.down, out var hit, castDist, ~0, QueryTriggerInteraction.Ignore))
            {
                var plat = hit.collider ? hit.collider.GetComponentInParent<OneTwoWayPlatform>() : null;
                if (plat && plat.solid == hit.collider && plat.jumpDownThrough)
                {
                    foundPlat = plat; foundCol = hit.collider;
                }
            }
        }

        if (!foundPlat) return false;

        pass.DropDownThrough(foundCol, duration);
        if (_velocity.y > -5f) _velocity.y = -5f;
        return true;
    }

    // -------- Climb candidate --------
    public void SetClimbCandidate(Climbable c) { _climbCandidate = c; }
    public void ClearClimbCandidate(Climbable c) { if (_climbCandidate == c) _climbCandidate = null; }

    public bool IsGrounded() => _cc.isGrounded;
    public bool IsClimbing() => _climbMode;
    public bool IsGliding() => _glideMode;

    public void ZeroHorizontal()
    {
        if (useZForHorizontal) _velocity.z = 0f;
        else _velocity.x = 0f;
    }

    // --- NEW: called by PlayerDeathManager after teleport to clear state/locks ---
    public void ZeroVelocity()
    {
        _velocity = Vector3.zero;
        if (useZForHorizontal) _velocity.x = 0f; else _velocity.z = 0f;
    }

    // --- NEW: full reset for consistent post-respawn behaviour ---
    public void ResetForRespawn()
    {
        _desiredX = 0f;
        _desiredY = 0f;
        _climbMode = false;
        _glideMode = false;
        _sprintMomentum = 0f;
        _passGrantedThisFrame = false;
        _inputLockUntil = -999f;   // release any external input locks

        ZeroVelocity();
        Mode_Walk();               // normal gravity/terminal + walk caps
    }

    private string DumpState()
    {
        return $"state[g={_curGravity:F2} term={_curTerminal:F2} vel=({_velocity.x:F2},{_velocity.y:F2},{_velocity.z:F2}) " +
               $"modes: climb={_climbMode} glide={_glideMode} grounded={_cc.isGrounded}]";
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var plat = hit.collider.GetComponentInParent<OneTwoWayPlatform>();
        if (plat && plat.solid == hit.collider && plat.jumpUpThrough)
        {
            bool underside = hit.normal.y < -0.5f;
            bool goingUp = _velocity.y > 0f;
            if (underside && goingUp && TryGetComponent<PlayerPlatformPass>(out var pass))
            {
                pass.PassUpThrough(plat.solid, plat.TopYWorld);
                _passGrantedThisFrame = true;
            }
        }
    }
}
