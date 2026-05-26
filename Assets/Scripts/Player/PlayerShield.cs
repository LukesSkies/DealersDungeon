using UnityEngine;
using TMPro;

public class PlayerShield : MonoBehaviour
{
    public static PlayerShield Instance;

    public int currentShield = 0;
    [SerializeField] private TMP_Text shieldText;

    private void Awake()
    {
        Instance = this;
    }

    public void AddShield(int amount)
    {
        currentShield += amount;
        currentShield = Mathf.Clamp(currentShield, 0, 3);

        UpdateUI();
    }

    public bool TryBlock()
    {
        if (currentShield > 0)
        {
            currentShield--;
            UpdateUI();
            return true;
        }

        return false;
    }

    private void UpdateUI()
    {
        if (shieldText != null)
            shieldText.text = "Shield: " + currentShield;
    }
}