using System.Collections.Generic;
using UnityEngine;

// Updates the selected deck slot visuals from DeckRuntimeManager.
public class SelectedDeckSlotDisplay : MonoBehaviour
{
    [Header("Selected Slot Visuals")]

    [SerializeField] private CardVisualUI[] selectedSlotVisuals;

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

    // Refreshes all selected deck slot visuals.
    public void Refresh()
    {
        if (selectedSlotVisuals == null)
            return;

        List<CardData> selectedCards = DeckRuntimeManager.Instance == null
            ? new List<CardData>()
            : DeckRuntimeManager.Instance.GetSelectedCards();

        for (int i = 0; i < selectedSlotVisuals.Length; i++)
        {
            if (selectedSlotVisuals[i] == null)
                continue;

            if (i < selectedCards.Count)
                selectedSlotVisuals[i].SetCardData(selectedCards[i]);
            else
                selectedSlotVisuals[i].ShowEmpty();
        }
    }
}