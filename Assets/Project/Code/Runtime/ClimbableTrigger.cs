// ClimbableTrigger.cs (on the trigger child)
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClimbableTrigger : MonoBehaviour
{
    public Climbable climbable; // reference on the parent

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (!climbable) climbable = GetComponentInParent<Climbable>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMotor m))
        {
            m.SetClimbCandidate(climbable);
            Debug.Log($"[ClimbTrigger] ENTER {other.name} -> {climbable?.name}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMotor m))
        {
            m.ClearClimbCandidate(climbable);
            Debug.Log($"[ClimbTrigger] EXIT {other.name} -> {climbable?.name}");
        }
    }
}
