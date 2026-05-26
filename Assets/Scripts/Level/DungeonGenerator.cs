using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Layout Settings")]
    public int roomCount = 12;

    [Range(0f, 1f)]
    public float extraConnectionChance = 0.15f;

    private readonly Direction[] directions =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West
    };

    public List<RoomNode> Generate()
    {
        Dictionary<Vector2Int, RoomNode> map = new();

        RoomNode start = new RoomNode
        {
            gridPos = Vector2Int.zero,
            isStart = true
        };

        map[start.gridPos] = start;

        List<RoomNode> expandableRooms = new()
        {
            start
        };

        while (map.Count < roomCount && expandableRooms.Count > 0)
        {
            RoomNode parent = expandableRooms[Random.Range(0, expandableRooms.Count)];

            List<Direction> validDirections = GetValidExpansionDirections(parent, map);

            if (validDirections.Count == 0)
            {
                expandableRooms.Remove(parent);
                continue;
            }

            Direction direction = validDirections[Random.Range(0, validDirections.Count)];
            Vector2Int newPos = parent.gridPos + DirectionUtility.ToVector(direction);

            RoomNode newRoom = new RoomNode
            {
                gridPos = newPos
            };

            map[newPos] = newRoom;

            parent.Connect(direction, newRoom);

            expandableRooms.Add(newRoom);
        }

        AddOptionalLoops(map);

        RoomNode bossRoom = GetFurthestRoomByPathDistance(start);
        bossRoom.isBoss = true;

        return new List<RoomNode>(map.Values);
    }

    private List<Direction> GetValidExpansionDirections(RoomNode room, Dictionary<Vector2Int, RoomNode> map)
    {
        List<Direction> valid = new();

        foreach (Direction direction in directions)
        {
            Vector2Int targetPos = room.gridPos + DirectionUtility.ToVector(direction);

            if (map.ContainsKey(targetPos))
                continue;

            valid.Add(direction);
        }

        return valid;
    }

    private void AddOptionalLoops(Dictionary<Vector2Int, RoomNode> map)
    {
        foreach (RoomNode room in map.Values)
        {
            foreach (Direction direction in directions)
            {
                if (room.HasConnection(direction))
                    continue;

                Vector2Int neighborPos = room.gridPos + DirectionUtility.ToVector(direction);

                if (!map.TryGetValue(neighborPos, out RoomNode neighbor))
                    continue;

                Direction opposite = DirectionUtility.Opposite(direction);

                if (neighbor.HasConnection(opposite))
                    continue;

                if (Random.value <= extraConnectionChance)
                {
                    room.Connect(direction, neighbor);
                }
            }
        }
    }

    private RoomNode GetFurthestRoomByPathDistance(RoomNode start)
    {
        Queue<RoomNode> queue = new();
        Dictionary<RoomNode, int> distances = new();

        queue.Enqueue(start);
        distances[start] = 0;

        RoomNode furthest = start;
        int furthestDistance = 0;

        while (queue.Count > 0)
        {
            RoomNode current = queue.Dequeue();
            int currentDistance = distances[current];

            if (currentDistance > furthestDistance)
            {
                furthestDistance = currentDistance;
                furthest = current;
            }

            foreach (RoomNode connected in current.connections.Values)
            {
                if (distances.ContainsKey(connected))
                    continue;

                distances[connected] = currentDistance + 1;
                queue.Enqueue(connected);
            }
        }

        return furthest;
    }
}