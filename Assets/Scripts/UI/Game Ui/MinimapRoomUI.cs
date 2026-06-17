using UnityEngine;
using UnityEngine.UI;

// Controls one room square on the minimap.
public class MinimapRoomUI : MonoBehaviour
{
    [Header("References")]

    // This room square's RectTransform.
    [SerializeField] private RectTransform rectTransform;

    // Main room square image.
    [SerializeField] private Image roomImage;

    // Icon shown for important rooms, like boss rooms.
    [SerializeField] private Image importantIcon;

    [Header("Icon Settings")]

    // Size of important icon compared to room square.
    [SerializeField] private float importantIconSizeMultiplier = 0.65f;

    // Lets other scripts access this RectTransform.
    public RectTransform RectTransform => rectTransform;

    private Vector2 currentRoomSize = new Vector2(14f, 14f);

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        SetupImportantIcon();

        if (importantIcon != null)
            importantIcon.gameObject.SetActive(false);
    }

    // Sets this room's minimap position.
    public void SetAnchoredPosition(Vector2 position)
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = position;
    }

    // Sets this room square's size.
    public void SetSize(Vector2 size)
    {
        if (rectTransform == null)
            return;

        currentRoomSize = size;

        rectTransform.localScale = Vector3.one;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

        SetupImportantIcon();
    }

    // Centres and sizes the important icon.
    private void SetupImportantIcon()
    {
        if (importantIcon == null)
            return;

        RectTransform iconRectTransform = importantIcon.rectTransform;

        iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        iconRectTransform.pivot = new Vector2(0.5f, 0.5f);

        iconRectTransform.anchoredPosition = Vector2.zero;
        iconRectTransform.localScale = Vector3.one;
        iconRectTransform.localRotation = Quaternion.identity;

        Vector2 iconSize = currentRoomSize * importantIconSizeMultiplier;

        iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, iconSize.x);
        iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconSize.y);

        importantIcon.preserveAspect = true;
        importantIcon.raycastTarget = false;
    }

    // Sets this room's colour.
    public void SetColour(Color colour)
    {
        if (roomImage != null)
            roomImage.color = colour;
    }

    // Shows or hides this room square.
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // Shows or hides the important icon.
    public void SetImportantIcon(Sprite icon, bool visible)
    {
        if (importantIcon == null)
            return;

        importantIcon.sprite = icon;

        SetupImportantIcon();

        importantIcon.gameObject.SetActive(visible && icon != null);

        importantIcon.transform.SetAsLastSibling();
    }
}