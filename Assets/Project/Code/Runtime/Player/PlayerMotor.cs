using System.Runtime.CompilerServices;
using UnityEngine;

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
    [Tooltip("Time (s) to build momentum upward while truly sprinting on ground.")]
    public float momentumRiseTime = 0.35f;
    [Tooltip("Time (s) to decay momentum when not truly sprinting.")]
    public float momentumFallTime = 0.60f;
    [Tooltip("Seconds after jump to ramp airMoveSpeed from walk to momentum-based target (used by Jump).")]
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

    // -1 = facing left (default), +1 = facing right
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

    private Climbable _climbCandidate;
    public bool HasClimbCandidate => _climbCandidate != null;
    public Climbable CurrentClimbable => _climbCandidate;

    public float VerticalSpeed => _velocity.y;
    /// <summary>Absolute planar speed the motor is currently applying this frame.</summary>
    public float CurrentPlanarSpeed => Mathf.Abs(useZForHorizontal ? _velocity.z : _velocity.x);

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _spriteRenderer = GetComponent<SpriteRenderer>(); // same object
        Mode_Walk();
        Debug.Log($"[Motor] Awake -> {DumpState()}");
    }

    void Update() => Tick(Time.deltaTime);

    public void Tick(float dt)
    {
        if (logFrames) Debug.Log($"[Motor] Tick START dt={dt:F4}  {DumpState()}");

        // Horizontal intent -> velocity under current cap
        float horiz = _desiredX * _curMaxSpeedX;
        if (useZForHorizontal) { _velocity.x = 0f; _velocity.z = horiz; }
        else { _velocity.x = horiz; _velocity.z = 0f; }
        if (logInputs) Debug.Log($"[Motor] HORIZ desiredX={_desiredX:F2} cap={_curMaxSpeedX:F2} -> vel=({_velocity.x:F2},{_velocity.z:F2})");

        // Vertical/gravity handling
        float beforeY = _velocity.y;
        if (_climbMode)
        {
            _velocity.y = _desiredY * climbSpeed;
            if (logFrames) Debug.Log($"[Motor] CLIMB vertical: desiredY={_desiredY:F2} -> {beforeY:F2}→{_velocity.y:F2}");
        }
        else
        {
            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f; // small stick-to-ground bias
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

        // Move
        Vector3 delta = _velocity * dt;
        CollisionFlags flags = _cc.Move(delta);

        if (logFrames) Debug.Log($"[Motor] Move delta={delta} flags={flags} grounded={_cc.isGrounded}");
        // ==== HEAD-BUMP: cancel upward motion immediately ====
        if ((flags & CollisionFlags.Above) != 0 && _velocity.y > 0f)
        {
            // Kill upward velocity and start falling now
            _velocity.y = -2f; // small downward bias so we don't re-stick to the ceiling

            // If you were gliding (unlikely on a jump bump), end it so gravity is normal
            if (_glideMode)
            {
                _glideMode = false;
                SetGravity(normalGravity, terminalFallSpeed, "HeadBump");
            }

            if (logTransitions) Debug.Log("[Motor] Head bump -> cancel jump, start falling.");
        }
        // === Momentum update (never builds in air/walk; only on true ground sprint) ===
        TickSprintMomentumBySpeed(dt);

        // === NEW: when grounded, keep airMoveSpeed in sync so future airborne cap is correct ===
        if (_cc.isGrounded)
            SyncAirMoveSpeedWhileGrounded();

        if (logFrames) Debug.Log($"[Motor] Tick END   {DumpState()}");
    }

    // -------- Momentum (speed-based, persistent) --------

    /// <summary>
    /// True if we should be allowed to BUILD momentum this frame:
    /// - On ground
    /// - Actually moving
    /// - Current cap equals sprint speed (proxy for being in Sprint state)
    /// </summary>
    private bool CanBuildMomentum()
    {
        bool moving = Mathf.Abs(_desiredX) > 0.05f;
        bool sprintCap = _curMaxSpeedX >= sprintSpeed * 0.98f; // cheap proxy for Sprint mode
        return _cc.isGrounded && moving && sprintCap;
    }

    /// <summary>
    /// Move sprint momentum toward a target ratio under clear rules:
    /// - If truly sprinting on ground: rise toward current speed ratio (>= walkRatio).
    /// - Else if moving (walk/crouch or in air): decay toward walkRatio (to keep air at walk).
    /// - Else (idle): decay toward 0.
    /// </summary>
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
        if (build)
        {
            // Only build toward actual speed ratio, but never below walkRatio while sprinting.
            target = Mathf.Max(walkRatio, speedRatio);
        }
        else
        {
            // Not truly sprinting:
            // - if still moving, momentum should sit at walk level
            // - if idle, momentum decays toward 0
            target = moving ? walkRatio : 0f;

            // Hard cap: when not building, never exceed walk-level momentum
            if (_sprintMomentum > walkRatio)
                _sprintMomentum = Mathf.Max(walkRatio, _sprintMomentum - (dt / fall));
        }

        // Approach target
        if (_sprintMomentum < target)
            _sprintMomentum = Mathf.Min(target, _sprintMomentum + (dt / rise));
        else if (_sprintMomentum > target)
            _sprintMomentum = Mathf.Max(target, _sprintMomentum - (dt / fall));
    }

    /// <summary>
    /// When grounded, precompute the "next air" cap:
    /// - If truly sprinting (building) -> airMoveSpeed tracks momentum (up to sprint)
    /// - Otherwise -> force walk, so walking/idle resets future air cap
    /// </summary>
    private void SyncAirMoveSpeedWhileGrounded()
    {
        if (_climbMode || _glideMode) return;

        if (CanBuildMomentum())
        {
            airMoveSpeed = Mathf.Lerp(walkSpeed, sprintSpeed, _sprintMomentum);
        }
        else
        {
            airMoveSpeed = walkSpeed; // critical: clears stale "10" after landing or when walking
        }
    }

    /// <summary>While airborne (not climbing/gliding), keep CC horizontal cap synced to airMoveSpeed.</summary>
    public void ApplyAirMoveCap()
    {
        if (!_climbMode && !_glideMode)
            _curMaxSpeedX = airMoveSpeed;
    }

    // -------- Knobs --------
    public void SetHorizontalInput(float x01)
    {
        _desiredX = Mathf.Clamp(x01, -1f, 1f);
        if (logInputs) Debug.Log($"[Motor] SetHorizontalInput -> {_desiredX:F2}");

        // flip sprite based on input
        if (_spriteRenderer)
        {
            if (_desiredX > 0.05f)      // moving right
            {
                _spriteRenderer.flipX = true;
                _faceDir = +1;
            }
            else if (_desiredX < -0.05f) // moving left
            {
                _spriteRenderer.flipX = false;
                _faceDir = -1;
            }
            // if idle, do nothing — keep last facing
        }
    }
    public void SetVerticalClimbInput(float y01) { _desiredY = Mathf.Clamp(y01, -1f, 1f); if (logInputs) Debug.Log($"[Motor] SetVerticalClimbInput -> {_desiredY:F2}"); }
    public void SetGravity(float g, float term, [CallerMemberName] string caller = null)
    { if (logGravityChanges) Debug.Log($"[Motor] SetGravity by '{caller}'  g:{_curGravity:F2}→{g:F2}  term:{_curTerminal:F2}→{term:F2}"); _curGravity = g; _curTerminal = term; }

    // -------- Modes --------
    public void Mode_Walk() { _climbMode = false; _glideMode = false; _curMaxSpeedX = walkSpeed; SetGravity(normalGravity, terminalFallSpeed); airMoveSpeed = walkSpeed; if (logTransitions) Debug.Log($"[Motor] Mode_Walk -> {DumpState()}"); }
    public void Mode_Sprint() { _climbMode = false; _glideMode = false; _curMaxSpeedX = sprintSpeed; SetGravity(normalGravity, terminalFallSpeed); /* airMoveSpeed will track via Sync */ if (logTransitions) Debug.Log($"[Motor] Mode_Sprint -> {DumpState()}"); }
    public void Mode_Crouch() { _climbMode = false; _glideMode = false; _curMaxSpeedX = crouchSpeed; SetGravity(normalGravity, terminalFallSpeed); airMoveSpeed = walkSpeed; if (logTransitions) Debug.Log($"[Motor] Mode_Crouch -> {DumpState()}"); }
    public void Mode_AirMove() { _climbMode = false; _glideMode = false; _curMaxSpeedX = airMoveSpeed; if (logTransitions) Debug.Log($"[Motor] Mode_AirMove -> {DumpState()}"); }

    public void Mode_Glide()
    {
        if (_cc.isGrounded) { if (logTransitions) Debug.Log("[Motor] Mode_Glide requested but grounded."); return; }
        _climbMode = false; _glideMode = true; _curMaxSpeedX = glideHorizontalSpeed;
        SetGravity(glideGravity, glideFallSpeed, nameof(Mode_Glide));
        float beforeY = _velocity.y;
        if (_velocity.y < glideFallSpeed) _velocity.y = glideFallSpeed;
        else if (_velocity.y > 0f) _velocity.y = 0f;
        if (logTransitions) Debug.Log($"[Motor] Mode_Glide ENTER  velY {beforeY:F2}→{_velocity.y:F2}");
    }
    public void End_Glide() { _glideMode = false; SetGravity(normalGravity, terminalFallSpeed, nameof(End_Glide)); if (logTransitions) Debug.Log($"[Motor] End_Glide -> {DumpState()}"); }
    public void Mode_Climb() { _glideMode = false; _climbMode = true; float beforeY = _velocity.y; _velocity.y = 0f; if (logTransitions) Debug.Log($"[Motor] Mode_Climb ENTER  velY {beforeY:F2}→{_velocity.y:F2}"); }
    public void End_Climb() { _climbMode = false; SetGravity(normalGravity, terminalFallSpeed, nameof(End_Climb)); _desiredY = 0f; if (logTransitions) Debug.Log($"[Motor] End_Climb -> {DumpState()}"); }

    // -------- One-shots --------
    public void DoJump()
    {
        if (_climbMode) { if (logTransitions) Debug.Log("[Motor] DoJump while climbing -> End_Climb first."); End_Climb(); }
        if (!_cc.isGrounded) { if (logTransitions) Debug.Log("[Motor] DoJump ignored (not grounded)."); return; }

        float jumpV = Mathf.Sqrt(Mathf.Abs(2f * normalGravity * jumpHeight));
        float beforeY = _velocity.y;
        _velocity.y = jumpV;
        _glideMode = false;
        SetGravity(normalGravity, terminalFallSpeed, nameof(DoJump));
        if (logTransitions) Debug.Log($"[Motor] DoJump -> velY {beforeY:F2}→{_velocity.y:F2}");

        // === Pre-open pass-through if a platform is right above ===
        if (TryGetComponent<PlayerPlatformPass>(out var pass))
        {
            float feetY = transform.position.y + _cc.center.y - (_cc.height * 0.5f) + _cc.skinWidth;
            Vector3 origin = new Vector3(transform.position.x, feetY + 0.05f, transform.position.z);
            float radius = _cc.radius * 0.95f;
            float distance = 0.4f;

            if (Physics.SphereCast(origin, radius, Vector3.up, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                var plat = hit.collider ? hit.collider.GetComponentInParent<OneTwoWayPlatform>() : null;
                if (plat && plat.solid == hit.collider && plat.jumpUpThrough)
                {
                    Debug.Log($"[Motor] Pre-open: spherecast found '{plat.name}' above @ y={plat.TopYWorld:F3}.", this);
                    pass.PassUpThrough(plat.solid, plat.TopYWorld);
                }
            }
        }
    }

    public void CutJump()
    {
        float beforeY = _velocity.y;
        if (_velocity.y > 0f) _velocity.y *= 0.5f;
        if (logTransitions) Debug.Log($"[Motor] CutJump {beforeY:F2}→{_velocity.y:F2}");
    }

    // -------- Drop-through (robust) --------
    public bool TryDropThrough(float duration = 0.30f)
    {
        if (!_cc.isGrounded)
        {
            if (logTransitions) Debug.Log("[Motor] TryDropThrough: not grounded → ignore.", this);
            return false;
        }

        if (!TryGetComponent<PlayerPlatformPass>(out var pass))
        {
            Debug.LogWarning("[Motor] TryDropThrough: PlayerPlatformPass missing.", this);
            return false;
        }

        // Feet position
        float feetY = transform.position.y + _cc.center.y - (_cc.height * 0.5f) + _cc.skinWidth;
        Vector3 feet = new Vector3(transform.position.x, feetY, transform.position.z);

        // 1) Overlap capsule to catch the slab we're touching
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

        // 2) Fallback: spherecast down
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

        if (!foundPlat)
        {
            if (logTransitions) Debug.Log("[Motor] TryDropThrough: no one/two-way slab beneath.", this);
            return false;
        }

        if (logTransitions) Debug.Log($"[Motor] TryDropThrough: opening '{foundPlat.name}' for {duration:F2}s.", this);
        pass.DropDownThrough(foundCol, duration);

        // Strong downward nudge to actually depart contact this frame
        if (_velocity.y > -5f) _velocity.y = -5f;

        return true;
    }

    // -------- Climb candidate --------
    public void SetClimbCandidate(Climbable c) { _climbCandidate = c; Debug.Log($"[Motor] ClimbCandidate = {(_climbCandidate ? _climbCandidate.name : "null")}"); }
    public void ClearClimbCandidate(Climbable c) { if (_climbCandidate == c) { _climbCandidate = null; Debug.Log("[Motor] ClimbCandidate cleared"); } }

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

    // === Underside fallback for jump-up ===
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (logFrames) Debug.Log($"[Motor] OnControllerColliderHit: {hit.collider.name} normal={hit.normal} moveDir={hit.moveDirection}");

        var plat = hit.collider.GetComponentInParent<OneTwoWayPlatform>();
        if (plat && plat.solid == hit.collider && plat.jumpUpThrough)
        {
            bool underside = hit.normal.y < -0.5f; // underside face
            bool goingUp = _velocity.y > 0f;
            if (underside && goingUp && TryGetComponent<PlayerPlatformPass>(out var pass))
            {
                Debug.Log("[Motor] UNDERSIDE contact while going up → grant pass-through.", this);
                pass.PassUpThrough(plat.solid, plat.TopYWorld);
            }
        }
    }
}
