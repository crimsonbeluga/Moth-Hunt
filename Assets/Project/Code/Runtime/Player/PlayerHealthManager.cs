using UnityEngine;

public class PlayerHealthManager : HealthManager
{
    // Adding a Start() & Update() method would override the base class methods, so we don't add them here

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
