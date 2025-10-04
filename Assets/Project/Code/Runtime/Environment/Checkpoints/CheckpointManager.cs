using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [Tooltip("List of checkpoints, make sure it is in order!")]
    public List<Checkpoint> checkpoints;
    public Checkpoint currentCheckpoint;

    // ------------------------------------------------------------------

    void Start()
    {
        
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
    public void SetCheckpoint(Checkpoint checkpoint)
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
    public void SetCheckpointIndex(int index)
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
