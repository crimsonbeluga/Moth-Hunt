using UnityEngine;

public class PlayerDeathManager : DeathManager
{

    public override void HandleDeath()
    {
        Debug.LogWarning("Player has died. Implement respawn or game over logic here.");
        // Example: Respawn the player at a checkpoint
        // transform.position = respawnPoint.position;
        // healthManager.ResetHealth();
    }
}
