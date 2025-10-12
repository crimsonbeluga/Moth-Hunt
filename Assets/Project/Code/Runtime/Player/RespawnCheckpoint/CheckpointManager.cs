// Runtime/Gameplay/Checkpoint/CheckpointManager.cs
using UnityEngine;

[DisallowMultipleComponent]
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Debug")]
    public bool log = true;

    private Checkpoint _last;
    private Vector3 _fallback;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var player = FindObjectOfType<PlayerLife>();
        _fallback = player ? player.transform.position : Vector3.zero;
        if (log) Debug.Log($"[CheckpointManager] Fallback spawn = {_fallback}");
    }

    public void SetCheckpoint(Checkpoint cp)
    {
        if (!cp) return;
        _last = cp;
        if (log) Debug.Log($"[CheckpointManager] Last checkpoint = {cp.name}");
    }

    public Checkpoint GetLastCheckpoint() => _last;

    public Vector3 GetFallbackSpawn() => _fallback;
}
