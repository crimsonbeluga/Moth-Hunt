using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleCapsuleResizer : MonoBehaviour
{
    [System.Serializable]
    public struct Profile { public float height, radius; public Vector3 center; public float stepOffset; }

    // Match STAND to your default CC values
    public Profile stand = new Profile { height = 2.79f, radius = 0.50f, center = new Vector3(-0.04f, -0.13f, 0f), stepOffset = 0.30f };
    public Profile crouch = new Profile { height = 1.20f, radius = 0.50f, center = new Vector3(-0.04f, 0.17f, 0f), stepOffset = 0.30f };
    public Profile glide = new Profile { height = 1.10f, radius = 0.45f, center = new Vector3(-0.04f, 0.12f, 0f), stepOffset = 0.00f };

    [Header("Tuning")]
    [Tooltip("Small downward move applied when shrinking so you remain grounded this frame.")]
    public float shrinkGroundSnap = 0.02f;

    [Tooltip("If true, stepOffset from the profile is applied. If false, keep the current CC stepOffset.")]
    public bool applyProfileStepOffset = true;

    [Header("Headroom Check")]
    [Tooltip("Layers considered solid for standing headroom. EXCLUDE the Player layer.")]
    public LayerMask headCheckMask = ~0;
    [Tooltip("A tiny padding to avoid incidental self-intersections in the query.")]
    public float headroomPadding = 0.005f;

    CharacterController _cc;

    // Feet lock: local Y of the bottom of the capsule, captured at Awake
    float _feetLocalY;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        // Capture feet position from CURRENT CC so it matches what you see in scene
        float currentFeet = _cc.center.y - _cc.height * 0.5f;
        float standFeet = stand.center.y - stand.height * 0.5f;
        _feetLocalY = Mathf.Abs(currentFeet) < 1000f ? currentFeet : standFeet;

        // Start standing with feet locked
        ApplyFeetLocked(stand, isShrink: false);
    }

    public void Stand() => ApplyFeetLocked(stand, isShrink: stand.height < _cc.height - 1e-4f);
    public void Crouch() => ApplyFeetLocked(crouch, isShrink: crouch.height < _cc.height - 1e-4f);
    public void Glide() => ApplyFeetLocked(glide, isShrink: glide.height < _cc.height - 1e-4f);

    // ---------- Core (feet-locked, no automatic headroom checks) ----------
    void ApplyFeetLocked(Profile p, bool isShrink)
    {
        float centerY = _feetLocalY + p.height * 0.5f;

        _cc.radius = p.radius;
        _cc.height = Mathf.Max(p.height, p.radius * 2f + 0.01f);
        _cc.center = new Vector3(p.center.x, centerY, p.center.z);

        if (applyProfileStepOffset)
            _cc.stepOffset = Mathf.Max(0f, p.stepOffset);

        if (isShrink && shrinkGroundSnap > 0f)
            _cc.Move(Vector3.down * shrinkGroundSnap);
    }

    // ---------- Headroom probe (for Brain to call before exiting crouch/sprint) ----------
    public bool HasHeadroomForStand() => HasHeadroomFor(stand);

    public bool HasHeadroomFor(Profile p)
    {
        // Build a world-space capsule where the STAND collider would be (feet-locked)
        float radius = Mathf.Max(0.001f, p.radius - headroomPadding);

        // Local bottom/top points of the capsule's cylindrical section
        Vector3 bottomLocal = new Vector3(p.center.x, _feetLocalY + radius, p.center.z);
        Vector3 topLocal = new Vector3(p.center.x, _feetLocalY + p.height - radius, p.center.z);

        Vector3 a = transform.TransformPoint(bottomLocal);
        Vector3 b = transform.TransformPoint(topLocal);

        // Query world for solid overlaps (ignore triggers)
        var hits = Physics.OverlapCapsule(a, b, radius, headCheckMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h || h.isTrigger) continue;
            if (h == _cc) continue;                      // skip our own CC
            if (h.transform.IsChildOf(transform)) continue; // skip our children, if any
            // Any other collider means no headroom
            return false;
        }
        return true;
    }
}
