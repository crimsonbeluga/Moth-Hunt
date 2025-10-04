using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ControllerPushRigidbodies : MonoBehaviour
{
    [Header("Push Tuning")]
    public float pushPower = 2.5f;       // how strong the shove is
    public float upwardModifier = 0.0f;  // small lift if you want (usually 0)
    public float maxMass = 50f;          // don’t push very heavy bodies

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var rb = hit.rigidbody;
        if (rb == null || rb.isKinematic) return;
        if (rb.mass > maxMass) return;

        // Only push along the horizontal plane
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);

        // Ignore almost-vertical hits (e.g., jumping into underside)
        if (pushDir.sqrMagnitude < 0.0001f) return;

        // VelocityChange = instant shove independent of rb mass (up to maxMass gate)
        Vector3 impulse = pushDir.normalized * pushPower;
        if (upwardModifier != 0f) impulse.y += upwardModifier;

        rb.AddForce(impulse, ForceMode.VelocityChange);
    }
}
