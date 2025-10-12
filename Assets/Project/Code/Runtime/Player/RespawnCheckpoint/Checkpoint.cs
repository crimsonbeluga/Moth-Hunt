// Runtime/Gameplay/Checkpoint/Checkpoint.cs
using UnityEngine;

[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("If null, this transform is used.")]
    public Transform explicitSpawnPoint;

    public Transform SpawnPoint => explicitSpawnPoint ? explicitSpawnPoint : transform;

    [Header("Debug")]
    public bool log = false;

    void OnTriggerEnter(Collider other)
    {
        var life = other.GetComponentInParent<PlayerLife>();
        if (!life) return;

        CheckpointManager.Instance.SetCheckpoint(this);
        if (log) Debug.Log($"[Checkpoint] Activated: {name} @ {SpawnPoint.position}");
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var p = SpawnPoint.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(p, 0.15f);
        Gizmos.DrawLine(p, p + Vector3.up);
    }
#endif
}
