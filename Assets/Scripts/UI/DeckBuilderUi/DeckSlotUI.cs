using System;
using UnityEngine;
using UnityEngine.UI;

// This controls one selected deck slot in the deck builder.
//
// This is UI, so it uses CardVisualUI.
//
// Clicking a filled slot removes that card.
// Removing slot 2 only empties slot 2.
// Slot 1 and slot 3 stay where they are.
public class DeckSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private CardVisualUI cardVisualUI;

    private int slotIndex;
    private Action<int> onClicked;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (cardVisualUI == null)
            cardVisualUI = GetComponentInChildren<CardVisualUI>(true);

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    public void Setup(int index, CardData cardData, Action<int> clickAction)
    {
        slotIndex = index;
        onClicked = clickAction;

        if (cardVisualUI != null)
            cardVisualUI.SetCardData(cardData);
    }

    private void HandleClick()
    {
        onClicked?.Invoke(slotIndex);
    }
}