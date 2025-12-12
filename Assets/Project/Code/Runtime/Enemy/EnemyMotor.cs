using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyMotor : MonoBehaviour
{
    // Variables
    [Header("Axes")]
    public bool useZForHorizontal = false;

    [Header("Speeds")]
    [Min(0f)] public float walkSpeed = 4f;
    [Min(0f)] public float sprintSpeed = 7f;
    [Min(0f)] public float crouchSpeed = 2f;
    [Min(0f)] public float airMoveSpeed = 4f;
    [Min(0f)] public float glideHorizontalSpeed = 3f;
    [Min(0f)] public float climbSpeed = 3f;

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

    private float _desiredX;
    private float _desiredY;
    private float _curMaxSpeedX;
    private float _curGravity;
    private float _curTerminal;

    private CharacterController _cc;
    private Vector3 _velocity;

    public float VerticalSpeed => _velocity.y;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        Mode_Walk();
        Debug.Log($"[Motor] Awake -> {DumpState()}");

        //testing collission/contact for connnecting to player
        _cc.providesContacts = true;
       
    }



    // -------- Knobs --------
    public void SetHorizontalInput(float x01) { _desiredX = Mathf.Clamp(x01, -1f, 1f); if (logInputs) Debug.Log($"[Motor] SetHorizontalInput -> {_desiredX:F2}"); }
    public void SetVerticalClimbInput(float y01) { _desiredY = Mathf.Clamp(y01, -1f, 1f); if (logInputs) Debug.Log($"[Motor] SetVerticalClimbInput -> {_desiredY:F2}"); }
    public void SetGravity(float g, float term, [CallerMemberName] string caller = null)
    { if (logGravityChanges) Debug.Log($"[Motor] SetGravity by '{caller}'  g:{_curGravity:F2}→{g:F2}  term:{_curTerminal:F2}→{term:F2}"); _curGravity = g; _curTerminal = term; }

    // -------- Modes --------
    public void Mode_Walk() { _curMaxSpeedX = walkSpeed; SetGravity(normalGravity, terminalFallSpeed); if (logTransitions) Debug.Log($"[Motor] Mode_Walk -> {DumpState()}"); }
    public void Mode_Sprint() { _curMaxSpeedX = sprintSpeed; SetGravity(normalGravity, terminalFallSpeed); if (logTransitions) Debug.Log($"[Motor] Mode_Sprint -> {DumpState()}"); }
    public void Mode_Crouch() { _curMaxSpeedX = crouchSpeed; SetGravity(normalGravity, terminalFallSpeed); if (logTransitions) Debug.Log($"[Motor] Mode_Crouch -> {DumpState()}"); }


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


    public void ZeroHorizontal() { if (useZForHorizontal) _velocity.z = 0f; else _velocity.x = 0f; if (logInputs) Debug.Log("[Motor] ZeroHorizontal"); }

    private string DumpState()
    {
        return $"state[g={_curGravity:F2} term={_curTerminal:F2} vel=({_velocity.x:F2},{_velocity.y:F2},{_velocity.z:F2}) " +
               $"modes: grounded={_cc.isGrounded}]";
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
