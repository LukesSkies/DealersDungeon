using System.Collections.Generic;
using UnityEngine;

// Builds and updates the dungeon minimap.
public class DungeonMinimapUI : MonoBehaviour
{
    public static DungeonMinimapUI Instance;

    [Header("References")]

    // Parent for minimap room squares.
    [SerializeField] private RectTransform mapContainer;

    // Room square prefab.
    [SerializeField] private MinimapRoomUI roomPrefab;

    // Player arrow UI object.
    [SerializeField] private RectTransform playerArrow;

    [Header("Icons")]

    // Boss room icon.
    [SerializeField] private Sprite bossRoomIcon;

    [Header("Layout")]

    // Size of each room square.
    [SerializeField] private Vector2 roomSize = new Vector2(14f, 14f);

    // Distance between room squares.
    [SerializeField] private float roomSpacing = 20f;

    [Header("Colours")]

    // Colour for revealed but unvisited rooms.
    [SerializeField] private Color revealedButUnvisitedColour = new Color(0.35f, 0.35f, 0.35f, 1f);

    // Colour for visited rooms.
    [SerializeField] private Color visitedColour = Color.white;

    // Colour for the visited start room.
    [SerializeField] private Color startRoomColour = new Color(0.2f, 0.8f, 1f, 1f);

    [Header("Player Arrow")]

    // Arrow size.
    [SerializeField] private Vector2 playerArrowSize = new Vector2(10f, 10f);

    // Small visual offset for the arrow.
    [SerializeField] private Vector2 playerArrowVisualOffset = Vector2.zero;

    // Rotation correction for the arrow sprite.
    [SerializeField] private float playerArrowRotationOffset = 0f;

    [Header("Debug")]

    // Logs arrow movement.
    [SerializeField] private bool debugArrowMovement = false;

    // Minimap rooms by grid position.
    private Dictionary<Vector2Int, MinimapRoomUI> mapRooms = new Dictionary<Vector2Int, MinimapRoomUI>();

    // Dungeon room data by grid position.
    private Dictionary<Vector2Int, RoomNode> roomNodes = new Dictionary<Vector2Int, RoomNode>();

    // Rooms currently visible on the minimap.
    private HashSet<Vector2Int> revealedRooms = new HashSet<Vector2Int>();

    // Rooms the player has entered.
    private HashSet<Vector2Int> visitedRooms = new HashSet<Vector2Int>();

    // Start room grid position.
    private Vector2Int startGridPosition = Vector2Int.zero;

    // Last arrow room position.
    private Vector2Int currentArrowRoomPosition = Vector2Int.zero;

    private void Awake()
    {
        Instance = this;

        SetupPlayerArrow();

        if (playerArrow != null)
            playerArrow.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdatePlayerArrowRotation();
    }

