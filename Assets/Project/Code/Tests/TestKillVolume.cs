// Runtime/Gameplay/Testing/TestKillVolume.cs
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class TestKillVolume : MonoBehaviour
{
    [Tooltip("Message to show in logs for why the player died.")]
    public string reason = "test kill volume";

    [Tooltip("Log when we capture the player.")]
    public bool log = true;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // make it a trigger by default
    }

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("[TestKillVolume] Collider is not a trigger. Setting isTrigger = true.");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var life = other.GetComponentInParent<PlayerLife>();
        if (!life) return;

        if (log) Debug.Log($"[TestKillVolume] Capturing player from '{name}' (reason: {reason})");
        life.Capture(reason);
    }

    // Optional: if you want rigid collisions instead of triggers, also support OnCollisionEnter.
    void OnCollisionEnter(Collision collision)
    {
        var life = collision.collider.GetComponentInParent<PlayerLife>();
        if (!life) return;

        if (log) Debug.Log($"[TestKillVolume] Capturing player via collision from '{name}' (reason: {reason})");
        life.Capture(reason);
    }
}
