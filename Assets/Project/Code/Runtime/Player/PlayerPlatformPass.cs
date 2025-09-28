using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerPlatformPass : MonoBehaviour
{
    [Header("Debug")]
    public bool debug = false;
    [Tooltip("Extra vertical buffer when deciding we've fully crossed a platform (meters).")]
    public float crossBuffer = 0.05f;

    private CharacterController _cc;
    private Collider _playerCol;
    private readonly HashSet<Collider> _ignored = new();

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _playerCol = _cc; // CharacterController is a Collider
        if (debug) Debug.Log($"[PPP] Awake. cc.height={_cc.height:F3} center={_cc.center} skin={_cc.skinWidth:F3}", this);
    }

    public bool IsIgnoring(Collider c) => c && _ignored.Contains(c);

    // === Up-through: ignore until feet above target Y ===
    public void PassUpThrough(Collider platformCol, float targetYWorld)
    {
        if (!platformCol) { if (debug) Debug.LogWarning("[PPP] PassUpThrough: null collider"); return; }
        if (_ignored.Contains(platformCol)) { if (debug) Debug.Log("[PPP] PassUpThrough: already ignoring"); return; }

        if (debug) Debug.Log($"[PPP] PassUpThrough START → '{platformCol.name}' until feetY > {targetYWorld + crossBuffer:F3}", this);
        Physics.IgnoreCollision(_playerCol, platformCol, true);
        _ignored.Add(platformCol);
        StartCoroutine(ReEnableWhenAbove(platformCol, targetYWorld + crossBuffer));
    }

    // === Down-through: ignore for a short time ===
    public void DropDownThrough(Collider platformCol, float duration = 0.25f)
    {
        if (!platformCol) { if (debug) Debug.LogWarning("[PPP] DropDownThrough: null collider"); return; }
        if (_ignored.Contains(platformCol)) { if (debug) Debug.Log("[PPP] DropDownThrough: already ignoring"); return; }

        if (debug) Debug.Log($"[PPP] DropDownThrough START → '{platformCol.name}' for {duration:F2}s", this);
        Physics.IgnoreCollision(_playerCol, platformCol, true);
        _ignored.Add(platformCol);
        StartCoroutine(ReEnableAfterDelay(platformCol, duration));
    }

    IEnumerator ReEnableWhenAbove(Collider c, float yThreshold)
    {
        while (true)
        {
            float feetY = transform.position.y + _cc.center.y - (_cc.height * 0.5f) + _cc.skinWidth;
            if (feetY > yThreshold) break;
            yield return null;
        }
        SafeReEnable(c, "ReEnableWhenAbove");
    }

    IEnumerator ReEnableAfterDelay(Collider c, float delay)
    {
        yield return new WaitForSeconds(delay);
        SafeReEnable(c, "ReEnableAfterDelay");
    }

    void SafeReEnable(Collider c, string reason)
    {
        if (!c) { if (debug) Debug.LogWarning($"[PPP] {reason}: collider null"); return; }
        if (_ignored.Remove(c))
        {
            Physics.IgnoreCollision(_playerCol, c, false);
            if (debug) Debug.Log($"[PPP] {reason}: restored '{c.name}'", this);
        }
    }
}
