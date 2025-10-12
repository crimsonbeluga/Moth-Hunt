// Runtime/Gameplay/Life/PlayerLife.cs
using UnityEngine;
using MothHunt.Input; // PlayerInputRouter

[DisallowMultipleComponent]
public class PlayerLife : MonoBehaviour
{
    public enum LifeState { Alive, Captured }

    [Header("Debug")]
    public bool log = true;

    public LifeState State { get; private set; } = LifeState.Alive;

    CharacterController _cc;
    PlayerMotor _motor;

    void Awake()
    {
        TryGetComponent(out _cc);
        TryGetComponent(out _motor);
        if (log) Debug.Log("[PlayerLife] Awake");
    }

    public void Capture(string reason = "")
    {
        if (State == LifeState.Captured) return;
        State = LifeState.Captured;

        if (log) Debug.Log($"[PlayerLife] CAPTURED reason='{reason}'");

        // Hard block all inputs immediately (indefinite until EnableAllInput)
        PlayerInputRouter.DisableAllInput(-1f);

        // Freeze motor movement & steering
        if (_motor)
        {
            _motor.ZeroVelocity();
            _motor.BeginInputLock(9999f); // very long; will be cleared on respawn
        }

        DeathManager.Instance.HandlePlayerDeath(this);
    }

    // ---- Called by DeathManager during respawn flow ----
    public void PrepareForTeleport()
    {
        if (_cc) _cc.enabled = false;
        if (_motor) _motor.ZeroVelocity();
    }

    public void TeleportTo(Vector3 pos, Quaternion rot, bool setRotation = false)
    {
        transform.position = pos;
        if (setRotation) transform.rotation = rot;
    }

    public void FinishTeleport()
    {
        if (_cc) _cc.enabled = true;
    }

    public void FinishRespawn()
    {
        // Reset motor to known-good baseline
        if (_motor)
        {
            _motor.ResetForRespawn();   // clears mode flags, momentum, glide/climb, input lock timer, etc.
            _motor.BeginInputLock(0f);  // ensure no residual lock
        }

        // Re-enable inputs globally
        PlayerInputRouter.EnableAllInput();

        State = LifeState.Alive;
        if (log) Debug.Log("[PlayerLife] ALIVE (controls restored)");
    }
}
