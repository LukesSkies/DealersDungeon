using System.Collections.Generic;
using UnityEngine;

// This script stores the active dungeon at runtime.
//
// It knows:
// - which rooms exist
// - which logical room nodes exist
// - which rooms have been visited
// - what room the player is currently in
//
// It also starts combat when entering rooms for the first time.
public class DungeonManager : MonoBehaviour
{
    // Singleton reference so other scripts can access the current dungeon.
    public static DungeonManager Instance;

    [Header("Combat")]

    // If true, entering the start room can begin combat.
    // If false, the start room will not start combat.
    public bool startRoomStartsCombat = false;

    // If true, first time entering a room starts combat.
    public bool firstVisitStartsCombat = true;

    // World-space room instances, keyed by grid position.
    private Dictionary<Vector2Int, Room> rooms = new();

    // Logical room graph nodes, keyed by grid position.
    private Dictionary<Vector2Int, RoomNode> roomNodes = new();

    // Stores every room the player has already entered.
    private HashSet<Vector2Int> visited = new();

    // The room the player is currently in.
    private Vector2Int currentRoom;

    private void Awake()
    {
        // Set up singleton reference.
        Instance = this;
    }

    // Registers a newly spawned dungeon.
    //
    // This is called after the dungeon has been generated and spawned.
    public void RegisterRooms(Dictionary<Vector2Int, Room> spawnedRooms, List<RoomNode> nodes)
    {
        // Copy the room dictionary.
        rooms = new Dictionary<Vector2Int, Room>(spawnedRooms);

        // Reset node and visited data for the new floor.
        roomNodes.Clear();
        visited.Clear();

        // Store all room nodes by grid position.
        foreach (RoomNode node in nodes)
        {
            roomNodes[node.gridPos] = node;
        }

        // Reset current room to the dungeon origin.
        currentRoom = Vector2Int.zero;
    }

    // Returns the spawned Room component at a grid position.
    public Room GetRoom(Vector2Int position)
    {
        rooms.TryGetValue(position, out Room room);
        return room;
    }

    // Returns true if a spawned room exists at this grid position.
    public bool HasRoom(Vector2Int position)
    {
        return rooms.ContainsKey(position);
    }

    // Returns true if the player has already visited this room.
    public bool IsVisited(Vector2Int position)
    {
        return visited.Contains(position);
    }

    // Tries to get the logical RoomNode for a grid position.
    public bool TryGetRoomNode(Vector2Int position, out RoomNode node)
    {
        return roomNodes.TryGetValue(position, out node);
    }

    // Returns a list of positions connected to the given room.
    //
    // This is useful for the minimap, because the minimap reveals neighbouring rooms
    // when a room is entered.
    public List<Vector2Int> GetConnectedRoomPositions(Vector2Int position)
    {
        List<Vector2Int> connectedPositions = new List<Vector2Int>();

        if (!roomNodes.TryGetValue(position, out RoomNode node))
            return connectedPositions;

        foreach (RoomNode connectedNode in node.connections.Values)
        {
            if (connectedNode != null)
                connectedPositions.Add(connectedNode.gridPos);
        }

        return connectedPositions;
    }

    // Returns true if movement from one room to another is valid.
    //
    // This checks:
    // - both rooms exist
    // - the direction between them is valid
    // - the first room connects to the second
    // - the second room has the opposite matching connection
    public bool CanMove(Vector2Int from, Vector2Int to)
    {
        if (!roomNodes.TryGetValue(from, out RoomNode fromNode))
            return false;

        if (!roomNodes.TryGetValue(to, out RoomNode toNode))
            return false;

        Vector2Int delta = to - from;

        if (!DirectionUtility.TryFromGridVector(delta, out Direction direction))
            return false;

        Direction opposite = DirectionUtility.Opposite(direction);

        if (!fromNode.TryGetConnection(direction, out RoomNode connectedNode))
            return false;

        if (connectedNode != toNode)
            return false;

        return toNode.HasConnection(opposite);
    }

    // Called when the player enters a room.
    public void EnterRoom(Vector2Int position)
    {
        // Update the current room.
        currentRoom = position;

        // Stop if no spawned room exists there.
        if (!rooms.ContainsKey(position))
            return;

        // Check if this is the first time the player entered this room.
        bool firstTimeEnteringThisRoom = !visited.Contains(position);

        // Mark this room as visited if needed.
        if (firstTimeEnteringThisRoom)
            visited.Add(position);

        // Update the minimap every time the player enters a room.
        //
        // This is important:
        // If the player walks back into an already visited room,
        // the minimap arrow still needs to move back to that room.
        if (DungeonMinimapUI.Instance != null)
            DungeonMinimapUI.Instance.RevealFromRoom(position);

        // If this room was already visited,
        // do not trigger first-visit behaviour again.
        if (!firstTimeEnteringThisRoom)
            return;

        // Stop here if rooms should not start combat on first visit.
        if (!firstVisitStartsCombat)
            return;

        // Do not start combat in the start room if disabled.
        if (position == Vector2Int.zero && !startRoomStartsCombat)
            return;

        // Start combat in the room.
        StartCombat(position);
    }

    // Returns the world-space center of the given room.
    public Vector3 GetRoomCenter(Vector2Int position)
    {
        if (rooms.TryGetValue(position, out Room room))
            return room.GetCenter();

        return Vector3.zero;
    }

    // Returns true if the current room is a boss room.
    public bool IsCurrentRoomBoss()
    {
        if (!rooms.TryGetValue(currentRoom, out Room room))
            return false;

        return room.isBossRoom;
    }

    // Returns the current room grid position.
    public Vector2Int GetCurrentRoomPosition()
    {
        return currentRoom;
    }

    // Starts combat in a room.
    private void StartCombat(Vector2Int position)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Combat);

        if (rooms.TryGetValue(position, out Room room))
        {
            EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();

            if (spawner != null && PlayerController.Instance != null)
            {
                int count = Random.Range(1, 4);

                spawner.SpawnEnemiesFacingPlayer(
                    PlayerController.Instance.transform,
                    count
                );
            }
        }

        if (HandManager.Instance != null)
            HandManager.Instance.StartCombatHand();
    }
}