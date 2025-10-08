// BouncePad.cs
using UnityEngine;

public class BouncePad : MonoBehaviour
{
    public enum AimMode { WorldUp, TransformUp, Custom }

    [Header("Bounce Core")]
    [Min(0f), Tooltip("Target apex height in meters, under the pad's chosen gravity.")]
    public float bounceHeight = 4.5f;

    [Tooltip("Lock horizontal input for this long after launch (seconds).")]
    [Min(0f)] public float inputLockTime = 0.08f;

    [Header("Gravity Override (optional)")]
    public bool overrideGravity = false;
    [Tooltip("Temporary gravity during the bounce arc (negative).")]
    public float padGravity = -30f;
    [Tooltip("Temporary terminal fall speed during the bounce arc (negative).")]
    public float padTerminal = -40f;

    [Header("Horizontal Handling")]
    [Range(0f, 1f), Tooltip("Scale of current horizontal speed preserved on launch.")]
    public float preserveHorizontal = 1f;

    [Tooltip("Extra horizontal (m/s) added. + = right (or +Z if using Z-for-horizontal).")]
    public float injectHorizontal = 0f;

    [Tooltip("Clamp resulting horizontal speed magnitude (m/s). 0 = no clamp.")]
    [Min(0f)] public float clampHorizontalMax = 0f;

    [Header("Aim / Direction")]
    public AimMode aimMode = AimMode.WorldUp;
    [Tooltip("Used only if AimMode=Custom; will be normalized.")]
    public Vector3 customDirection = Vector3.up;

    [Header("Rules")]
    [Tooltip("Require downward entry (prevents pogo while rising).")]
    public bool requireDownwardEntry = true;

    [Tooltip("Min downward speed to trigger (m/s).")]
    public float minDownwardSpeed = -0.5f;

    [Tooltip("Grace after leaving ground where bounce still triggers (coyote).")]
    [Min(0f)] public float coyoteWindow = 0.06f;

    [Tooltip("Fail if there is an immediate ceiling directly above the pad.")]
    public bool headroomCheck = true;

    [Tooltip("Headroom check distance (meters) for immediate ceiling.")]
    [Min(0.05f)] public float headroomProbeDistance = 0.5f;

    [Tooltip("If the pad is a thin one-way platform, grant pass-through on this bounce frame.")]
    public bool oneWayCompat = true;

    [Tooltip("Ignore extra triggers for this many seconds after bouncing.")]
    [Min(0f)] public float retriggerCooldown = 0.10f;

    [Header("Debug")]
    public bool log;

    private float _nextReadyTime;
    private BoxCollider _trigger;

    // Track last grounded time (uses PlayerInputRouter.IsGrounded published by PlayerMotor)
    private float _lastGroundedTime = -999f;

    private void Awake()
    {
        _trigger = GetComponent<BoxCollider>();
        if (!_trigger) _trigger = gameObject.AddComponent<BoxCollider>();
        _trigger.isTrigger = true;
    }

    private void Reset()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 0.25f, 0f);
        box.size = new Vector3(1.5f, 0.5f, 1.0f);
    }

    private void Update()
    {
        if (MothHunt.Input.PlayerInputRouter.IsGrounded)
            _lastGroundedTime = Time.time;
    }

    private void OnTriggerEnter(Collider other) => TryBounce(other, enterEvent: true);
    private void OnTriggerStay(Collider other) => TryBounce(other, enterEvent: false);

    private void TryBounce(Collider other, bool enterEvent)
    {
        if (Time.time < _nextReadyTime) return;
        if (!other || !other.TryGetComponent<PlayerMotor>(out var motor)) return;

        // Direction rule: downward or within coyote window
        bool downwardOK = !requireDownwardEntry || (motor.VerticalSpeed <= minDownwardSpeed);
        bool coyoteOK = (Time.time - _lastGroundedTime) <= coyoteWindow;
        if (!(downwardOK || coyoteOK)) return;

        // Optional headroom check along aim vector
        if (headroomCheck)
        {
            Vector3 up = GetAimVector();
            if (Physics.Raycast(transform.position + Vector3.up * 0.05f, up.normalized, out var hit,
                                headroomProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                return; // avoid instant head-bump
            }
        }

        // Gravity override
        float? g = overrideGravity ? padGravity : (float?)null;
        float? term = overrideGravity ? padTerminal : (float?)null;

        // Launch vertically to target apex (consistent with jump math)
        motor.LaunchUpToHeight(bounceHeight, cancelGlide: true, cancelClimb: true,
                               gravityOverride: g, terminalOverride: term);

        // Lock input FIRST so motor doesn't overwrite the push on this frame
        if (inputLockTime > 0f) motor.BeginInputLock(inputLockTime);

        // Horizontal handling: preserve, inject, clamp
        float inject = injectHorizontal;
        motor.NudgeHorizontal(preserveHorizontal, inject);

        if (clampHorizontalMax > 0f)
            motor.ClampHorizontal(clampHorizontalMax);

        // Optional one-way compatibility: mark pass-through this frame
        if (oneWayCompat && other.TryGetComponent<PlayerPlatformPass>(out var pass))
        {
            var motorType = typeof(PlayerMotor);
            var field = motorType.GetField("_passGrantedThisFrame",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(motor, true);
        }

        _nextReadyTime = Time.time + retriggerCooldown;

        if (log)
        {
            Debug.Log($"[BouncePad] Bounce h={bounceHeight:F2}m, g={(g.HasValue ? g.Value.ToString("F2") : "motor")}, " +
                      $"pres={preserveHorizontal:F2}, inject={injectHorizontal:F2}, clamp={clampHorizontalMax:F2}, " +
                      $"aim={aimMode}, coyoteOK={coyoteOK}, downOK={downwardOK}");
        }
    }

    private Vector3 GetAimVector()
    {
        switch (aimMode)
        {
            case AimMode.TransformUp: return transform.up;
            case AimMode.Custom: return customDirection.sqrMagnitude > 0.0001f ? customDirection.normalized : Vector3.up;
            case AimMode.WorldUp:
            default: return Vector3.up;
        }
    }
}
