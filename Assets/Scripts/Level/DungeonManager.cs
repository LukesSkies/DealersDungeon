using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    private Dictionary<Vector2Int, Room> rooms = new();
    private Dictionary<Vector2Int, RoomNode> roomNodes = new();

    private HashSet<Vector2Int> visited = new();

    private Vector2Int currentRoom;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterRooms(Dictionary<Vector2Int, Room> spawnedRooms, List<RoomNode> nodes)
    {
        rooms = spawnedRooms;
        roomNodes.Clear();
        visited.Clear();

        foreach (RoomNode node in nodes)
        {
            roomNodes[node.gridPos] = node;
        }
    }

    public Room GetRoom(Vector2Int position)
    {
        rooms.TryGetValue(position, out Room room);
        return room;
    }

    public bool HasRoom(Vector2Int position)
    {
        return rooms.ContainsKey(position);
    }

    public bool CanMove(Vector2Int from, Vector2Int to)
    {
        if (!roomNodes.TryGetValue(from, out RoomNode fromNode))
            return false;

        if (!roomNodes.TryGetValue(to, out RoomNode toNode))
            return false;

        Vector2Int delta = to - from;

        Direction direction = DirectionUtility.FromVector(delta);
        Direction opposite = DirectionUtility.Opposite(direction);

        return fromNode.HasConnection(direction) && toNode.HasConnection(opposite);
    }

    public void EnterRoom(Vector2Int position)
    {
        currentRoom = position;

        if (visited.Contains(position))
            return;

        visited.Add(position);

        StartCombat(position);
    }

    public Vector3 GetRoomCenter(Vector2Int position)
    {
        if (rooms.TryGetValue(position, out Room room))
            return room.GetCenter();

        return Vector3.zero;
    }

    public bool IsCurrentRoomBoss()
    {
        if (!rooms.TryGetValue(currentRoom, out Room room))
            return false;

        return room.isBossRoom;
    }

    private void StartCombat(Vector2Int position)
    {
        GameManager.Instance.SetState(GameState.Combat);

        if (rooms.TryGetValue(position, out Room room))
        {
            EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();

            if (spawner != null)
            {
                int count = Random.Range(1, 4);

                spawner.SpawnEnemiesFacingPlayer(
                    PlayerController.Instance.transform,
                    count
                );
            }
        }

        HandManager.Instance.StartCombatHand();
    }
}