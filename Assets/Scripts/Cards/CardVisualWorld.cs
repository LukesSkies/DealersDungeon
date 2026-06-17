using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Updates the visible world-space card sprite, UI icon, and text using CardData.
public class CardVisualWorld : MonoBehaviour
{
    [Header("Card Sprite")]
    [SerializeField] private SpriteRenderer cardSpriteRenderer;

    [Header("Mini Game Icon")]
    [Tooltip("Use a UI Image on a world-space Canvas, not a SpriteRenderer.")]
    [SerializeField] private Image miniGameIconImage;

    [Header("Card Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text skillText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text damageText;

    private CardData currentData;

    private float lastShownMana = -1f;

    public CardData CurrentData => currentData;

    public void SetCardData(CardData data)
    {
        currentData = data;

        if (data == null)
        {
            ClearVisuals();
            return;
        }

        if (cardSpriteRenderer != null)
        {
            cardSpriteRenderer.sprite = data.cardSprite;
            cardSpriteRenderer.enabled = data.cardSprite != null;
        }

        if (miniGameIconImage != null)
        {
            bool hasMiniGameIcon =
                data.miniGameIcon != null &&
                data.miniGameType != CardMiniGameType.None;

            miniGameIconImage.sprite = data.miniGameIcon;
            miniGameIconImage.enabled = hasMiniGameIcon;
            miniGameIconImage.gameObject.SetActive(hasMiniGameIcon);
        }

        if (nameText != null)
            nameText.text = data.cardName;

        if (skillText != null)
            skillText.text = data.GetAbilityText();

        if (manaText != null)
            manaText.text = FormatManaText(data);

        if (damageText != null)
            damageText.text = FormatDamageText(data);
    }

    private void LateUpdate()
    {
        if (currentData == null)
            return;

        if (currentData.manaCostMode != CardManaCostMode.AllRemaining)
            return;

        if (ManaManager.Instance == null)
            return;

        if (Mathf.Approximately(lastShownMana, ManaManager.Instance.currentMana))
            return;

        lastShownMana = ManaManager.Instance.currentMana;

        if (manaText != null)
            manaText.text = FormatManaText(currentData);
    }

    public void ClearVisuals()
    {
        currentData = null;

        if (cardSpriteRenderer != null)
        {
            cardSpriteRenderer.sprite = null;
            cardSpriteRenderer.enabled = false;
        }

        if (miniGameIconImage != null)
        {
            miniGameIconImage.sprite = null;
            miniGameIconImage.enabled = false;
            miniGameIconImage.gameObject.SetActive(false);
        }

        if (nameText != null)
            nameText.text = "";

        if (skillText != null)
            skillText.text = "";

        if (manaText != null)
            manaText.text = "";

        if (damageText != null)
            damageText.text = "";
    }

    private string FormatManaText(CardData data)
    {
        if (data == null)
            return "";

        if (!data.HasSkill())
            return "-";

        if (!data.spendManaOnSkillCast)
            return "0";

        if (data.manaCostMode == CardManaCostMode.AllRemaining)
        {
            if (ManaManager.Instance != null)
                return ManaManager.Instance.currentMana.ToString("0.##");

            return "0";
        }

        return data.manaCost.ToString("0.##");
    }

    private string FormatDamageText(CardData data)
    {
        if (data == null)
            return "";

        return data.baseDamage.ToString();
    }
}