using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the CheckpointManager that handles checkpoint logic")]
    public CheckpointManager checkpointManager;
    [Tooltip("Collider that acts as the trigger area for the checkpoint")]
    public Collider triggerCollider;
    [Tooltip("Reference to the Checkpoint this trigger is associated with")]
    public Checkpoint checkpoint;

    // ------------------------------------------------------------------

    void Start()
    {
        if (checkpointManager == null)
        {
            Debug.LogError("CheckpointManager component not found on " + gameObject.name);
        }

        if (triggerCollider == null)
        {
            Debug.LogError("Trigger Collider not assigned on " + gameObject.name);
        }
        else
        {
            triggerCollider.isTrigger = true; // Ensure the collider is set as a trigger
        }

        if (checkpoint == null)
        {
            Debug.LogError("Checkpoint reference not assigned on " + gameObject.name);
        }
    }

    void Update()
    {

    }

    // ------------------------------------------------------------------

    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            // Set the current checkpoint in the CheckpointManager
            checkpointManager.SetCheckpoint(checkpoint);
            Debug.Log("Checkpoint reached: " + checkpoint.name);
        }
    }
}
