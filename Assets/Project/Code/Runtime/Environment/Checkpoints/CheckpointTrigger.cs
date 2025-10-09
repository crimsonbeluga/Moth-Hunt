using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Collider that acts as the trigger area for the checkpoint")]
    public Collider triggerCollider;
    [Tooltip("Reference to the Checkpoint this trigger is associated with")]
    public Checkpoint checkpoint;

    // Reference to the CheckpointManager that handles checkpoint logic
    private CheckpointManager _checkpointManager;

    // ------------------------------------------------------------------

    void Start()
    {
        if (_checkpointManager == null)
        {
            _checkpointManager = FindFirstObjectByType<CheckpointManager>();
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
            _checkpointManager.SetCurrentCheckpoint(checkpoint);
            Debug.Log("Checkpoint reached: " + checkpoint.name);
        }
    }
}
