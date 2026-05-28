using UnityEngine;
using UnityEngine.UI;

// This script controls one room square on the minimap.
//
// One MinimapRoomUI represents one dungeon room.
//
// It handles:
// - the room square image
// - changing the room colour
// - showing/hiding the boss icon
// - setting the room square size
//
// It does NOT handle connection line visuals.
// The minimap shows rooms based on their real dungeon grid positions.
public class MinimapRoomUI : MonoBehaviour
{
    [Header("References")]

    // The RectTransform of this minimap room square.
    //
    // This is used so the minimap can position and resize the square correctly
    // inside the minimap background.
    [SerializeField] private RectTransform rectTransform;

    // The main image for this room square.
    //
    // This should be the square/room icon image.
    [SerializeField] private Image roomImage;

    // Icon displayed on top of this room if it is important.
    //
    // Right now, this is used for the boss room.
    //
    // Put this image as a child of the room square.
    [SerializeField] private Image importantIcon;

    [Header("Icon Settings")]

    // How large the important icon should be compared to the room square.
    //
    // 0.65 means the icon takes up 65% of the room square.
    [SerializeField] private float importantIconSizeMultiplier = 0.65f;

    // Public access to this room's RectTransform.
    //
    // DungeonMinimapUI uses this to move the player arrow
    // to the centre of the room square.
    public RectTransform RectTransform => rectTransform;

    private Vector2 currentRoomSize = new Vector2(14f, 14f);

    private void Awake()
    {
        // Auto-find RectTransform if it was not assigned in the Inspector.
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        // Prepare the important icon RectTransform.
        SetupImportantIcon();

        // Hide the important icon by default.
        if (importantIcon != null)
            importantIcon.gameObject.SetActive(false);
    }

    // Sets this room's position inside the minimap.
    public void SetAnchoredPosition(Vector2 position)
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = position;
    }

    // Sets this room square's size.
    //
    // This stops the prefab from appearing too large on the minimap.
    public void SetSize(Vector2 size)
    {
        if (rectTransform == null)
            return;

        currentRoomSize = size;

        // Force clean UI scaling.
        rectTransform.localScale = Vector3.one;

        // Force centre anchors/pivot so every spawned square uses the same positioning rules.
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Set width and height.
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

        // Resize and centre the important icon to fit inside this square.
        SetupImportantIcon();
    }

    // Forces the important icon to be centred and fitted inside the room square.
    private void SetupImportantIcon()
    {
        if (importantIcon == null)
            return;

        RectTransform iconRectTransform = importantIcon.rectTransform;

        // The icon should not stretch to the parent.
        // It should sit in the exact centre of the room square.
        iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        iconRectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Remove any prefab offset/scale/rotation.
        iconRectTransform.anchoredPosition = Vector2.zero;
        iconRectTransform.localScale = Vector3.one;
        iconRectTransform.localRotation = Quaternion.identity;

        // Fit the icon inside the square.
        Vector2 iconSize = currentRoomSize * importantIconSizeMultiplier;

        iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, iconSize.x);
        iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconSize.y);

        // Keep the sprite aspect ratio.
        importantIcon.preserveAspect = true;

        // Usually minimap icons should not block UI raycasts.
        importantIcon.raycastTarget = false;
    }

    // Sets this room's colour.
    //
    // Example:
    // - grey for revealed but unvisited rooms
    // - white for visited rooms
    // - blue/green for start room
    public void SetColour(Color colour)
    {
        if (roomImage != null)
            roomImage.color = colour;
    }

    // Shows or hides this whole minimap room square.
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // Shows or hides the important room icon.
    //
    // For now, this is the boss icon.
    public void SetImportantIcon(Sprite icon, bool visible)
    {
        if (importantIcon == null)
            return;

        importantIcon.sprite = icon;

        // Re-apply fitting after the sprite changes.
        SetupImportantIcon();

        importantIcon.gameObject.SetActive(visible && icon != null);

        // Keep the icon above the room square image.
        importantIcon.transform.SetAsLastSibling();
    }
}