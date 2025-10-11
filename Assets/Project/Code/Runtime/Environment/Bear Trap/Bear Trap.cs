using System.Collections;
using UnityEngine;
using MothHunt.Input; // <--- NEW: access PlayerInputRouter

/// Put this on the child object that has the Trigger Collider (e.g., "BearTrap Trigger").
/// The parent can hold the visual mesh/animation.
[RequireComponent(typeof(Collider))]
public class BearTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    [Tooltip("How long the player stays stuck (seconds).")]
    [Min(0f)] public float trapDuration = 2.0f;

    [Tooltip("If true, the trap can only be triggered once.")]
    public bool oneShot = true;

    [Tooltip("Optional: Only affect this layer (-1 = any).")]
    public int onlyAffectLayer = -1;

    [Header("Optional FX")]
    public AudioSource snapSfx;
    public Animator trapAnimator; // e.g., play a "Snap" trigger

    private bool _armed = true;

    private void Reset()
    {
        // Make sure the collider is set up as a trigger.
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_armed) return;
        if (onlyAffectLayer >= 0 && other.gameObject.layer != onlyAffectLayer) return;

        var motor = other.GetComponentInParent<PlayerMotor>() ?? other.GetComponent<PlayerMotor>();
        if (!motor) return;

        StartCoroutine(TrapRoutine(motor));
        if (oneShot) _armed = false;

        // Optional feedback
        if (snapSfx) snapSfx.Play();
        if (trapAnimator) trapAnimator.SetTrigger("Snap");
    }

    private IEnumerator TrapRoutine(PlayerMotor motor)
    {
        // Snapshot current state to restore later
        float restoreMultiplier = motor.surfaceSpeedMultiplier;

        // Anchor position (XZ) at the moment of snap
        Vector3 anchor = motor.transform.position;

        // --- NEW: block ALL input for the full duration ---
        PlayerInputRouter.DisableAllInput(trapDuration);

        // Also lock motor movement for the full duration (belt-and-suspenders)
        motor.BeginInputLock(trapDuration);

        float t = Mathf.Max(0f, trapDuration);
        while (t > 0f)
        {
            // Hard-stop horizontal movement:
            motor.surfaceSpeedMultiplier = 0f;     // clamps horizontal to zero via PlayerMotor
            motor.ZeroHorizontal();                // zero any residual horizontal velocity

            // Keep the player pinned in XZ at the snap location
            Vector3 p = motor.transform.position;
            motor.transform.position = new Vector3(anchor.x, p.y, anchor.z);

            // Optional: kill upward boost if they were mid-jump
            if (motor.VerticalSpeed > 0f) motor.CutJump();

            t -= Time.deltaTime;
            yield return null;
        }

        // Restore original movement
        motor.surfaceSpeedMultiplier = restoreMultiplier;
        // Note: PlayerInputRouter unblocks automatically when time expires; motor input lock also expires.
    }
}
