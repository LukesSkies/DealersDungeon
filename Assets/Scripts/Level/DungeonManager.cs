using System.Collections.Generic;
using UnityEngine;

// Stores and manages the current dungeon.
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    [Header("Combat")]
    public bool startRoomStartsCombat = false;
    public bool firstVisitStartsCombat = true;

    private Dictionary<Vector2Int, Room> rooms = new Dictionary<Vector2Int, Room>();
    private Dictionary<Vector2Int, RoomNode> roomNodes = new Dictionary<Vector2Int, RoomNode>();
    private HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

    private Vector2Int currentRoom;

    private void Awake()
    {
        Instance = this;
    }

    // Registers a freshly spawned dungeon.
    public void RegisterRooms(Dictionary<Vector2Int, Room> spawnedRooms, List<RoomNode> nodes)
    {
        rooms = new Dictionary<Vector2Int, Room>(spawnedRooms);

        roomNodes.Clear();
        visited.Clear();

        foreach (RoomNode node in nodes)
        {
            if (node != null)
                roomNodes[node.gridPos] = node;
        }

        currentRoom = Vector2Int.zero;
    }

    // Gets a room at a grid position.
    public Room GetRoom(Vector2Int position)
    {
        rooms.TryGetValue(position, out Room room);
        return room;
    }

    // Checks if a room exists.
    public bool HasRoom(Vector2Int position)
    {
        return rooms.ContainsKey(position);
    }

    // Checks if a room has been visited.
    public bool IsVisited(Vector2Int position)
    {
        return visited.Contains(position);
    }

    // Gets room node data.
    public bool TryGetRoomNode(Vector2Int position, out RoomNode node)
    {
        return roomNodes.TryGetValue(position, out node);
    }

    // Gets connected room positions.
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

    // Checks if movement between rooms is valid.
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
        currentRoom = position;

        if (!rooms.ContainsKey(position))
            return;

        bool firstTimeEnteringThisRoom = !visited.Contains(position);

        if (firstTimeEnteringThisRoom)
            visited.Add(position);

        if (DungeonMinimapUI.Instance != null)
            DungeonMinimapUI.Instance.RevealFromRoom(position);

        if (!firstTimeEnteringThisRoom)
            return;

        if (!firstVisitStartsCombat)
            return;

        if (position == Vector2Int.zero && !startRoomStartsCombat)
            return;

        StartCombat(position);
    }

    // Gets the room center.
    public Vector3 GetRoomCenter(Vector2Int position)
    {
        if (rooms.TryGetValue(position, out Room room))
            return room.GetCenter();

        return Vector3.zero;
    }

    // Checks if the current room is the boss room.
    public bool IsCurrentRoomBoss()
    {
        if (!rooms.TryGetValue(currentRoom, out Room room))
            return false;

        return room.isBossRoom;
    }

    // Gets the current room position.
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
                int floorNumber = DungeonRunManager.Instance == null
                    ? 1
                    : DungeonRunManager.Instance.CurrentFloor;

                spawner.SpawnEnemiesForRoom(
                    room,
                    PlayerController.Instance.transform,
                    floorNumber
                );
            }
        }

        if (HandManager.Instance != null)
            HandManager.Instance.StartCombatHand();
    }
}