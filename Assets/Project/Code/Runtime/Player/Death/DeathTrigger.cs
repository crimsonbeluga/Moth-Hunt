// Runtime/Player/Death/DeathTrigger.cs
using UnityEngine;
using MothHunt.Player;

namespace MothHunt.World
{
    [RequireComponent(typeof(Collider))]
    public class DeathTrigger : MonoBehaviour
    {
        [Tooltip("Optional: override cause string reported to the death system.")]
        public string cause = "Hazard";

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other) return;

            // Find the player's health/death stack on the entering object or its parents
            var health = other.GetComponentInParent<PlayerHealthManager>();
            if (!health) return;

            // NEW: respect post-respawn invulnerability window
            var pdm = health.GetComponent<PlayerDeathManager>();
            if (pdm == null) pdm = health.GetComponentInParent<PlayerDeathManager>();
            if (pdm && pdm.IsInvulnerable) return;

            health.Capture(cause, transform);
        }
    }
}
