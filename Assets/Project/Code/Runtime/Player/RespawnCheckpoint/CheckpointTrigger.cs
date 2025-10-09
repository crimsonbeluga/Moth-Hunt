// Runtime/World/CheckpointTrigger.cs
using UnityEngine;
using MothHunt.Systems;

namespace MothHunt.World
{
    /// <summary>
    /// Place in scene as a simple trigger volume. When the Player enters, it becomes the active checkpoint.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CheckpointTrigger : MonoBehaviour
    {
        [Tooltip("If true, only triggers for colliders tagged 'Player'.")]
        public bool requirePlayerTag = true;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true; // make setup painless
        }

        private void OnEnable()
        {
            CheckpointManager.Instance?.Register(transform);
        }

        private void OnDisable()
        {
            CheckpointManager.Instance?.Unregister(transform);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (requirePlayerTag && !other.CompareTag("Player")) return;
            if (RespawnManager.Instance == null) return;

            RespawnManager.Instance.SetCheckpoint(transform);
            CheckpointManager.Instance?.SetActive(transform);
            // Optional: SFX, VFX, UI ping here
        }
    }
}
