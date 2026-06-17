using System;
using System.Collections.Generic;
using UnityEngine;

// Stores the player's selected deck.
// Saves and loads the last deck using PlayerPrefs.
// Uses fixed slots so removing slot 2 does not shift slots 3, 4, and 5 down.
public class DeckRuntimeManager : MonoBehaviour
{
    public static DeckRuntimeManager Instance;

    [Header("Deck Settings")]

    // Maximum number of card slots.
    [SerializeField] private int maxDeckSize = 5;

    [Header("Available Cards")]

    // Add every CardData asset in your game here.
    [SerializeField] private List<CardData> availableCards = new List<CardData>();

    // Fixed selected card slots.
    // Empty slots are stored as null.
    private readonly List<CardData> selectedCards = new List<CardData>();

    private const string SavedDeckKey = "SavedDeck";

    public int MaxDeckSize => maxDeckSize;

    // Keeps your existing DeckBuilderUI code working.
    public List<CardData> AvailableCards => availableCards;

    // UI scripts can listen for deck changes.
    public event Action OnDeckChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSlotListSize();
        LoadSavedDeck();
    }

    private void OnValidate()
    {
        if (maxDeckSize < 1)
            maxDeckSize = 1;
    }

    // Makes sure selectedCards always has exactly maxDeckSize slots.
    private void EnsureSlotListSize()
    {
        while (selectedCards.Count < maxDeckSize)
            selectedCards.Add(null);

        while (selectedCards.Count > maxDeckSize)
            selectedCards.RemoveAt(selectedCards.Count - 1);
    }

    // Adds a card to the first empty slot.
    public bool TryAddCard(CardData cardData)
    {
        if (cardData == null)
            return false;

        EnsureSlotListSize();

        if (ContainsCard(cardData))
            return false;

        int emptySlot = GetFirstEmptySlot();

        if (emptySlot < 0)
            return false;

        selectedCards[emptySlot] = cardData;

        SaveSelectedDeck();
        NotifyDeckChanged();

        return true;
    }

    // Same as TryAddCard.
    public bool AddCard(CardData cardData)
    {
        return TryAddCard(cardData);
    }

    // Removes a card without shifting the other slots.
    public bool RemoveCard(CardData cardData)
    {
        if (cardData == null)
            return false;

        EnsureSlotListSize();

        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] == cardData)
            {
                selectedCards[i] = null;

                SaveSelectedDeck();
                NotifyDeckChanged();

                return true;
            }
        }

        return false;
    }

    // Gets the card in a selected deck slot.
    public CardData GetCardAtSlot(int slotIndex)
    {
        EnsureSlotListSize();

        if (slotIndex < 0 || slotIndex >= selectedCards.Count)
            return null;

        return selectedCards[slotIndex];
    }

    // Removes a card from a selected deck slot without shifting other slots.
    public bool RemoveCardAtSlot(int slotIndex)
    {
        EnsureSlotListSize();

        if (slotIndex < 0 || slotIndex >= selectedCards.Count)
            return false;

        if (selectedCards[slotIndex] == null)
            return false;

        selectedCards[slotIndex] = null;

        SaveSelectedDeck();
        NotifyDeckChanged();

        return true;
    }

    // Same as RemoveCardAtSlot.
    public bool RemoveCardAt(int slotIndex)
    {
        return RemoveCardAtSlot(slotIndex);
    }

    // Checks if this card is already selected.
    public bool ContainsCard(CardData cardData)
    {
        if (cardData == null)
            return false;

        EnsureSlotListSize();

        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] == cardData)
                return true;
        }

        return false;
    }

    // Clears all selected slots without changing slot count.
    public void ClearSelectedDeck()
    {
        EnsureSlotListSize();

        for (int i = 0; i < selectedCards.Count; i++)
            selectedCards[i] = null;

        SaveSelectedDeck();
        NotifyDeckChanged();
    }

    // Same as ClearSelectedDeck.
    public void ClearDeck()
    {
        ClearSelectedDeck();
    }

    // Deletes the saved deck completely.
    public void DeleteSavedDeck()
    {
        EnsureSlotListSize();

        for (int i = 0; i < selectedCards.Count; i++)
            selectedCards[i] = null;

        PlayerPrefs.DeleteKey(SavedDeckKey);
        PlayerPrefs.Save();

        NotifyDeckChanged();
    }

    // Gets the number of filled slots.
    public int GetSelectedCount()
    {
        EnsureSlotListSize();

        int count = 0;

        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] != null)
                count++;
        }

        return count;
    }

    // Checks if any cards are selected.
    public bool HasSelectedCards()
    {
        return GetSelectedCount() > 0;
    }

    // Gets a copy of selected slots.
    // This keeps empty slots as null.
    public List<CardData> GetSelectedCards()
    {
        EnsureSlotListSize();

        return new List<CardData>(selectedCards);
    }

    // Gets selected cards without empty slots.
    // Use this for combat.
    public List<CardData> GetCombatDeck()
    {
        EnsureSlotListSize();

        List<CardData> combatDeck = new List<CardData>();

        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] != null)
                combatDeck.Add(selectedCards[i]);
        }

        return combatDeck;
    }

    // Finds the first empty selected slot.
    private int GetFirstEmptySlot()
    {
        EnsureSlotListSize();

        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] == null)
                return i;
        }

        return -1;
    }

    // Saves selected slots.
    private void SaveSelectedDeck()
    {
        EnsureSlotListSize();

        SavedDeckData saveData = new SavedDeckData();

        for (int i = 0; i < selectedCards.Count; i++)
        {
            CardData card = selectedCards[i];

            if (card == null)
            {
                saveData.cardNames.Add("");
                continue;
            }

            if (string.IsNullOrWhiteSpace(card.cardName))
            {
                Debug.LogWarning("A selected card has no cardName, so it cannot be saved.");
                saveData.cardNames.Add("");
                continue;
            }

            saveData.cardNames.Add(card.cardName);
        }

        string json = JsonUtility.ToJson(saveData);

        PlayerPrefs.SetString(SavedDeckKey, json);
        PlayerPrefs.Save();
    }

    // Loads the saved deck into the same slot positions.
    public void LoadSavedDeck()
    {
        EnsureSlotListSize();

        for (int i = 0; i < selectedCards.Count; i++)
            selectedCards[i] = null;

        if (!PlayerPrefs.HasKey(SavedDeckKey))
        {
            NotifyDeckChanged();
            return;
        }

        string json = PlayerPrefs.GetString(SavedDeckKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            NotifyDeckChanged();
            return;
        }

        SavedDeckData saveData = JsonUtility.FromJson<SavedDeckData>(json);

        if (saveData == null || saveData.cardNames == null)
        {
            NotifyDeckChanged();
            return;
        }

        int slotCount = Mathf.Min(saveData.cardNames.Count, maxDeckSize);

        for (int i = 0; i < slotCount; i++)
        {
            string cardName = saveData.cardNames[i];

            if (string.IsNullOrWhiteSpace(cardName))
                continue;

            CardData card = FindCardByName(cardName);

            if (card != null)
                selectedCards[i] = card;
            else
                Debug.LogWarning("Saved card could not be found: " + cardName);
        }

        NotifyDeckChanged();
    }

    // Finds a card by cardName.
    private CardData FindCardByName(string cardName)
    {
        if (string.IsNullOrWhiteSpace(cardName))
            return null;

        for (int i = 0; i < availableCards.Count; i++)
        {
            CardData card = availableCards[i];

            if (card == null)
                continue;

            if (card.cardName == cardName)
                return card;
        }

        return null;
    }

    // Tells UI scripts to refresh.
    private void NotifyDeckChanged()
    {
        OnDeckChanged?.Invoke();
    }

    // Save data format.
    [Serializable]
    private class SavedDeckData
    {
        public List<string> cardNames = new List<string>();
    }
}