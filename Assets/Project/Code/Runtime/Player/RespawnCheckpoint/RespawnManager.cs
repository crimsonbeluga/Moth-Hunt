// Runtime/Systems/RespawnManager.cs
using UnityEngine;

namespace MothHunt.Systems
{
    /// <summary>
    /// Tracks the current respawn Transform (checkpoint). If none set, uses the player's initial spawn.
    /// </summary>
    [DisallowMultipleComponent]
    public class RespawnManager : MonoBehaviour
    {
        public static RespawnManager Instance { get; private set; }

        [Header("Defaults")]
        [Tooltip("Player root transform. Used to capture the initial spawn if no checkpoint yet.")]
        public Transform playerRoot;

        [Tooltip("Optional explicit initial spawn. If null, we capture playerRoot's position/rotation on Awake.")]
        public Transform initialSpawn;

        /// <summary>Current active checkpoint. Null until first checkpoint is touched; falls back to initial.</summary>
        public Transform CurrentCheckpoint { get; private set; }

        private Vector3 _initialPos;
        private Quaternion _initialRot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (playerRoot == null)
                playerRoot = GameObject.FindGameObjectWithTag("Player")?.transform;

            // Capture initial spawn
            if (initialSpawn != null)
            {
                _initialPos = initialSpawn.position;
                _initialRot = initialSpawn.rotation;
            }
            else if (playerRoot != null)
            {
                _initialPos = playerRoot.position;
                _initialRot = playerRoot.rotation;
            }
            else
            {
                _initialPos = Vector3.zero;
                _initialRot = Quaternion.identity;
                Debug.LogWarning("[RespawnManager] No playerRoot set; initial spawn defaults to world origin.");
            }
        }

        /// <summary>Called by a CheckpointTrigger when the player overlaps it.</summary>
        public void SetCheckpoint(Transform checkpoint)
        {
            if (checkpoint == null) return;
            CurrentCheckpoint = checkpoint;
            // Optional: fire an event or SFX here later
            // Debug.Log($"[RespawnManager] Checkpoint set to {checkpoint.name}");
        }

        /// <summary>Returns the current respawn transform (checkpoint if set, else a virtual 'initial spawn').</summary>
        public void GetRespawn(out Vector3 position, out Quaternion rotation)
        {
            if (CurrentCheckpoint != null)
            {
                position = CurrentCheckpoint.position;
                rotation = CurrentCheckpoint.rotation;
                return;
            }

            position = _initialPos;
            rotation = _initialRot;
        }
    }
}