    // Sets up the player arrow RectTransform.
    private void SetupPlayerArrow()
    {
        if (playerArrow == null)
            return;

        playerArrow.localScale = Vector3.one;

        playerArrow.anchorMin = new Vector2(0.5f, 0.5f);
        playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrow.pivot = new Vector2(0.5f, 0.5f);

        playerArrow.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, playerArrowSize.x);
        playerArrow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, playerArrowSize.y);

        playerArrow.anchoredPosition = playerArrowVisualOffset;

        playerArrow.SetAsLastSibling();
    }

    // Builds the minimap from dungeon room data.
    public void BuildMap(List<RoomNode> nodes, Vector2Int startPosition)
    {
        ClearMap();

        startGridPosition = startPosition;
        currentArrowRoomPosition = startPosition;

        if (mapContainer == null)
        {
            Debug.LogError("DungeonMinimapUI is missing Map Container.");
            return;
        }

        if (roomPrefab == null)
        {
            Debug.LogError("DungeonMinimapUI is missing Room Prefab.");
            return;
        }

        if (nodes == null)
        {
            Debug.LogError("DungeonMinimapUI cannot build map because nodes is null.");
            return;
        }

        if (playerArrow != null)
        {
            playerArrow.SetParent(mapContainer, false);
            playerArrow.gameObject.SetActive(false);
            SetupPlayerArrow();
        }

        foreach (RoomNode node in nodes)
        {
            if (node == null)
                continue;

            roomNodes[node.gridPos] = node;

            MinimapRoomUI roomUI = Instantiate(roomPrefab, mapContainer);

            roomUI.SetSize(roomSize);

            Vector2 minimapPosition = GridToMinimapPosition(node.gridPos);

            roomUI.SetAnchoredPosition(minimapPosition);

            roomUI.SetVisible(false);

            roomUI.SetImportantIcon(bossRoomIcon, false);

            mapRooms[node.gridPos] = roomUI;
        }

        if (playerArrow != null)
            playerArrow.gameObject.SetActive(false);
    }

    // Clears the minimap.
    public void ClearMap()
    {
        if (playerArrow != null && mapContainer != null)
        {
            playerArrow.SetParent(mapContainer, false);
            playerArrow.gameObject.SetActive(false);
        }

        foreach (MinimapRoomUI roomUI in mapRooms.Values)
        {
            if (roomUI != null)
                Destroy(roomUI.gameObject);
        }

        mapRooms.Clear();
        roomNodes.Clear();
        revealedRooms.Clear();
        visitedRooms.Clear();

        if (playerArrow != null)
            playerArrow.gameObject.SetActive(false);
    }

    // Converts dungeon grid position to minimap position.
    private Vector2 GridToMinimapPosition(Vector2Int gridPosition)
    {
        Vector2Int relativePosition = gridPosition - startGridPosition;

        return new Vector2(
            relativePosition.x * roomSpacing,
            relativePosition.y * roomSpacing
        );
    }

    // Reveals rooms from the current room.
    public void RevealFromRoom(Vector2Int roomPosition)
    {
        if (!roomNodes.ContainsKey(roomPosition))
            return;

        currentArrowRoomPosition = roomPosition;

        visitedRooms.Add(roomPosition);
        revealedRooms.Add(roomPosition);

        RoomNode currentNode = roomNodes[roomPosition];

        foreach (RoomNode connectedNode in currentNode.connections.Values)
        {
            if (connectedNode == null)
                continue;

            revealedRooms.Add(connectedNode.gridPos);
        }

        RefreshMapVisuals();

        MovePlayerArrowToRoom(roomPosition);
    }

    // Refreshes minimap room colours and icons.
    private void RefreshMapVisuals()
    {
        foreach (KeyValuePair<Vector2Int, MinimapRoomUI> pair in mapRooms)
        {
            Vector2Int gridPosition = pair.Key;
            MinimapRoomUI roomUI = pair.Value;

            if (roomUI == null)
                continue;

            bool isRevealed = revealedRooms.Contains(gridPosition);
            bool isVisited = visitedRooms.Contains(gridPosition);

            roomUI.SetVisible(isRevealed);

            if (!isRevealed)
                continue;

            RoomNode node = roomNodes[gridPosition];

            if (node.isStart && isVisited)
                roomUI.SetColour(startRoomColour);
            else if (isVisited)
                roomUI.SetColour(visitedColour);
            else
                roomUI.SetColour(revealedButUnvisitedColour);

            roomUI.SetImportantIcon(bossRoomIcon, node.isBoss);
        }
    }

    // Moves the player arrow to a room square.
    private void MovePlayerArrowToRoom(Vector2Int roomPosition)
    {
        if (playerArrow == null)
            return;

        if (!revealedRooms.Contains(roomPosition))
        {
            playerArrow.gameObject.SetActive(false);
            return;
        }

        if (!mapRooms.TryGetValue(roomPosition, out MinimapRoomUI roomUI) || roomUI == null)
        {
            playerArrow.gameObject.SetActive(false);
            return;
        }

        playerArrow.SetParent(roomUI.RectTransform, false);

        SetupPlayerArrow();

        playerArrow.anchoredPosition = playerArrowVisualOffset;

        playerArrow.gameObject.SetActive(true);

        playerArrow.SetAsLastSibling();

        roomUI.RectTransform.SetAsLastSibling();

        playerArrow.SetAsLastSibling();

        UpdatePlayerArrowRotation();

        if (debugArrowMovement)
        {
            Debug.Log(
                $"Minimap arrow parented to room {roomPosition}. " +
                $"Arrow local position: {playerArrow.anchoredPosition}."
            );
        }
    }

    // Rotates the player arrow to match player direction.
    private void UpdatePlayerArrowRotation()
    {
        if (playerArrow == null)
            return;

        if (!playerArrow.gameObject.activeSelf)
            return;

        if (PlayerController.Instance == null)
            return;

        playerArrow.SetAsLastSibling();

        Vector3 forward = PlayerController.Instance.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return;

        forward.Normalize();

        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        playerArrow.localEulerAngles = new Vector3(
            0f,
            0f,
            -angle + playerArrowRotationOffset
        );
    }
}