using UnityEngine;

// Automatically sends CardData from Card to CardVisualWorld.
public class CardVisualAutoBinder : MonoBehaviour
{
    [SerializeField] private Card card;
    [SerializeField] private CardVisualWorld cardVisualWorld;

    private CardData lastData;

    private void Awake()
    {
        if (card == null)
            card = GetComponent<Card>();

        if (cardVisualWorld == null)
            cardVisualWorld = GetComponentInChildren<CardVisualWorld>(true);
    }

    private void Start()
    {
        Refresh();
    }

    private void LateUpdate()
    {
        if (card == null || cardVisualWorld == null)
            return;

        CardData currentData = card.GetCardData();

        if (currentData != lastData)
            Refresh();
    }

    public void Refresh()
    {
        if (card == null || cardVisualWorld == null)
            return;

        lastData = card.GetCardData();
        cardVisualWorld.SetCardData(lastData);
    }
}
