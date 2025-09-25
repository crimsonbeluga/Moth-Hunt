using UnityEngine;

public abstract class HealthManager : MonoBehaviour
{

    [Header("Health")]
    [Min(0f)] public float maxHealth = 100f;

    [Header("Regeneration")]
    [Tooltip("How much health per second")]
    [Min(0f)] public float regenRate = 1f;
    [Tooltip("How many seconds after taking damage for regen begins")]
    [Min(0f)] public float regenDelay = 5f;

    public DeathManager deathManager;

    // Internal
    protected float _currentHealth;
    protected float _currentRegenRate;
    protected float _currentRegenDelay;


    // ------------------------------------------------------------------

    void Start()
    {
        // Make sure DeathManager is assigned
        if (deathManager == null)
        {
            Debug.LogError("DeathManager component not found on " + gameObject.name);
        }

        // Initialize health and regen values
        _currentHealth = maxHealth;
        _currentRegenRate = regenRate;
        _currentRegenDelay = 0f;
    }

    void Update()
    {
        // Regenerate health if not at max and regen delay has passed
        if (_currentHealth < maxHealth && _currentRegenDelay <= 0f)
        {
            CurrentHealth += _currentRegenRate * Time.deltaTime;
        }
        _currentRegenDelay -= Time.deltaTime; // If delay is active, count it down
    }

    // ------------------------------------------------------------------

    // Gets & Sets CurrentHealth, Override if Needed
    public virtual float CurrentHealth
    {
        get { return _currentHealth; }
        set
        {
            _currentHealth = Mathf.Clamp(value, 0f, maxHealth);

            if (_currentHealth <= 0f)
            {
                deathManager.HandleDeath();
            }
            // Optionally, can add logic for when health is over maxHealth
        }
    }

    // Gets & Sets CurrentRegenRate, Override if Needed
    public virtual float CurrentRegenRate
    {
        get { return _currentRegenRate; }
        set { _currentRegenRate = Mathf.Max(0f, value); }
    }

    // Gets & Sets CurrentRegenDelay, Override if Needed
    public virtual float CurrentRegenDelay
    {
        get { return _currentRegenDelay; }
        set { _currentRegenDelay = Mathf.Max(0f, value); }
    }

    // ------------------------------------------------------------------

    public virtual void TakeDamage(float damage)
    {
        if (damage <= 0f) return;
        CurrentHealth -= damage; // Death is handled in the setter of CurrentHealth
        HurtEffect();
        CurrentRegenDelay = regenDelay; // Reset regen delay
    }

    public virtual void TakeHeal(float amount)
    {
        if (amount <= 0f) return;
        CurrentHealth += amount; // Healing beyond maxHealth is clamped in the setter of CurrentHealth
        HealEffect();
    }

    public abstract void HurtEffect(); // Override this method in derived classes to implement specific hurt effects
    public abstract void HealEffect(); // Override this method in derived classes to implement specific heal effects
}
