using System.Collections.Generic;
using UnityEngine;

// Controls how the boss room is chosen.
public enum BossRoomSelectionMode
{
    FurthestLeafFromStart,
    RandomLeaf
}

// Generates dungeon room data.
public class DungeonGenerator : MonoBehaviour
{
    [Header("Layout Settings")]

    // Total rooms to generate.
    [Min(6)]
    public int roomCount = 12;

    // Chance to add extra room connections.
    [Range(0f, 1f)]
    public float extraConnectionChance = 0.15f;

    [Header("Start Room")]

    // Forces the start room to have four connections.
    public bool forceStartRoomFourWay = true;

    [Header("Boss Room")]

    // How the boss room is picked.
    public BossRoomSelectionMode bossRoomSelectionMode = BossRoomSelectionMode.FurthestLeafFromStart;

    // Minimum boss distance from start.
    [Min(1)]
    public int minimumBossDistanceFromStart = 2;

    [Header("Generation Safety")]

    // Stops generation from looping forever.
    public int maxGenerationSafetySteps = 5000;

    // Creates the dungeon layout.
    public List<RoomNode> Generate()
    {
        Dictionary<Vector2Int, RoomNode> map = new Dictionary<Vector2Int, RoomNode>();

        RoomNode start = new RoomNode
        {
            gridPos = Vector2Int.zero,
            isStart = true,
            isBoss = false
        };

        map[start.gridPos] = start;

        if (forceStartRoomFourWay)
            CreateForcedStartConnections(start, map);

        int targetRoomCount = Mathf.Max(roomCount, forceStartRoomFourWay ? 5 : 2);

        List<RoomNode> expandableRooms = new List<RoomNode>(map.Values);

        int safety = 0;

        while (map.Count < targetRoomCount && expandableRooms.Count > 0)
        {
            safety++;

            if (safety > maxGenerationSafetySteps)
            {
                Debug.LogWarning("Dungeon generation safety limit reached.");
                break;
            }

            RoomNode parent = expandableRooms[Random.Range(0, expandableRooms.Count)];

            List<Direction> validDirections = GetValidExpansionDirections(parent, map);

            if (validDirections.Count == 0)
            {
                expandableRooms.Remove(parent);
                continue;
            }

            Direction direction = validDirections[Random.Range(0, validDirections.Count)];

            Vector2Int newPos = parent.gridPos + DirectionUtility.ToGridVector(direction);

            RoomNode newRoom = new RoomNode
            {
                gridPos = newPos,
                isStart = false,
                isBoss = false
            };

            map[newPos] = newRoom;

            parent.Connect(direction, newRoom);

            expandableRooms.Add(newRoom);
        }

        ClearBossFlags(map);

        RoomNode bossRoom = ChooseBossRoom(start, map);

        if (bossRoom != null)
        {
            bossRoom.isBoss = true;

            Debug.Log(
                $"Boss room selected at {bossRoom.gridPos}. " +
                $"Boss door mask: {bossRoom.GetDoorMask()}."
            );
        }
        else
        {
            Debug.LogError("Could not select a boss room.");
        }

        AddOptionalLoops(map, bossRoom);

        return new List<RoomNode>(map.Values);
    }

    // Creates four rooms around the start room.
    private void CreateForcedStartConnections(RoomNode start, Dictionary<Vector2Int, RoomNode> map)
    {
        foreach (Direction direction in DirectionUtility.AllDirections)
        {
            Vector2Int newPos = start.gridPos + DirectionUtility.ToGridVector(direction);

            if (map.ContainsKey(newPos))
                continue;

            RoomNode newRoom = new RoomNode
            {
                gridPos = newPos,
                isStart = false,
                isBoss = false
            };

            map[newPos] = newRoom;

            start.Connect(direction, newRoom);
        }
    }

    // Gets empty directions around a room.
    private List<Direction> GetValidExpansionDirections(RoomNode room, Dictionary<Vector2Int, RoomNode> map)
    {
        List<Direction> validDirections = new List<Direction>();

        foreach (Direction direction in DirectionUtility.AllDirections)
        {
            Vector2Int targetPos = room.gridPos + DirectionUtility.ToGridVector(direction);

            if (map.ContainsKey(targetPos))
                continue;

            validDirections.Add(direction);
        }

        return validDirections;
    }

