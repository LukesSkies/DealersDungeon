using UnityEngine;
using UnityEngine.UI;

// Clears every selected card from the deck.
public class DeckClearButton : MonoBehaviour
{
    [Header("Button")]

    [SerializeField] private Button button;

    [Header("UI Refresh")]

    // Drag your DeckBuilderUI object here.
    [SerializeField] private DeckBuilderUI deckBuilderUI;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (deckBuilderUI == null)
            deckBuilderUI = FindFirstObjectByType<DeckBuilderUI>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(ClearDeck);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(ClearDeck);
    }

    // Clears the deck and refreshes the scroll grid straight away.
    private void ClearDeck()
    {
        if (DeckRuntimeManager.Instance == null)
        {
            Debug.LogError("No DeckRuntimeManager found.");
            return;
        }

        DeckRuntimeManager.Instance.ClearSelectedDeck();

        if (deckBuilderUI != null)
            deckBuilderUI.Refresh();
    }
}