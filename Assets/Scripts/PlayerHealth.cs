using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Manages the player's health pool, UI slider, damage/healing, and death (scene reload).
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("UI")]
    [Tooltip("Health bar slider — automatically synced on damage/heal")]
    public Slider healthBar;

    private void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    // Reduces health by the given amount. Triggers death at zero.
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Restores health up to the maximum.
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }
    }

    // Reloads the current scene on death. Replace with a proper game-over
    // screen or respawn system as the project matures.
    private void Die()
    {
        Debug.Log("Player died!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
