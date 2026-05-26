using UnityEngine;
using TMPro;

// This script manages the player's mana.
//
// It controls:
// - current mana amount
// - adding mana
// - spending mana
// - resetting mana after combat
// - updating the mana UI text
public class ManaManager : MonoBehaviour
{
    // Singleton reference so other scripts can call it.
    public static ManaManager Instance;

    // The player's current mana amount.
    //
    // This is a float so cards can cost decimal values if needed.
    public float currentMana = 0f;

    // UI text that displays the current mana.
    //
    // Assign this in the Inspector.
    [SerializeField] private TMP_Text manaText;

    private void Awake()
    {
        // Set up the singleton instance.
        Instance = this;
    }

    private void Start()
    {
        // Update the UI at the start so it shows the correct starting mana.
        UpdateManaUI();
    }

    // Adds mana to the player.
    //
    // Example:
    // AddMana(2);
    // increases current mana by 2.
    public void AddMana(float amount)
    {
        currentMana += amount;

        // Refresh the UI after changing mana.
        UpdateManaUI();

        // Debug message so you can see mana changes in the Console.
        Debug.Log("Mana: " + currentMana);
    }

    // Resets mana back to zero.
    //
    // EnemyManager calls this when combat ends.
    public void ResetMana()
    {
        currentMana = 0f;

        // Refresh the UI after resetting mana.
        UpdateManaUI();
    }

    // Updates the mana UI text.
    private void UpdateManaUI()
    {
        // Only update the text if one has been assigned.
        if (manaText != null)
        {
            // 0.## means:
            // - show whole numbers normally
            // - show up to 2 decimal places if needed
            //
            // Example:
            // 3 becomes "3"
            // 3.5 becomes "3.5"
            // 3.25 becomes "3.25"
            manaText.text = "Mana: " + currentMana.ToString("0.##");
        }
    }

    // Tries to spend mana.
    //
    // Returns true if the player had enough mana.
    // Returns false if the player did not have enough mana.
    public bool TrySpendMana(float amount)
    {
        // If the player has enough mana, spend it.
        if (currentMana >= amount)
        {
            currentMana -= amount;

            // Refresh the UI after spending mana.
            UpdateManaUI();

            return true;
        }

        // Not enough mana, so nothing is spent.
        return false;
    }
}