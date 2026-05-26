using UnityEngine;
using TMPro;

public class ManaManager : MonoBehaviour
{
    public static ManaManager Instance;

    public float currentMana = 0f;

    [SerializeField] private TMP_Text manaText; // Assign in Inspector

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
        currentMana += amount;
        UpdateManaUI();
        Debug.Log("Mana: " + currentMana);
    }

    public void ResetMana()
    {
        currentMana = 0f;
        UpdateManaUI();
    }

    private void UpdateManaUI()
    {
        if (manaText != null)
        {
            manaText.text = "Mana: " + currentMana.ToString("0.##");
        }
    }

    public bool TrySpendMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            UpdateManaUI();
            return true;
        }

        return false;
    }
}