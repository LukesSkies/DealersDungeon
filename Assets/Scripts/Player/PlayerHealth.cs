using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("Stats")]
    public int maxHP = 30;
    private int currentHP;

    [Header("UI")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider hpSlider;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
        }

        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        // future: reflect hooks here

        if (PlayerShield.Instance != null && PlayerShield.Instance.TryBlock())
            return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateUI();

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateUI();

        Debug.Log("Healed: " + amount + " | HP: " + currentHP + "/" + maxHP);
    }

    private void UpdateUI()
    {
        if (hpText != null)
            hpText.text = $"HP: {currentHP}/{maxHP}";

        if (hpSlider != null)
            hpSlider.value = currentHP;
    }

    private void Die()
    {
        Debug.Log("Player Died");
        // TODO: Game Over
    }
}