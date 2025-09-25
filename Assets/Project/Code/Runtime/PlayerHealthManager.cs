using UnityEngine;

public class PlayerHealthManager : HealthManager
{
    



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        print(_currentHealth);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void HurtEffect()
    {
        // Implement player-specific hurt effect here
        Debug.Log("Player hurt effect triggered!");
    }

    public override void HealEffect()
    {
        // Implement player-specific death effect here
        Debug.Log("Player death effect triggered!");
    }
}
