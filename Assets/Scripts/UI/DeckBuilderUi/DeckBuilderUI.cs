using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Controls the deck builder screen.
public class DeckBuilderUI : MonoBehaviour
{
    [Header("Available Cards Scroll Grid")]

    // The Content object inside your Scroll View.
    [SerializeField] private Transform availableCardsContent;

    // Prefab for one card in the scroll grid.
    // It should have CardVisualUI and a Button.
    [SerializeField] private GameObject availableCardPrefab;

    [Header("Selected Deck Slots")]

    // The 5 selected slot visuals at the top.
    [SerializeField] private CardVisualUI[] selectedSlotVisuals;

    // Optional buttons on the selected slots.
    // Clicking one removes that card from the deck.
    [SerializeField] private Button[] selectedSlotButtons;

    // Spawned card visuals in the scroll grid.
    private readonly List<CardVisualUI> availableCardVisuals = new List<CardVisualUI>();

    // CardData linked to each spawned scroll grid visual.
    private readonly List<CardData> availableCardData = new List<CardData>();

    // Used so we can safely remove only the listeners this script added.
    private readonly List<UnityAction> selectedSlotActions = new List<UnityAction>();

    private void OnEnable()
    {
        if (DeckRuntimeManager.Instance != null)
            DeckRuntimeManager.Instance.OnDeckChanged += Refresh;

        BuildAvailableCards();
        AddSelectedSlotListeners();
        Refresh();
    }

    private void OnDisable()
    {
        if (DeckRuntimeManager.Instance != null)
            DeckRuntimeManager.Instance.OnDeckChanged -= Refresh;

        RemoveSelectedSlotListeners();
    }

    // Builds all cards in the scroll grid.
    private void BuildAvailableCards()
    {
        ClearAvailableCards();

        if (DeckRuntimeManager.Instance == null)
            return;

        if (availableCardsContent == null)
        {
            Debug.LogError("DeckBuilderUI is missing Available Cards Content.");
            return;
        }

        if (availableCardPrefab == null)
        {
            Debug.LogError("DeckBuilderUI is missing Available Card Prefab.");
            return;
        }

        List<CardData> cards = DeckRuntimeManager.Instance.AvailableCards;

        for (int i = 0; i < cards.Count; i++)
        {
            CardData cardData = cards[i];

            if (cardData == null)
                continue;

            GameObject instance = Instantiate(availableCardPrefab, availableCardsContent);

            CardVisualUI visual = instance.GetComponent<CardVisualUI>();

            if (visual == null)
                visual = instance.GetComponentInChildren<CardVisualUI>(true);

            Button button = instance.GetComponent<Button>();

            if (button == null)
                button = instance.GetComponentInChildren<Button>(true);

            if (visual != null)
                visual.SetCardData(cardData);

            CardData capturedCard = cardData;

            if (button != null)
                button.onClick.AddListener(() => SelectCard(capturedCard));

            availableCardVisuals.Add(visual);
            availableCardData.Add(cardData);
        }
    }

    // Removes all spawned cards from the scroll grid.
    private void ClearAvailableCards()
    {
        availableCardVisuals.Clear();
        availableCardData.Clear();

        if (availableCardsContent == null)
            return;

        for (int i = availableCardsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(availableCardsContent.GetChild(i).gameObject);
        }
    }

    // Adds click listeners to selected deck slots.
    private void AddSelectedSlotListeners()
    {
        RemoveSelectedSlotListeners();

        if (selectedSlotButtons == null)
            return;

        for (int i = 0; i < selectedSlotButtons.Length; i++)
        {
            Button slotButton = selectedSlotButtons[i];

            if (slotButton == null)
                continue;

            int capturedIndex = i;

            UnityAction action = () => RemoveCardAtSlot(capturedIndex);

            selectedSlotActions.Add(action);
            slotButton.onClick.AddListener(action);
        }
    }

    // Removes click listeners from selected deck slots.
    private void RemoveSelectedSlotListeners()
    {
        if (selectedSlotButtons == null)
        {
            selectedSlotActions.Clear();
            return;
        }

        for (int i = 0; i < selectedSlotButtons.Length && i < selectedSlotActions.Count; i++)
        {
            if (selectedSlotButtons[i] != null && selectedSlotActions[i] != null)
                selectedSlotButtons[i].onClick.RemoveListener(selectedSlotActions[i]);
        }

        selectedSlotActions.Clear();
    }

    // Adds a card to the selected deck.
    private void SelectCard(CardData cardData)
    {
        if (DeckRuntimeManager.Instance == null)
            return;

        DeckRuntimeManager.Instance.TryAddCard(cardData);

        Refresh();
    }

    // Removes a card from a selected slot.
    private void RemoveCardAtSlot(int slotIndex)
    {
        if (DeckRuntimeManager.Instance == null)
            return;

        DeckRuntimeManager.Instance.RemoveCardAtSlot(slotIndex);

        Refresh();
    }

    // Refreshes selected slots and scroll grid dimming.
    public void Refresh()
    {
        RefreshSelectedSlots();
        RefreshAvailableCards();
    }

    // Updates the selected deck slot visuals.
    private void RefreshSelectedSlots()
    {
        if (selectedSlotVisuals == null)
            return;

        for (int i = 0; i < selectedSlotVisuals.Length; i++)
        {
            if (selectedSlotVisuals[i] == null)
                continue;

            CardData cardData = DeckRuntimeManager.Instance == null
                ? null
                : DeckRuntimeManager.Instance.GetCardAtSlot(i);

            if (cardData != null)
                selectedSlotVisuals[i].SetCardData(cardData);
            else
                selectedSlotVisuals[i].ShowEmpty();
        }
    }

    // Updates the scroll grid cards.
    private void RefreshAvailableCards()
    {
        for (int i = 0; i < availableCardVisuals.Count; i++)
        {
            CardVisualUI visual = availableCardVisuals[i];

            if (visual == null)
                continue;

            CardData cardData = i < availableCardData.Count ? availableCardData[i] : null;

            bool isSelected = DeckRuntimeManager.Instance != null &&
                              DeckRuntimeManager.Instance.ContainsCard(cardData);

            if (isSelected)
                visual.SetDimmed();
            else
                visual.SetNormal();
        }
    }
}