using UnityEngine;

public class PlayerDeathManager : DeathManager
{

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public override void HandleDeath()
    {
        Debug.Log("Player has died. Implement respawn or game over logic here.");
        // Example: Respawn the player at a checkpoint
        // transform.position = respawnPoint.position;
        // healthManager.ResetHealth();
    }
}
