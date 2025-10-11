using UnityEngine;

/// <summary>
/// Apply a temporary slowdown to PlayerMotor while the player is inside this trigger.
/// Put this on a GameObject with a 2D/3D Collider set to "Is Trigger".
/// </summary>
[RequireComponent(typeof(Collider))]
public class SlowingTile : MonoBehaviour
{
    [Tooltip("Multiply the player's max horizontal speed by this amount while inside.\n1 = no change, 0.5 = half speed.")]
    [Range(0f, 1f)] public float slowMultiplier = 0.5f;

    [Tooltip("Optional: only affect objects on this layer (set your Player to this layer). Leave -1 for any.")]
    public int onlyAffectLayer = -1;

    // If multiple slow tiles overlap, we’ll apply the strongest slow (the lowest multiplier).
    private float _previousMultiplier = 1f;
    private bool _applied = false;

    private void OnTriggerEnter(Collider other)
    {
        if (onlyAffectLayer >= 0 && other.gameObject.layer != onlyAffectLayer) return;

        var motor = other.GetComponentInParent<PlayerMotor>() ?? other.GetComponent<PlayerMotor>();
        if (!motor) return;

        // Save the current multiplier so we can restore the exact value on exit.
        _previousMultiplier = motor.surfaceSpeedMultiplier;

        // Apply the strongest slow among the two values (use the smaller multiplier).
        motor.surfaceSpeedMultiplier = Mathf.Min(motor.surfaceSpeedMultiplier, Mathf.Clamp01(slowMultiplier));
        _applied = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_applied) return;
        if (onlyAffectLayer >= 0 && other.gameObject.layer != onlyAffectLayer) return;

        var motor = other.GetComponentInParent<PlayerMotor>() ?? other.GetComponent<PlayerMotor>();
        if (!motor) return;

        // Restore what we had before we entered this tile.
        motor.surfaceSpeedMultiplier = _previousMultiplier;
        _applied = false;
    }

    private void OnTriggerStay(Collider other)
    {
        // Safety: ensure we keep the slow active if physics briefly toggles contacts.
        if (onlyAffectLayer >= 0 && other.gameObject.layer != onlyAffectLayer) return;

        var motor = other.GetComponentInParent<PlayerMotor>() ?? other.GetComponent<PlayerMotor>();
        if (!motor) return;

        var target = Mathf.Clamp01(slowMultiplier);
        if (motor.surfaceSpeedMultiplier > target)
        {
            motor.surfaceSpeedMultiplier = target;
        }
    }
}
