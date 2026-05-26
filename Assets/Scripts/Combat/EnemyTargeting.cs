using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyTargeting : MonoBehaviour
{
    private Enemy currentEnemy;
    private Outline currentOutline;

    private Vector2 mouseStartPos;
    private bool isDragging;

    public float dragThreshold = 20f;

    void Update()
    {
        HandleHover();
        HandleInput();
    }

    void HandleHover()
    {
        ClearCurrent();

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
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

    void HandleInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<Card>() != null)
                return;
        }

        if (!HandManager.Instance.HasActiveCard())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            mouseStartPos = Input.mousePosition;
            isDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            float dist = Vector2.Distance(mouseStartPos, Input.mousePosition);

            if (dist > dragThreshold)
                isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Card activeCard = HandManager.Instance.GetActiveCard();

            if (activeCard == null)
                return;

            if (isDragging)
                activeCard.MultiAttack();
            else
                activeCard.SingleAttack(currentEnemy);
        }
    }

    void ClearCurrent()
    {
        if (currentOutline != null)
            currentOutline.enabled = false;

        currentEnemy = null;
        currentOutline = null;
    }
}