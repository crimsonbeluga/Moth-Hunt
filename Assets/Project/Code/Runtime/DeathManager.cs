using UnityEngine;

public abstract class DeathManager : MonoBehaviour
{
    public HealthManager healthManager;
    
    public abstract void HandleDeath();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
