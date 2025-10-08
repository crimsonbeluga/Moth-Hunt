// Runtime/Systems/CheckpointManager.cs
using System.Collections.Generic;
using UnityEngine;

namespace MothHunt.Systems
{
    /// <summary>
    /// Optional overseer: registers checkpoint triggers for debug/gizmos and tracks last active.
    /// </summary>
    [DisallowMultipleComponent]
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        [Header("Debug")]
        public Color checkpointColor = new Color(0f, 1f, 1f, 0.25f);
        public Color activeColor = new Color(1f, 1f, 0f, 0.35f);

        private readonly List<Transform> _checkpoints = new();
        private Transform _active;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Register(Transform t)
        {
            if (t != null && !_checkpoints.Contains(t))
                _checkpoints.Add(t);
        }

        public void Unregister(Transform t)
        {
            if (t != null) _checkpoints.Remove(t);
        }

        public void SetActive(Transform t)
        {
            _active = t;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_checkpoints == null) return;
            foreach (var cp in _checkpoints)
            {
                if (cp == null) continue;
                Gizmos.color = (cp == _active) ? activeColor : checkpointColor;
                Gizmos.DrawSphere(cp.position, 0.2f);
            }
        }
#endif
    }
}
