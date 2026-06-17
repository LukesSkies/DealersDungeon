using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Manages the player's health.
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("Stats")]
    public int maxHP = 30;
    private int currentHP;
    private bool isDead = false;

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
            hpSlider.value = currentHP;
        }

        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        int remainingDamage = amount;

        if (PlayerShield.Instance != null)
            remainingDamage = PlayerShield.Instance.AbsorbDamage(remainingDamage);

        if (remainingDamage <= 0)
            return;

        currentHP -= remainingDamage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateUI();

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateUI();
    }

    public void FullHeal()
    {
        if (isDead)
            return;

        currentHP = maxHP;
        UpdateUI();
    }

    public int GetCurrentHP()
    {
        return currentHP;
    }

    public bool IsDead()
    {
        return isDead;
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
        if (isDead)
            return;

        isDead = true;
        Debug.Log("Player Died");

        CameraBob cameraBob = GetComponentInChildren<CameraBob>();

        if (cameraBob != null)
            cameraBob.StopWalking();

        if (HandManager.Instance != null)
            HandManager.Instance.ClearHand();

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
        else if (GameOverUI.Instance != null)
            GameOverUI.Instance.Show();
    }
}
