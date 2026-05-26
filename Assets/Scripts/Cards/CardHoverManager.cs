using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverManager : MonoBehaviour
{
    private Card currentCard;

    private float hoverGraceTime = 0.1f;
    private float lastTimeSeen;

    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Clear();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        float closest = float.MaxValue;
        Card found = null;

        foreach (var hit in hits)
        {
            Card card = hit.collider.GetComponentInParent<Card>();

            if (card != null && card.IsActive() && hit.distance < closest)
            {
                closest = hit.distance;
                found = card;
            }
        }

        if (found != null)
        {
            lastTimeSeen = Time.time;
        }

        if (found != currentCard)
        {
            if (found != null)
            {
                if (currentCard != null)
                    currentCard.SetHovered(false);

                currentCard = found;
                currentCard.SetHovered(true);
            }
        }

        if (currentCard != null && Time.time - lastTimeSeen > hoverGraceTime)
        {
            currentCard.SetHovered(false);
            currentCard = null;
        }

        if (currentCard != null && Input.GetMouseButtonDown(0))
        {
            currentCard.Click();
        }
    }

    void Clear()
    {
        if (currentCard != null)
        {
            currentCard.SetHovered(false);
            currentCard = null;
        }
    }
}