    // Clears boss flags from all rooms.
    private void ClearBossFlags(Dictionary<Vector2Int, RoomNode> map)
    {
        foreach (RoomNode room in map.Values)
        {
            if (room == null)
                continue;

            room.isBoss = false;
        }
    }

    // Picks the boss room.
    private RoomNode ChooseBossRoom(RoomNode start, Dictionary<Vector2Int, RoomNode> map)
    {
        Dictionary<RoomNode, int> distances = GetDistancesFromStart(start);

        List<RoomNode> validLeafRooms = new List<RoomNode>();

        foreach (RoomNode room in map.Values)
        {
            if (room == null)
                continue;

            if (room.isStart)
                continue;

            if (room.GetConnectionCount() != 1)
                continue;

            int distance = distances.TryGetValue(room, out int value) ? value : 0;

            if (distance < minimumBossDistanceFromStart)
                continue;

            validLeafRooms.Add(room);
        }

        if (validLeafRooms.Count == 0)
        {
            foreach (RoomNode room in map.Values)
            {
                if (room == null)
                    continue;

                if (room.isStart)
                    continue;

                if (room.GetConnectionCount() == 1)
                    validLeafRooms.Add(room);
            }
        }

        if (validLeafRooms.Count == 0)
        {
            foreach (RoomNode room in map.Values)
            {
                if (room == null)
                    continue;

                if (!room.isStart)
                    validLeafRooms.Add(room);
            }
        }

        if (validLeafRooms.Count == 0)
            return null;

        if (bossRoomSelectionMode == BossRoomSelectionMode.RandomLeaf)
            return validLeafRooms[Random.Range(0, validLeafRooms.Count)];

        return GetFurthestLeaf(validLeafRooms, distances);
    }

    // Gets the furthest possible boss room.
    private RoomNode GetFurthestLeaf(List<RoomNode> leafRooms, Dictionary<RoomNode, int> distances)
    {
        int furthestDistance = -1;
        List<RoomNode> furthestRooms = new List<RoomNode>();

        foreach (RoomNode room in leafRooms)
        {
            int distance = distances.TryGetValue(room, out int value) ? value : 0;

            if (distance > furthestDistance)
            {
                furthestDistance = distance;
                furthestRooms.Clear();
                furthestRooms.Add(room);
            }
            else if (distance == furthestDistance)
            {
                furthestRooms.Add(room);
            }
        }

        if (furthestRooms.Count == 0)
            return leafRooms[Random.Range(0, leafRooms.Count)];

        return furthestRooms[Random.Range(0, furthestRooms.Count)];
    }

    // Gets path distance from the start room.
    private Dictionary<RoomNode, int> GetDistancesFromStart(RoomNode start)
    {
        Queue<RoomNode> queue = new Queue<RoomNode>();
        Dictionary<RoomNode, int> distances = new Dictionary<RoomNode, int>();

        queue.Enqueue(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            RoomNode current = queue.Dequeue();
            int currentDistance = distances[current];

            foreach (RoomNode connected in current.connections.Values)
            {
                if (distances.ContainsKey(connected))
                    continue;

                distances[connected] = currentDistance + 1;
                queue.Enqueue(connected);
            }
        }

        return distances;
    }

    // Adds optional loop connections.
    private void AddOptionalLoops(Dictionary<Vector2Int, RoomNode> map, RoomNode bossRoom)
    {
        foreach (RoomNode room in map.Values)
        {
            if (room == null)
                continue;

            if (room == bossRoom)
                continue;

            foreach (Direction direction in DirectionUtility.AllDirections)
            {
                if (room.HasConnection(direction))
                    continue;

                Vector2Int neighborPos = room.gridPos + DirectionUtility.ToGridVector(direction);

                if (!map.TryGetValue(neighborPos, out RoomNode neighbor))
                    continue;

                if (neighbor == bossRoom)
                    continue;

                Direction opposite = DirectionUtility.Opposite(direction);

                if (neighbor.HasConnection(opposite))
                    continue;

                if (Random.value <= extraConnectionChance)
                    room.Connect(direction, neighbor);
            }
        }
    }
}