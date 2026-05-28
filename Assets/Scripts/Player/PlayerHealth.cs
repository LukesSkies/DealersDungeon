using UnityEngine;
using TMPro;
using UnityEngine.UI;

// This script manages the player's health.
//
// It controls:
// - max HP
// - current HP
// - taking damage
// - healing
// - HP UI text
// - HP slider
// - death / game over
public class PlayerHealth : MonoBehaviour
{
    // Singleton reference so other scripts can call:
    // PlayerHealth.Instance.TakeDamage()
    // PlayerHealth.Instance.Heal()
    public static PlayerHealth Instance;

    [Header("Stats")]

    // Maximum player HP.
    public int maxHP = 30;

    // Current player HP.
    private int currentHP;

    // True after the player dies.
    //
    // This prevents Die() being called multiple times.
    private bool isDead = false;

    [Header("UI")]

    // Text that displays player HP.
    [SerializeField] private TMP_Text hpText;

    // Slider that displays player HP.
    [SerializeField] private Slider hpSlider;

    private void Awake()
    {
        // Set up singleton instance.
        Instance = this;
    }

    private void Start()
    {
        // Start at full HP.
        currentHP = maxHP;

        // Set slider maximum value if assigned.
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        // Update HP display.
        UpdateUI();
    }

    // Damages the player.
    public void TakeDamage(int amount)
    {
        // Do nothing if the player is already dead.
        if (isDead)
            return;

        // Ignore zero or negative damage.
        if (amount <= 0)
            return;

        // Future: reflect hooks here.

        // Let shield block the hit first.
        //
        // If TryBlock() returns true, the shield absorbed the damage.
        if (PlayerShield.Instance != null && PlayerShield.Instance.TryBlock())
            return;

        // Subtract HP.
        currentHP -= amount;

        // Clamp HP so it never goes below 0 or above maxHP.
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // Refresh UI.
        UpdateUI();

        // Die if HP reaches 0.
        if (currentHP <= 0)
            Die();
    }

    // Heals the player.
    public void Heal(int amount)
    {
        // Do nothing if the player is dead.
        if (isDead)
            return;

        // Ignore zero or negative healing.
        if (amount <= 0)
            return;

        // Add HP.
        currentHP += amount;

        // Clamp HP so it never goes above maxHP.
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // Refresh UI.
        UpdateUI();

        Debug.Log("Healed: " + amount + " | HP: " + currentHP + "/" + maxHP);
    }

    // Returns current HP.
    public int GetCurrentHP()
    {
        return currentHP;
    }

    // Returns true if the player is dead.
    public bool IsDead()
    {
        return isDead;
    }

    // Updates HP text and slider.
    private void UpdateUI()
    {
        if (hpText != null)
            hpText.text = $"HP: {currentHP}/{maxHP}";

        if (hpSlider != null)
            hpSlider.value = currentHP;
    }

    // Kills the player and triggers Game Over.
    private void Die()
    {
        // Prevent death from happening more than once.
        if (isDead)
            return;

        isDead = true;

        Debug.Log("Player Died");

        // Stop camera bob if the player has it.
        CameraBob cameraBob = GetComponentInChildren<CameraBob>();

        if (cameraBob != null)
            cameraBob.StopWalking();

        // Clear the current hand if combat is active.
        if (HandManager.Instance != null)
            HandManager.Instance.ClearHand();

        // Trigger the game over state.
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
        else if (GameOverUI.Instance != null)
            GameOverUI.Instance.Show();
    }
}