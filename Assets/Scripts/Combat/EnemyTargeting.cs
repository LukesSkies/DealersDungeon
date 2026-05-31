using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class EnemyTargeting : MonoBehaviour
{
    private Enemy currentEnemy;
    private Outline currentOutline;

    private Vector2 mouseStartPos;
    private bool isDragging;
    private readonly HashSet<Enemy> draggedEnemies = new HashSet<Enemy>();

    [Header("Drag")]
    public float dragThreshold = 20f;

    private void Update()
    {
        HandleHover();
        HandleInput();
    }

    private void HandleHover()
    {
        ClearCurrent();

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                currentEnemy = enemy;
            }
        }

        if (currentEnemy != null)
        {
            currentOutline = currentEnemy.GetComponent<Outline>();

            if (currentOutline != null)
                currentOutline.enabled = true;
        }
    }

    private void HandleInput()
    {
        if (HandManager.Instance == null || !HandManager.Instance.HasActiveCard())
            return;

        Card activeCard = HandManager.Instance.GetActiveCard();

        if (activeCard == null)
            return;

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Do not basic-attack while clicking card visuals.
        // The CardHoverManager / IPointerClickHandler handles card clicks.
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<Card>() != null)
                return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            mouseStartPos = Input.mousePosition;
            isDragging = false;
            draggedEnemies.Clear();

            if (currentEnemy != null)
                draggedEnemies.Add(currentEnemy);
        }

        if (Input.GetMouseButton(0))
        {
            float distance = Vector2.Distance(mouseStartPos, Input.mousePosition);

            if (distance > dragThreshold)
                isDragging = true;

            if (isDragging && currentEnemy != null)
                draggedEnemies.Add(currentEnemy);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (activeCard.IsWaitingForSpellTarget())
            {
                activeCard.TryCastQueuedSpell(currentEnemy);
                draggedEnemies.Clear();
                return;
            }

            if (isDragging)
            {
                if (currentEnemy != null)
                    draggedEnemies.Add(currentEnemy);

                List<Enemy> targets = draggedEnemies
                    .Where(enemy => enemy != null && enemy.GetCurrentHP() > 0)
                    .Distinct()
                    .ToList();

                activeCard.MultiAttack(targets);
            }
            else
            {
                activeCard.SingleAttack(currentEnemy);
            }

            draggedEnemies.Clear();
        }
    }

    private void ClearCurrent()
    {
        if (currentOutline != null)
            currentOutline.enabled = false;

        currentEnemy = null;
        currentOutline = null;
    }
}
