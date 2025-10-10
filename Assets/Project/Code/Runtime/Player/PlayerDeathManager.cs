using UnityEngine;

public class PlayerDeathManager : DeathManager
{
    private CheckpointManager _checkpointManager;
    private PlayerHealthManager _healthManager;

    private void Start()
    {
        // Find the CheckpointManager in the scene
        _checkpointManager = FindFirstObjectByType<CheckpointManager>();
        _healthManager = GetComponent<PlayerHealthManager>();
    }


    public override void HandleDeath()
    {
        Debug.LogWarning("Player has died. Implement respawn or game over logic here.");
        // Can put animation , sound effects, etc. here. Probably put in another method and call it here.

        Vector3 deathPosition = transform.position;
        Vector3 checkpointPosition = _checkpointManager.GetCurrentCheckpoint().transform.position;

        // Log the checkpoint information for debugging
        Debug.Log("Respawning at checkpoint: " + _checkpointManager.GetCurrentCheckpoint().name);
        Debug.Log("Respawn Position: " + checkpointPosition);
        Debug.Log("Player Position Before Respawn: " + deathPosition);

        // If we didn't have this while loop, it would return player to where they died.
        while (GetComponentInParent<Transform>().position != checkpointPosition)
        {
            // Respawn at the last checkpoint
            GetComponentInParent<Transform>().position = checkpointPosition;
        }
        
        _healthManager.ResetHealth();

        // Log the new position for verification
        Debug.Log("Player Position After Respawn: " + GetComponentInParent<Transform>().position);
    }
}
