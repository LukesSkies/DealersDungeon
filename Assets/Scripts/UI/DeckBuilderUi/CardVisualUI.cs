using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Updates a UI card using CardData.
public class CardVisualUI : MonoBehaviour
{
    [Header("Card Image")]
    [SerializeField] private Image cardImage;

    [Tooltip("Sprite shown when the slot is empty.")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("Mini Game Icon")]
    [SerializeField] private Image miniGameIconImage;

    [Header("Card Text")]
    [SerializeField] private TMP_Text nameText;

    [FormerlySerializedAs("abilityText")]
    [SerializeField] private TMP_Text skillText;

    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text damageText;

    [Header("Alpha")]
    [SerializeField] private float normalAlpha = 1f;
    [SerializeField] private float dimmedAlpha = 0.35f;
    [SerializeField] private float emptyAlpha = 0.25f;

    private CardData currentData;

    public CardData CurrentData => currentData;

    public void SetCardData(CardData data)
    {
        currentData = data;

        if (data == null)
        {
            ShowEmpty();
            return;
        }

        if (cardImage != null)
        {
            cardImage.sprite = data.cardSprite;
            cardImage.enabled = data.cardSprite != null;
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

        SetNormal();
    }

    public void ShowEmpty()
    {
        currentData = null;

        if (cardImage != null)
        {
            cardImage.sprite = emptySlotSprite;
            cardImage.enabled = emptySlotSprite != null;
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

        SetImageAlpha(emptyAlpha);
        SetTextAlpha(emptyAlpha);
        SetMiniGameIconAlpha(emptyAlpha);
    }

    public void SetNormal()
    {
        SetImageAlpha(normalAlpha);
        SetTextAlpha(normalAlpha);
        SetMiniGameIconAlpha(normalAlpha);
    }

    public void SetDimmed()
    {
        SetImageAlpha(dimmedAlpha);
        SetTextAlpha(dimmedAlpha);
        SetMiniGameIconAlpha(dimmedAlpha);
    }

    public void SetCustomAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        SetImageAlpha(alpha);
        SetTextAlpha(alpha);
        SetMiniGameIconAlpha(alpha);
    }

    private string FormatManaText(CardData data)
    {
        if (data == null)
            return "";

        return data.GetManaText();
    }

    private string FormatDamageText(CardData data)
    {
        if (data == null)
            return "";

        return data.baseDamage.ToString();
    }

    private void SetImageAlpha(float alpha)
    {
        if (cardImage == null)
            return;

        Color color = cardImage.color;
        color.a = alpha;
        cardImage.color = color;
    }

    private void SetMiniGameIconAlpha(float alpha)
    {
        if (miniGameIconImage == null)
            return;

        Color color = miniGameIconImage.color;
        color.a = alpha;
        miniGameIconImage.color = color;
    }

    private void SetTextAlpha(float alpha)
    {
        if (nameText != null)
        {
            Color color = nameText.color;
            color.a = alpha;
            nameText.color = color;
        }

        if (skillText != null)
        {
            Color color = skillText.color;
            color.a = alpha;
            skillText.color = color;
        }

        if (manaText != null)
        {
            Color color = manaText.color;
            color.a = alpha;
            manaText.color = color;
        }

        if (damageText != null)
        {
            Color color = damageText.color;
            color.a = alpha;
            damageText.color = color;
        }
    }
}