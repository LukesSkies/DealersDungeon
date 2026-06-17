using UnityEngine;

// Shows a preview of the selected deck.
public class DeckPreviewUI : MonoBehaviour
{
    [Header("Preview Slots")]

    // Card visuals used for previewing the selected deck.
    [SerializeField] private CardVisualUI[] previewSlots;

    private void OnEnable()
    {
        if (DeckRuntimeManager.Instance != null)
            DeckRuntimeManager.Instance.OnDeckChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (DeckRuntimeManager.Instance != null)
            DeckRuntimeManager.Instance.OnDeckChanged -= Refresh;
    }

    // Refreshes the preview.
    public void Refresh()
    {
        RefreshPreviewSlots();
    }

    // Updates every preview slot.
    private void RefreshPreviewSlots()
    {
        if (previewSlots == null)
            return;

        for (int i = 0; i < previewSlots.Length; i++)
        {
            if (previewSlots[i] == null)
                continue;

            CardData cardData = DeckRuntimeManager.Instance == null
                ? null
                : DeckRuntimeManager.Instance.GetCardAtSlot(i);

            if (cardData != null)
                previewSlots[i].SetCardData(cardData);
            else
                previewSlots[i].ShowEmpty();
        }
    }
}