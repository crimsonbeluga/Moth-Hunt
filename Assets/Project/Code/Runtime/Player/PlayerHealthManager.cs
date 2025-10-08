// Runtime/Player/Death/PlayerHealthManager.cs
using System;
using UnityEngine;

namespace MothHunt.Player
{
    /// <summary>
    /// One-shot health model: any capture equals death.
    /// Auto-discovers PlayerDeathManager; no manual assignment needed.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealthManager : MonoBehaviour
    {
        public bool IsAlive { get; private set; } = true;

        /// <summary>Fires once when death begins.</summary>
        public event Action<string, Transform> OnDeathStarted;
        /// <summary>Fires after respawn completes.</summary>
        public event Action OnRespawned;

        // Auto-found at runtime
        private PlayerDeathManager _deathManager;

        private void Awake()
        {
            // Prefer same object
            TryGetComponent(out _deathManager);

            // Then parent/children
            if (_deathManager == null) _deathManager = GetComponentInParent<PlayerDeathManager>();
            if (_deathManager == null) _deathManager = GetComponentInChildren<PlayerDeathManager>(true);

            // Last resort: anywhere in the scene (active objects)
            if (_deathManager == null) _deathManager = FindObjectOfType<PlayerDeathManager>();

#if UNITY_EDITOR
            if (_deathManager == null)
                Debug.LogWarning("[PlayerHealthManager] Could not auto-find PlayerDeathManager. " +
                                 "Death will fall back to an instant origin reset.");
#endif
        }

        /// <summary>Call this to kill/capture the player (e.g., from an enemy grab or hazard).</summary>
        public void Capture(string cause = "Captured", Transform killer = null)
        {
            if (!IsAlive) return; // idempotent
            IsAlive = false;

            OnDeathStarted?.Invoke(cause, killer);

            if (_deathManager != null)
            {
                _deathManager.BeginDeathSequence(cause, killer, onDone: () =>
                {
                    IsAlive = true;
                    OnRespawned?.Invoke();
                });
            }
            else
            {
                // Very simple fallback if no death manager exists in the scene.
                transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                IsAlive = true;
                OnRespawned?.Invoke();
            }
        }
    }
}
