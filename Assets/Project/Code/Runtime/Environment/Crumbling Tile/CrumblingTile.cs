// Runtime/World/CrumblingTile.cs
using UnityEngine;
using System.Collections.Generic;

public class CrumblingTile : MonoBehaviour
{
    [Header("Crumble Settings")]
    [Min(0f)] public float standTimeToCrumble = 0.5f;
    public bool canReform = true;
    [Min(0f)] public float reformDelay = 2f;

    [Tooltip("If true, waits until the detect box is completely clear before reforming.")]
    public bool waitUntilClearToReform = true;

    [Header("Player Detection")]
    [Tooltip("How far upward from the tile’s surface to detect a standing player.")]
    [Min(0f)] public float detectHeightUp = 0.6f;
    [Tooltip("How far below the top surface to include in the detection box.")]
    [Min(0f)] public float detectDepthDown = 0.05f;
    [Tooltip("Padding on the XZ plane for the detect box.")]
    [Min(0f)] public float detectPadXZ = 0.05f;

    [Header("Debug")]
    public bool log = false;
    public bool logEveryFrame = false;
    public Color gizmoDetectColor = new Color(0f, 1f, 0.4f, 0.15f);
    public Color gizmoSolidColor = new Color(1f, 0.6f, 0f, 0.15f);

    // Cached
    private readonly List<Collider> _solids = new List<Collider>();
    private Renderer _renderer;

    // State
    private int _playersOver = 0;
    private bool _isSolid = true;
    private bool _isCrumbling = false;
    private Coroutine _crumbleRoutine;
    private Coroutine _reformRoutine;

    private string TagStr => $"[CrumblingTile '{name}' id={GetInstanceID()}]";

    void Awake()
    {
        CacheSolids();
        _renderer = GetComponentInChildren<Renderer>();
        SetSolid(true);

        if (log)
        {
            Debug.Log($"{TagStr} Awake. standTimeToCrumble={standTimeToCrumble}, canReform={canReform}, reformDelay={reformDelay}");
            DumpSetup();
        }
    }

    void Update()
    {
        int before = _playersOver;
        _playersOver = CountPlayersInDetectBox();

        if (logEveryFrame)
            Debug.Log($"{TagStr} Pulse playersOver={_playersOver}, was={before}, isSolid={_isSolid}, isCrumbling={_isCrumbling}");

        if (_playersOver > 0 && _isSolid && !_isCrumbling && _crumbleRoutine == null)
        {
            if (log) Debug.Log($"{TagStr} Starting crumble countdown (player detected).");
            _crumbleRoutine = StartCoroutine(CrumbleCountdown());
        }
        else if (_playersOver == 0 && _isCrumbling && _crumbleRoutine != null)
        {
            if (log) Debug.Log($"{TagStr} Cancelling crumble countdown (player left).");
            StopCoroutine(_crumbleRoutine);
            _crumbleRoutine = null;
            _isCrumbling = false;
        }
    }

    // -------------------------------------------------------
    // Crumble & Reform
    // -------------------------------------------------------

    private System.Collections.IEnumerator CrumbleCountdown()
    {
        _isCrumbling = true;
        float t = 0f;

        while (t < standTimeToCrumble)
        {
            if (_playersOver <= 0)
            {
                _isCrumbling = false;
                _crumbleRoutine = null;
                yield break;
            }

            t += Time.deltaTime;
            if (logEveryFrame) Debug.Log($"{TagStr} Crumble ticking t={t:0.000}");
            yield return null;
        }

        if (log) Debug.Log($"{TagStr} CRUMBLE now, disabling solids.");
        SetSolid(false);
        _isCrumbling = false;
        _crumbleRoutine = null;

        if (canReform)
        {
            if (_reformRoutine != null) StopCoroutine(_reformRoutine);
            _reformRoutine = StartCoroutine(ReformRoutine());
        }
    }

    private System.Collections.IEnumerator ReformRoutine()
    {
        float t = 0f;
        while (t < reformDelay)
        {
            t += Time.deltaTime;
            if (logEveryFrame) Debug.Log($"{TagStr} Reform delay ticking t={t:0.000}");
            yield return null;
        }

        // Wait until detect box is totally clear of the player
        if (waitUntilClearToReform)
        {
            if (log) Debug.Log($"{TagStr} Reform delay done. Waiting for detect box to clear...");
            int loops = 0;
            while (!IsDetectBoxClear())
            {
                loops++;
                if (logEveryFrame) Debug.Log($"{TagStr} Reform blocked, player still inside detect box. loop={loops}");
                yield return null;
            }
        }

        if (log) Debug.Log($"{TagStr} REFORM now, enabling solids.");
        SetSolid(true);
        _reformRoutine = null;
    }

