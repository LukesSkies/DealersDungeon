using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// Handles enemy hover, clicking, and dragging.
public class EnemyTargeting : MonoBehaviour
{
    private Enemy currentEnemy;
    private Outline currentOutline;

    private Vector2 mouseStartPos;
    private Vector2 mouseEndPos;

    private bool isDragging;
    private bool inputInProgress;

    private readonly List<Enemy> draggedEnemies = new List<Enemy>();

    [Header("Drag")]
    public float dragThreshold = 20f;

    [Header("Drag Anywhere")]
    [SerializeField] private bool dragAnywhereHitsAllEnemies = true;

    [Header("Directional Damage Priority")]
    [SerializeField] private bool sortDragTargetsByDirection = true;

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
        {
            ResetInput();
            return;
        }

        Card activeCard = HandManager.Instance.GetActiveCard();

        if (activeCard == null || Camera.main == null)
        {
            ResetInput();
            return;
        }

        if (activeCard.IsWaitingForSpellTarget() &&
            (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            activeCard.CancelQueuedSpell();
            ResetInput();
            return;
        }

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0))
        {
            if (pointerOverUI)
                return;

            // Important: world-space cards are not UI, so ignore this press completely if it started on a card.
            // This stops targeted spells from being queued on mouse-down and cancelled on mouse-up.
            if (IsPointerOverCard())
                return;

            mouseStartPos = Input.mousePosition;
            mouseEndPos = mouseStartPos;

            isDragging = false;
            inputInProgress = true;
            draggedEnemies.Clear();

            AddDraggedEnemy(currentEnemy);
        }

        if (!inputInProgress)
            return;

        if (Input.GetMouseButton(0))
        {
            mouseEndPos = Input.mousePosition;

            float distance = Vector2.Distance(mouseStartPos, mouseEndPos);

            if (distance > dragThreshold)
                isDragging = true;

            if (isDragging && !pointerOverUI)
                AddDraggedEnemy(currentEnemy);
        }

        if (Input.GetMouseButtonUp(0))
        {
            mouseEndPos = Input.mousePosition;

            if (pointerOverUI)
            {
                ResetInput();
                return;
            }

            if (activeCard.IsWaitingForSpellTarget())
            {
                if (currentEnemy != null)
                    activeCard.TryCastQueuedSpell(currentEnemy);

                ResetInput();
                return;
            }

            if (isDragging)
            {
                AddDraggedEnemy(currentEnemy);
                List<Enemy> targets = GetDragAttackTargets();
                activeCard.MultiAttack(targets);
                ResetInput();
                return;
            }

            activeCard.SingleAttack(currentEnemy);
            ResetInput();
        }
    }

    private List<Enemy> GetDragAttackTargets()
    {
        List<Enemy> targets = draggedEnemies
            .Where(enemy => enemy != null && enemy.GetCurrentHP() > 0)
            .Distinct()
            .ToList();

        if (targets.Count == 0 && dragAnywhereHitsAllEnemies)
            targets = GetAllLivingEnemies();

        if (sortDragTargetsByDirection)
            targets = SortTargetsByDragDirection(targets);

        return targets;
    }

    private List<Enemy> SortTargetsByDragDirection(List<Enemy> targets)
    {
        if (targets == null || targets.Count <= 1)
            return targets;

        if (Camera.main == null)
            return targets;

        float dragX = mouseEndPos.x - mouseStartPos.x;
        bool draggedLeftToRight = dragX >= 0f;

        if (draggedLeftToRight)
        {
            return targets
                .OrderBy(enemy => Camera.main.WorldToScreenPoint(enemy.transform.position).x)
                .ToList();
        }

        return targets
            .OrderByDescending(enemy => Camera.main.WorldToScreenPoint(enemy.transform.position).x)
            .ToList();
    }

    private List<Enemy> GetAllLivingEnemies()
    {
        if (EnemyManager.Instance == null)
            return new List<Enemy>();

        return EnemyManager.Instance.GetAllEnemies()
            .Where(enemy => enemy != null && enemy.GetCurrentHP() > 0)
            .Distinct()
            .ToList();
    }

    private bool IsPointerOverCard()
    {
        if (Camera.main == null)
            return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        foreach (RaycastHit hit in hits)
        {
            Card card = hit.collider.GetComponentInParent<Card>();

            if (card != null)
                return true;
        }

        return false;
    }

    private void AddDraggedEnemy(Enemy enemy)
    {
        if (enemy == null || enemy.GetCurrentHP() <= 0)
            return;

        if (!draggedEnemies.Contains(enemy))
            draggedEnemies.Add(enemy);
    }

    private void ClearCurrent()
    {
        if (currentOutline != null)
            currentOutline.enabled = false;

        currentEnemy = null;
        currentOutline = null;
    }

    private void ResetInput()
    {
        draggedEnemies.Clear();
        isDragging = false;
        inputInProgress = false;
        mouseStartPos = Vector2.zero;
        mouseEndPos = Vector2.zero;
    }
}
