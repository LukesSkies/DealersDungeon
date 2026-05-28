using System.Collections.Generic;
using UnityEngine;

// This script builds and updates the minimap from the real dungeon layout.
//
// It uses the exact same RoomNode grid positions as the generated dungeon.
//
// Behaviour:
// - the start room appears in the dead centre of the minimap
// - rooms connected to the start room appear grey immediately
// - when the player enters a room, that room becomes visited
// - entering a room reveals every room connected to it
// - boss rooms show a boss icon when revealed
// - the player icon is parented directly to the current room square
// - the player icon rotates based on where the player is looking
public class DungeonMinimapUI : MonoBehaviour
{
    // Singleton reference so DungeonManager / DungeonRunManager can update the minimap.
    public static DungeonMinimapUI Instance;

    [Header("References")]

    // Parent object for all minimap room squares.
    //
    // This should be a child of your minimap background.
    //
    // Important:
    // Put this dead centre inside the minimap background.
    [SerializeField] private RectTransform mapContainer;

    // Prefab used for each minimap room square.
    //
    // This prefab must have MinimapRoomUI on it.
    [SerializeField] private MinimapRoomUI roomPrefab;

    // The player arrow image.
    //
    // This should be a UI Image RectTransform.
    //
    // Important:
    // This script will parent the arrow directly inside the current room square.
    // That fixes the "moving double distance" issue because the arrow no longer
    // needs to calculate or copy map-space positions.
    [SerializeField] private RectTransform playerArrow;

    [Header("Icons")]

    // Icon displayed on top of boss room squares.
    [SerializeField] private Sprite bossRoomIcon;

    [Header("Layout")]

    // Size of each minimap room square.
    //
    // Reduce this if rooms look too large.
    //
    // Good values:
    // 10 x 10 = small
    // 14 x 14 = readable
    // 18 x 18 = medium
    [SerializeField] private Vector2 roomSize = new Vector2(14f, 14f);

    // Distance between room square centres on the minimap.
    //
    // This should usually be bigger than roomSize.
    //
    // Example:
    // roomSize = 14
    // roomSpacing = 20
    [SerializeField] private float roomSpacing = 20f;

    [Header("Colours")]

    // Colour used for rooms that have been revealed but not entered yet.
    //
    // This should be your grey colour.
    [SerializeField] private Color revealedButUnvisitedColour = new Color(0.35f, 0.35f, 0.35f, 1f);

    // Colour used for rooms the player has actually entered.
    [SerializeField] private Color visitedColour = Color.white;

    // Colour used specifically for the start room after it is visited.
    [SerializeField] private Color startRoomColour = new Color(0.2f, 0.8f, 1f, 1f);

    [Header("Player Arrow")]

    // Size of the player arrow.
    //
    // Keep this smaller than the room square.
    [SerializeField] private Vector2 playerArrowSize = new Vector2(10f, 10f);

    // Offset for the arrow graphic inside the room square.
    //
    // Keep this at 0,0.
    //
    // Only change this if the arrow sprite itself has transparent padding.
    [SerializeField] private Vector2 playerArrowVisualOffset = Vector2.zero;

    // If your arrow sprite points up by default, leave this at 0.
    //
    // If your arrow sprite points right/left/down by default,
    // adjust this until it visually matches the player's facing direction.
    [SerializeField] private float playerArrowRotationOffset = 0f;

    [Header("Debug")]

    // If true, logs which room the minimap arrow moves to.
    //
    // Turn this off once the map is working.
    [SerializeField] private bool debugArrowMovement = false;

    // Every minimap room square that was created.
    //
    // Key = dungeon grid position.
    // Value = minimap square.
    private Dictionary<Vector2Int, MinimapRoomUI> mapRooms = new Dictionary<Vector2Int, MinimapRoomUI>();

    // The real dungeon layout data.
    //
    // Key = dungeon grid position.
    // Value = RoomNode from the dungeon generator.
    private Dictionary<Vector2Int, RoomNode> roomNodes = new Dictionary<Vector2Int, RoomNode>();

    // Rooms that are currently visible on the minimap.
    //
    // A room becomes revealed when:
    // - the player enters it
    // - or it is directly connected to a room the player just entered
    private HashSet<Vector2Int> revealedRooms = new HashSet<Vector2Int>();

    // Rooms the player has actually entered.
    private HashSet<Vector2Int> visitedRooms = new HashSet<Vector2Int>();

