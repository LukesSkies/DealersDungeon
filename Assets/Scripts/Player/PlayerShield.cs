using TMPro;
using UnityEngine;

// Manages the player's shield.
public class PlayerShield : MonoBehaviour
{
    public static PlayerShield Instance;

    [Header("Shield")]
    public int currentShield = 0;
    [SerializeField] private int maxShield = 30;

    [Header("UI")]
    [SerializeField] private TMP_Text shieldText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddShield(int amount)
    {
        if (amount <= 0)
            return;

        currentShield += amount;
        currentShield = Mathf.Clamp(currentShield, 0, maxShield);
        UpdateUI();
    }

    public int AbsorbDamage(int damage)
    {
        if (damage <= 0)
            return 0;

        if (currentShield <= 0)
            return damage;

        int blocked = Mathf.Min(currentShield, damage);
        currentShield -= blocked;
        UpdateUI();

        return damage - blocked;
    }

    public bool TryBlock()
    {
        if (currentShield <= 0)
            return false;

        currentShield--;
        currentShield = Mathf.Clamp(currentShield, 0, maxShield);
        UpdateUI();
        return true;
    }

    public void ClearShield()
    {
        currentShield = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (shieldText != null)
            shieldText.text = "Shield: " + currentShield;
    }
}
