using TMPro;
using UnityEngine;

// Manages the player's mana during combat.
public class ManaManager : MonoBehaviour
{
    public static ManaManager Instance;

    public float currentMana = 0f;

    [SerializeField] private TMP_Text manaText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateManaUI();
    }

    public void AddMana(float amount)
    {
        if (amount <= 0f)
            return;

        currentMana += amount;
        UpdateManaUI();
    }

    public void ResetMana()
    {
        currentMana = 0f;
        UpdateManaUI();
    }

    public bool TrySpendMana(float amount)
    {
        amount = Mathf.Max(0f, amount);

        if (currentMana >= amount)
        {
            currentMana -= amount;
            UpdateManaUI();
            return true;
        }

        return false;
    }

    public float SpendAllMana()
    {
        float spent = Mathf.Max(0f, currentMana);
        currentMana = 0f;
        UpdateManaUI();
        return spent;
    }

    private void UpdateManaUI()
    {
        if (manaText != null)
            manaText.text = "Mana: " + currentMana.ToString("0.##");
    }
}
