// Runtime/Gameplay/Life/DeathManager.cs
using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    [Header("Timing")]
    [Min(0f)] public float preRespawnDelay = 0.35f;
    [Min(0f)] public float postRespawnGrace = 0.05f;

    [Header("Debug")]
    public bool log = true;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void HandlePlayerDeath(PlayerLife life)
    {
        if (life && log) Debug.Log("[DeathManager] HandlePlayerDeath()");
        StartCoroutine(RespawnSequence(life));
    }

    private IEnumerator RespawnSequence(PlayerLife life)
    {
        if (!life) yield break;

        if (preRespawnDelay > 0f) yield return new WaitForSeconds(preRespawnDelay);

        // Find destination
        var cp = CheckpointManager.Instance.GetLastCheckpoint();
        Vector3 pos = cp ? cp.SpawnPoint.position : CheckpointManager.Instance.GetFallbackSpawn();
        Quaternion rot = cp ? cp.SpawnPoint.rotation : Quaternion.identity;

        // Safe teleport
        life.PrepareForTeleport();
        life.TeleportTo(pos, rot, setRotation: false);
        life.FinishTeleport();

        if (postRespawnGrace > 0f) yield return new WaitForSeconds(postRespawnGrace);

        life.FinishRespawn();

        if (log) Debug.Log("[DeathManager] Respawn complete.");
    }
}
