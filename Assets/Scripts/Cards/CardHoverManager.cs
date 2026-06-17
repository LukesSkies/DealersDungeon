using UnityEngine;
using UnityEngine.EventSystems;

// Handles hovering and clicking world-space cards.
public class CardHoverManager : MonoBehaviour
{
    // The card currently under the mouse.
    private Card currentCard;

    // Small delay before hover is removed.
    private float hoverGraceTime = 0.1f;

    // Last time the mouse was over a valid card.
    private float lastTimeSeen;

    private void Update()
    {
        // Do not interact with cards while the mouse is over UI.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Clear();
            return;
        }

        // Shoot a ray from the camera to the mouse position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Get every object hit by the ray.
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        float closest = float.MaxValue;
        Card found = null;

        // Find the closest active card under the mouse.
        foreach (var hit in hits)
        {
            Card card = hit.collider.GetComponentInParent<Card>();

            if (card != null && card.IsActive() && hit.distance < closest)
            {
                closest = hit.distance;
                found = card;
            }
        }

        // Remember when a card was last seen.
        if (found != null)
        {
            lastTimeSeen = Time.time;
        }

        // Change hover to the newly found card.
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

        // Remove hover if the mouse has left the card.
        if (currentCard != null && Time.time - lastTimeSeen > hoverGraceTime)
        {
            currentCard.SetHovered(false);
            currentCard = null;
        }

        // Click the hovered card.
        if (currentCard != null && Input.GetMouseButtonDown(0))
        {
            currentCard.Click();
        }
    }

    // Clears the current hovered card.
    private void Clear()
    {
        if (currentCard != null)
        {
            currentCard.SetHovered(false);
            currentCard = null;
        }
    }
}