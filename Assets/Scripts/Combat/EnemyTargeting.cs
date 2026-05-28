using UnityEngine;
using UnityEngine.EventSystems;

// This script handles targeting enemies with the mouse.
//
// It does two main things:
// 1. Finds the enemy currently under the mouse and enables its outline.
// 2. Handles clicking or dragging with the active card.
//
// Click = single-target attack.
// Drag = multi-target attack.
public class EnemyTargeting : MonoBehaviour
{
    // The enemy currently under the mouse.
    private Enemy currentEnemy;

    // The Outline component on the currently hovered enemy.
    private Outline currentOutline;

    // The mouse position when the player first presses left click.
    //
    // Used to detect whether the player clicked or dragged.
    private Vector2 mouseStartPos;

    // True if the player has dragged far enough to count as a drag attack.
    private bool isDragging;

    // How far the mouse must move before the input counts as a drag.
    //
    // Smaller number = easier to trigger multi-attack.
    // Bigger number = harder to trigger multi-attack.
    public float dragThreshold = 20f;

    private void Update()
    {
        // Update enemy hover/outline every frame.
        HandleHover();

        // Handle clicking or dragging with the active card.
        HandleInput();
    }

    // Finds the closest enemy under the mouse and enables its outline.
    private void HandleHover()
    {
        // Clear the previous hovered enemy first.
        //
        // This makes sure old outlines get disabled before checking the new hover.
        ClearCurrent();

        // If the mouse is over UI, do not target enemies.
        //
        // This prevents clicking through cards, buttons, menus, etc.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Create a ray from the camera through the mouse position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Get every object hit by the ray.
        //
        // RaycastAll is used because multiple objects may overlap.
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        // Tracks the closest enemy hit by the ray.
        float closestDistance = float.MaxValue;

        // Check each raycast hit.
        foreach (var hit in hits)
        {
            // Try to find an Enemy component on the hit object or its parent.
            //
            // This is useful when the collider is on a child object.
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            // If this enemy is closer than the previous enemy, use it.
            if (enemy != null && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                currentEnemy = enemy;
            }
        }

        // If an enemy was found, enable its outline.
        if (currentEnemy != null)
        {
            currentOutline = currentEnemy.GetComponent<Outline>();

            if (currentOutline != null)
                currentOutline.enabled = true;
        }
    }

    // Handles card attack input.
    private void HandleInput()
    {
        // Raycast from the camera through the mouse.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // If the mouse is currently over a Card object,
        // do not also target an enemy.
        //
        // This prevents card clicks from accidentally attacking enemies.
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<Card>() != null)
                return;
        }

        // If there is no active card, the player cannot attack.
        if (!HandManager.Instance.HasActiveCard())
            return;

        // When left mouse is first pressed, store the start position.
        if (Input.GetMouseButtonDown(0))
        {
            mouseStartPos = Input.mousePosition;
            isDragging = false;
        }

        // While left mouse is held, check how far the mouse has moved.
        if (Input.GetMouseButton(0))
        {
            float dist = Vector2.Distance(mouseStartPos, Input.mousePosition);

            // If the mouse moved far enough, count this as a drag.
            if (dist > dragThreshold)
                isDragging = true;
        }

        // When left mouse is released, perform either a single attack or multi-attack.
        if (Input.GetMouseButtonUp(0))
        {
            // If the mouse is over UI when released, cancel the attack.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // Get the currently active card from the hand.
            Card activeCard = HandManager.Instance.GetActiveCard();

            if (activeCard == null)
                return;

            // Dragging uses the card's multi-attack.
            if (isDragging)
                activeCard.MultiAttack();
            // Clicking uses the card's single-target attack.
            else
                activeCard.SingleAttack(currentEnemy);
        }
    }

    // Clears the currently hovered enemy and disables its outline.
    private void ClearCurrent()
    {
        if (currentOutline != null)
            currentOutline.enabled = false;

        currentEnemy = null;
        currentOutline = null;
    }
}