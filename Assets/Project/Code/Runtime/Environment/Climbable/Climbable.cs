using UnityEngine;

public enum ClimbType { Ladder, Vine, Pipe }

public class Climbable : MonoBehaviour
{
    public ClimbType type = ClimbType.Ladder;
    [Tooltip("Primary axis to move along while climbing.")]
    public Vector3 climbAxis = Vector3.up;
    [Tooltip("Optional point to snap the player when starting climb.")]
    public Transform snapPoint;
    [Tooltip("Pull-in distance when entering climb (optional).")]
    public float enterOffset = 0.2f;

    private void OnDrawGizmosSelected()
    {
        if (snapPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(snapPoint.position, 0.05f);
        }
    }
}
