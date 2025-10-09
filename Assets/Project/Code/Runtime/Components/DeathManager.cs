using UnityEngine;

public abstract class DeathManager : MonoBehaviour
{
    // We might need this, but let's comment it out for now
    // public HealthManager healthManager;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    // Abstract method to handle death, must be implemented by subclasses
    public abstract void HandleDeath();
}
