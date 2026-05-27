using UnityEngine;
using UnityEngine.EventSystems;

// This script handles hovering and clicking cards with the mouse.
//
// It works by:
// - raycasting from the camera through the mouse position
// - finding the closest active Card under the cursor
// - telling that card when it is hovered
// - clicking the card when the player presses left mouse button
public class CardHoverManager : MonoBehaviour
{
    // The card currently being hovered by the mouse.
    private Card currentCard;

    // A small delay before hover is removed.
    //
    // This helps stop flickering if the raycast briefly misses the card
    // for a frame or two.
    private float hoverGraceTime = 0.1f;

    // The last time the mouse raycast saw a valid card.
    private float lastTimeSeen;

    private void Update()
    {
        // If the mouse is currently over normal Unity UI,
        // stop card hovering.
        //
        // This prevents cards being clicked/hovered through UI buttons,
        // menus, panels, etc.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Clear();
            return;
        }

        // Create a ray from the camera through the mouse position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Get every object hit by the ray.
        //
        // Physics.RaycastAll is used instead of Physics.Raycast because cards
        // may overlap, and we want to pick the closest active card.
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        // Tracks the closest card hit by the ray.
        float closest = float.MaxValue;

        // The active card found under the mouse this frame.
        Card found = null;

        // Check every raycast hit.
        foreach (var hit in hits)
        {
            // Try to find a Card component on the hit object or its parent.
            //
            // This is useful if the collider is on a child object of the card.
            Card card = hit.collider.GetComponentInParent<Card>();

            // Only allow hovering active cards.
            //
            // Also choose the closest card if multiple cards were hit.
            if (card != null && card.IsActive() && hit.distance < closest)
            {
                closest = hit.distance;
                found = card;
            }
        }

        // If a card was found this frame,
        // update the last seen time.
        if (found != null)
        {
            lastTimeSeen = Time.time;
        }

        // If the card under the mouse changed,
        // update hover states.
        if (found != currentCard)
        {
            if (found != null)
            {
                // Unhover the previous card if there was one.
                if (currentCard != null)
                    currentCard.SetHovered(false);

                // Set the new card as the current hovered card.
                currentCard = found;
                currentCard.SetHovered(true);
            }
        }

        // If no valid card has been seen for longer than the grace time,
        // clear the current hover.
        if (currentCard != null && Time.time - lastTimeSeen > hoverGraceTime)
        {
            currentCard.SetHovered(false);
            currentCard = null;
        }

        // If a card is currently hovered and the player left-clicks,
        // click the card.
        if (currentCard != null && Input.GetMouseButtonDown(0))
        {
            currentCard.Click();
        }
    }

    // Clears the current hovered card.
    //
    // Used when the mouse is over UI or when hover should be cancelled.
    private void Clear()
    {
        if (currentCard != null)
        {
            currentCard.SetHovered(false);
            currentCard = null;
        }
    }
}