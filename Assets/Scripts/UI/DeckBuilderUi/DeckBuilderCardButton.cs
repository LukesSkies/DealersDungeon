using System;
using UnityEngine;
using UnityEngine.UI;

// This controls one card button inside the deck builder scroll grid.
//
// This is UI, so it uses CardVisualUI, not CardVisualWorld.
public class DeckBuilderCardButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private CardVisualUI cardVisualUI;

    private CardData cardData;
    private Action<CardData> onClicked;

    public CardData CardData => cardData;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (cardVisualUI == null)
            cardVisualUI = GetComponentInChildren<CardVisualUI>(true);

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Setup(CardData data, Action<CardData> clickAction)
    {
        cardData = data;
        onClicked = clickAction;

        if (cardVisualUI != null)
            cardVisualUI.SetCardData(cardData);
    }

    public void SetSelectedState(bool isSelected)
    {
        if (button != null)
            button.interactable = !isSelected;

        if (cardVisualUI != null)
        {
            if (isSelected)
                cardVisualUI.SetDimmed();
            else
                cardVisualUI.SetNormal();
        }
    }

    private void HandleClick()
    {
        if (cardData == null)
            return;

        onClicked?.Invoke(cardData);
    }
}