    // The start room grid position.
    //
    // Usually this is (0, 0).
    private Vector2Int startGridPosition = Vector2Int.zero;

    // The last room the player arrow was moved to.
    private Vector2Int currentArrowRoomPosition = Vector2Int.zero;

    private void Awake()
    {
        // Set up singleton reference.
        Instance = this;

        // Prepare the arrow RectTransform.
        SetupPlayerArrow();

        // Hide the player arrow until the map is built and revealed.
        if (playerArrow != null)
            playerArrow.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Only rotate the arrow every frame.
        //
        // The arrow position is moved only when RevealFromRoom() is called.
        UpdatePlayerArrowRotation();
    }

    // Prepares the player arrow RectTransform.
    private void SetupPlayerArrow()
    {
        if (playerArrow == null)
            return;

        // Force clean UI transform values.
        playerArrow.localScale = Vector3.one;

        // Force centre anchors/pivot.
        //
        // Since the arrow will be parented to a room square,
        // anchoredPosition 0,0 means dead centre of that room square.
        playerArrow.anchorMin = new Vector2(0.5f, 0.5f);
        playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrow.pivot = new Vector2(0.5f, 0.5f);

        // Set arrow size.
        playerArrow.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, playerArrowSize.x);
        playerArrow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, playerArrowSize.y);

        // Start centred.
        playerArrow.anchoredPosition = playerArrowVisualOffset;

        // Make sure the arrow draws above whatever room square it is parented to.
        playerArrow.SetAsLastSibling();
    }

    // Builds the minimap from the generated dungeon layout.
    //
    // This should be called once after a new dungeon floor is generated.
    public void BuildMap(List<RoomNode> nodes, Vector2Int startPosition)
    {
        // Clear old minimap data first.
        ClearMap();

        // Store the start position.
        //
        // This is used so the start room appears dead centre on the minimap.
        startGridPosition = startPosition;

        // Reset the arrow room position.
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

        // Temporarily keep the arrow under the map container while the map is rebuilt.
        //
        // This prevents the arrow from being destroyed if it was previously parented
        // inside an old minimap room square.
        if (playerArrow != null)
        {
            playerArrow.SetParent(mapContainer, false);
            playerArrow.gameObject.SetActive(false);
            SetupPlayerArrow();
        }

        // Create one minimap square for every room in the dungeon layout.
        //
        // Every square starts hidden.
        // Rooms only become visible when revealed.
        foreach (RoomNode node in nodes)
        {
            if (node == null)
                continue;

            // Store this room node by grid position.
            roomNodes[node.gridPos] = node;

            // Spawn one minimap room square.
            MinimapRoomUI roomUI = Instantiate(roomPrefab, mapContainer);

            // Force the minimap square size.
            //
            // This prevents the prefab from appearing huge because of its original RectTransform size.
            roomUI.SetSize(roomSize);

            // Convert the dungeon grid position into minimap UI position.
            //
            // The start room becomes position (0, 0),
            // which is the centre of the MapContainer.
            Vector2 minimapPosition = GridToMinimapPosition(node.gridPos);

            // Move the square to its minimap position.
            roomUI.SetAnchoredPosition(minimapPosition);

            // Hide every room at first.
            roomUI.SetVisible(false);

            // Hide boss/important icons at first.
            roomUI.SetImportantIcon(bossRoomIcon, false);

            // Store the UI square.
            mapRooms[node.gridPos] = roomUI;
        }

        // Hide player arrow until the start room is revealed.
        if (playerArrow != null)
            playerArrow.gameObject.SetActive(false);
    }

    // Clears all minimap rooms and runtime map data.
    public void ClearMap()
    {
        // If the arrow is currently parented inside a room square,
        // move it back to the map container before destroying old room squares.
        //
        // Otherwise the arrow could be destroyed with the old room square.
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

    // Converts a dungeon grid position into a minimap UI position.
    //
    // The important part:
    // startGridPosition becomes Vector2.zero.
    //
    // That means the start room is always dead centre of the minimap.
    private Vector2 GridToMinimapPosition(Vector2Int gridPosition)
    {
        Vector2Int relativePosition = gridPosition - startGridPosition;

        return new Vector2(
            relativePosition.x * roomSpacing,
            relativePosition.y * roomSpacing
        );
    }

    // Call this when the player enters a room.
    //
    // It:
    // - marks the entered room as visited
    // - reveals the entered room
    // - reveals every room directly connected to it
    // - refreshes room colours and boss icons
    // - moves the player arrow to this exact room
    public void RevealFromRoom(Vector2Int roomPosition)
    {
        if (!roomNodes.ContainsKey(roomPosition))
            return;

        // Store the confirmed arrow room position.
        currentArrowRoomPosition = roomPosition;

        // The room the player entered is now visited.
        visitedRooms.Add(roomPosition);

        // The room the player entered is visible.
        revealedRooms.Add(roomPosition);

        RoomNode currentNode = roomNodes[roomPosition];

        // Reveal every room directly connected to this room.
        //
        // Example:
        // If the start room connects Up, Down, Right, and Left,
        // those four rooms become visible immediately.
        foreach (RoomNode connectedNode in currentNode.connections.Values)
        {
            if (connectedNode == null)
                continue;

            revealedRooms.Add(connectedNode.gridPos);
        }

        // Refresh the whole minimap after reveal data changes.
        RefreshMapVisuals();

        // Move the player arrow to the exact centre of the confirmed room square.
        MovePlayerArrowToRoom(roomPosition);
    }

    // Refreshes every room square on the minimap.
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

            // Rooms that have not been revealed are completely hidden.
            roomUI.SetVisible(isRevealed);

            if (!isRevealed)
                continue;

            RoomNode node = roomNodes[gridPosition];

            // Visited rooms get visited colour.
            // Revealed but unvisited rooms get grey colour.
            if (node.isStart && isVisited)
                roomUI.SetColour(startRoomColour);
            else if (isVisited)
                roomUI.SetColour(visitedColour);
            else
                roomUI.SetColour(revealedButUnvisitedColour);

            // Boss icon appears in the centre of the square when the boss room is revealed.
            roomUI.SetImportantIcon(bossRoomIcon, node.isBoss);
        }
    }

    // Moves the player arrow to the exact centre of a specific minimap room.
    //
    // Important:
    // This does NOT copy anchoredPosition anymore.
    //
    // Instead, it parents the arrow directly to the room square.
    // Then anchoredPosition 0,0 means the centre of that square.
    //
    // This fixes the "moving double the distance" problem.
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

        // Parent the arrow directly to the current room square.
        //
        // This makes the arrow use the room square's local coordinate space.
        playerArrow.SetParent(roomUI.RectTransform, false);

        // Re-apply clean transform settings after reparenting.
        SetupPlayerArrow();

        // Centre the arrow inside the room square.
        playerArrow.anchoredPosition = playerArrowVisualOffset;

        // Show the arrow.
        playerArrow.gameObject.SetActive(true);

        // Keep arrow above the room image and boss icon.
        playerArrow.SetAsLastSibling();

        // Bring the whole current room square above neighbouring squares.
        //
        // This prevents nearby room squares from drawing over the arrow.
        roomUI.RectTransform.SetAsLastSibling();

        // After moving the room square to the front,
        // make the arrow last inside that square again.
        playerArrow.SetAsLastSibling();

        // Update rotation immediately too.
        UpdatePlayerArrowRotation();

        if (debugArrowMovement)
        {
            Debug.Log(
                $"Minimap arrow parented to room {roomPosition}. " +
                $"Arrow local position: {playerArrow.anchoredPosition}."
            );
        }
    }

    // Rotates the player arrow based on where the player is looking.
    //
    // This does not move the arrow between rooms.
    private void UpdatePlayerArrowRotation()
    {
        if (playerArrow == null)
            return;

        if (!playerArrow.gameObject.activeSelf)
            return;

        if (PlayerController.Instance == null)
            return;

        // Make sure the arrow stays above the current room square's children.
        playerArrow.SetAsLastSibling();

        // Rotate the arrow based on the player object's forward direction.
        Vector3 forward = PlayerController.Instance.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return;

        forward.Normalize();

        // Converts world direction to a 2D UI angle.
        //
        // Facing +Z / Up    = 0 degrees
        // Facing +X / Right = 90 degrees
        // Facing -Z / Down  = 180 degrees
        // Facing -X / Left  = -90 degrees
        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        // UI rotation is usually opposite sign on Z,
        // so this keeps the arrow visually matching world direction.
        playerArrow.localEulerAngles = new Vector3(
            0f,
            0f,
            -angle + playerArrowRotationOffset
        );
    }
}