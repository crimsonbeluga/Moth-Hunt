using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class OneTwoWayPlatform : MonoBehaviour
{
    [Header("Behavior")]
    public bool jumpUpThrough = true;     // come up from below
    public bool jumpDownThrough = true;   // drop from top

    [Header("References")]
    [Tooltip("Solid collider for the slab (non-trigger). If null, uses this object's BoxCollider.")]
    public BoxCollider solid;

    [Header("Debug")]
    public bool debug = false;

    public float TopYWorld => solid ? solid.bounds.max.y : float.NegativeInfinity;

    void Reset()
    {
        solid = GetComponent<BoxCollider>();
        if (solid) solid.isTrigger = false;
        if (debug) Debug.Log($"[OTP] Reset. solid='{solid?.name}', isTrigger={solid?.isTrigger}");
    }

    void OnValidate()
    {
        if (!solid) solid = GetComponent<BoxCollider>();
        if (solid) solid.isTrigger = false;
        if (debug)
            Debug.Log($"[OTP] OnValidate. jumpUp={jumpUpThrough}, jumpDown={jumpDownThrough}, TopY={TopYWorld:F3}", this);
    }

    void OnEnable()
    {
        if (debug && solid)
            Debug.Log($"[OTP] Enabled. solid='{solid.name}', TopY={TopYWorld:F3}", this);
    }
}
