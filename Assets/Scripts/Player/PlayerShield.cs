using UnityEngine;
using TMPro;

// This script manages the player's shield.
//
// Shield works like temporary hit blocks.
// If the player has shield, incoming damage removes 1 shield instead of HP.
public class PlayerShield : MonoBehaviour
{
    // Singleton reference so other scripts can call:
    // PlayerShield.Instance.AddShield()
    // PlayerShield.Instance.TryBlock()
    public static PlayerShield Instance;

    [Header("Shield")]

    // Current shield amount.
    public int currentShield = 0;

    // Maximum shield allowed.
    [SerializeField] private int maxShield = 3;

    [Header("UI")]

    // Text that displays current shield.
    [SerializeField] private TMP_Text shieldText;

    private void Awake()
    {
        // Set up singleton instance.
        Instance = this;
    }

    private void Start()
    {
        // Make sure shield UI is correct at the start.
        UpdateUI();
    }

    // Adds shield to the player.
    public void AddShield(int amount)
    {
        // Ignore zero or negative shield.
        if (amount <= 0)
            return;

        // Add shield.
        currentShield += amount;

        // Clamp shield so it stays between 0 and maxShield.
        currentShield = Mathf.Clamp(currentShield, 0, maxShield);

        // Refresh UI.
        UpdateUI();
    }

    // Tries to block an incoming hit.
    //
    // Returns true if shield blocked the hit.
    // Returns false if the player had no shield.
    public bool TryBlock()
    {
        if (currentShield > 0)
        {
            // Spend 1 shield to block the hit.
            currentShield--;

            // Refresh UI.
            UpdateUI();

            return true;
        }

        return false;
    }

    // Clears all shield.
    public void ClearShield()
    {
        currentShield = 0;
        UpdateUI();
    }

    // Updates shield UI text.
    private void UpdateUI()
    {
        if (shieldText != null)
            shieldText.text = "Shield: " + currentShield;
    }
}