    // -------------------------------------------------------
    // Detection helpers
    // -------------------------------------------------------

    private int CountPlayersInDetectBox()
    {
        Bounds b = GetSolidsBounds();

        Vector3 center = b.center;
        Vector3 size = b.size;

        size.x += detectPadXZ * 2f;
        size.z += detectPadXZ * 2f;
        size.y = detectHeightUp + detectDepthDown;

        center.y = b.max.y + (detectHeightUp * 0.5f) - (detectDepthDown * 0.5f);

        var hits = Physics.OverlapBox(center, size * 0.5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        int count = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h) continue;
            bool isPlayer = IsPlayer(h);
            if (isPlayer) count++;
        }

        return count;
    }

    private bool IsDetectBoxClear()
    {
        Bounds b = GetSolidsBounds();

        Vector3 center = b.center;
        Vector3 size = b.size;

        size.x += detectPadXZ * 2f;
        size.z += detectPadXZ * 2f;
        size.y = detectHeightUp + detectDepthDown;

        center.y = b.max.y + (detectHeightUp * 0.5f) - (detectDepthDown * 0.5f);

        var hits = Physics.OverlapBox(center, size * 0.5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h && IsPlayer(h))
                return false;
        }

        return true;
    }

    private bool IsPlayer(Collider c)
    {
        var pm = c ? c.GetComponentInParent<PlayerMotor>() : null;
        return pm != null;
    }

    // -------------------------------------------------------
    // Setup & State
    // -------------------------------------------------------

    private void CacheSolids()
    {
        _solids.Clear();
        var own = GetComponents<Collider>();
        var children = GetComponentsInChildren<Collider>(true);
        AddSolids(own, "own");
        AddSolids(children, "children");

        if (_solids.Count == 0)
        {
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            _solids.Add(bc);
            if (log) Debug.Log($"{TagStr} No solids found, auto-added BoxCollider.");
        }
    }

    private void AddSolids(Collider[] arr, string where)
    {
        foreach (var c in arr)
        {
            if (!c || c.isTrigger) continue;
            if (!_solids.Contains(c))
            {
                _solids.Add(c);
                if (log) Debug.Log($"{TagStr} Found solid in {where}: {c.name} on {c.gameObject.name}");
            }
        }
    }

    private void SetSolid(bool solid)
    {
        _isSolid = solid;

        foreach (var col in _solids)
        {
            if (!col) continue;
            col.enabled = solid;
        }

        if (_renderer)
        {
            var mr = _renderer as MeshRenderer;
            if (mr && mr.material.HasProperty("_Color"))
            {
                var c = mr.material.color;
                c.a = solid ? 1f : 0.5f;
                mr.material.color = c;
            }
        }
    }

    private Bounds GetSolidsBounds()
    {
        Bounds? acc = null;
        foreach (var c in _solids)
        {
            if (!c) continue;
            acc = acc == null ? c.bounds : Encapsulate(acc.Value, c.bounds);
        }
        return acc ?? new Bounds(transform.position, Vector3.one * 0.5f);
    }

    private Bounds Encapsulate(Bounds a, Bounds b)
    {
        a.Encapsulate(b.min);
        a.Encapsulate(b.max);
        return a;
    }

    private void DumpSetup()
    {
        Debug.Log($"{TagStr} Setup dump:");
        foreach (var s in _solids)
        {
            if (!s) continue;
            var b = s.bounds;
            Debug.Log($"{TagStr} Solid: {s.name} enabled={s.enabled} center={b.center} size={b.size}");
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw solids
        Gizmos.color = gizmoSolidColor;
        var sb = GetSolidsBounds();
        Gizmos.DrawCube(sb.center, sb.size);
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
        Gizmos.DrawWireCube(sb.center, sb.size);

        // Draw detect box
        Bounds b = sb;
        Vector3 center = b.center;
        Vector3 size = b.size;
        size.x += detectPadXZ * 2f;
        size.z += detectPadXZ * 2f;
        size.y = detectHeightUp + detectDepthDown;
        center.y = b.max.y + (detectHeightUp * 0.5f) - (detectDepthDown * 0.5f);

        Gizmos.color = gizmoDetectColor;
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(0f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
