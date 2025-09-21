using UnityEngine;

/// <summary>
/// Manages the player's health and interactions with damage sources.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health points.")]
    public int maxHealth = 3;
    
    [Header("Damage Settings")]
    [Tooltip("Amount of damage taken per hit.")]
    public int damagePerHit = 1;
    
    private int currentHealth;

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Reduces health by the specified damage amount.
    /// </summary>
    /// <param name="damage">Amount of damage to take.</param>
    public void TakeDamage(int damage = -1)
    {
        // Use damagePerHit if no damage amount is specified
        int actualDamage = damage == -1 ? damagePerHit : damage;
        
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(0, currentHealth); // Ensure health doesn't go below 0
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Lose a life in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife();
            }
        }
    }

    /// <summary>
    /// Handles player death.
    /// </summary>
    private void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver("Krishna has fallen!");
        }
        
        // Optionally disable the player GameObject
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Gets the current health value.
    /// </summary>
    /// <returns>The current health points.</returns>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Gets the maximum health value.
    /// </summary>
    /// <returns>The maximum health points.</returns>
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Restores health by the specified amount.
    /// </summary>
    /// <param name="healAmount">Amount of health to restore.</param>
    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Min(maxHealth, currentHealth); // Ensure health doesn't exceed max
    }

    /// <summary>
    /// Checks if the player is still alive.
    /// </summary>
    /// <returns>True if current health is greater than 0.</returns>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}