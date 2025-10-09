using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CheckpointManager : MonoBehaviour
{
    [Tooltip("List of checkpoints, make sure it is in order!")]
    public List<Checkpoint> checkpoints;
    public Checkpoint currentCheckpoint;

    private List<CheckpointTrigger> checkpointTriggers;

    private static CheckpointManager instance;

    private GameObject checkpointGroup;

    // ------------------------------------------------------------------

    private void Awake()
    {
        // Find Checkpoints
        checkpointGroup = GameObject.Find("Checkpoints");

        // Get all checkpoints from the group
        if (checkpointGroup != null)
        {
            checkpoints = new List<Checkpoint>(checkpointGroup.GetComponentsInChildren<Checkpoint>());
        }
        else
        {
            Debug.LogError("No GameObject named 'Checkpoints' found in the scene.");
        }

    }

    
    void Start()
    {
        // Could probably have a group of checkpoints as a child of an empty object and then grab them to put them onto a list

    }

    void Update()
    {
        
    }

    // ------------------------------------------------------------------
    public Checkpoint GetCurrentCheckpoint()
    {
        return currentCheckpoint;
    }

    int GetCheckpointIndex(Checkpoint checkpoint)
    {
        return checkpoints.IndexOf(checkpoint);
    }

    int GetLastCheckpointIndex()
    {
        return checkpoints.Count - 1;
    }

    int GetFirstCheckpointIndex()
    {
        return 0;
    }

    // If you want to set the checkpoint directly
    public void SetCurrentCheckpoint(Checkpoint checkpoint)
    {
        if (GetCheckpointIndex(checkpoint) != -1)
        {
            currentCheckpoint = checkpoint;
        }
        else
        {
            Debug.LogError("Checkpoint not found in the list of checkpoints.");
        }
    }

    // If you want to set the checkpoint by index
    public void SetCurrentCheckpointIndex(int index)
    {
        if (index >= 0 && index < checkpoints.Count)
        {
            currentCheckpoint = checkpoints[index];
        }
        else
        {
            Debug.LogError("Index out of range for checkpoints list.");
        }
    }
}
