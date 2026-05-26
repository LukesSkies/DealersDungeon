using System.Collections.Generic;
using UnityEngine;

// This script creates the dungeon layout.
//
// It does NOT spawn visible rooms.
// It only creates RoomNode data saying:
// - where each room is on the grid
// - which rooms connect to each other
// - which room is the start room
// - which room is the boss room
public class DungeonGenerator : MonoBehaviour
{
    [Header("Layout Settings")]

    // Total number of rooms to generate.
    //
    // If bossCountsTowardRoomCount is true,
    // this includes the boss room.
    //
    // If bossCountsTowardRoomCount is false,
    // the boss room is added after this many normal rooms.
    [Min(6)]
    public int roomCount = 12;

    // Chance to add extra connections between rooms that are already next to each other.
    //
    // 0 = no extra loops, more linear dungeon.
    // 1 = lots of loops, more connected dungeon.
    [Range(0f, 1f)]
    public float extraConnectionChance = 0.15f;

    [Header("Start Room")]

    // If true, the start room always has connections in all four directions:
    // Up, Down, Right, and Left.
    //
    // This means your start room prefab should support all four doors.
    public bool forceStartRoomFourWay = true;

    [Header("Boss Room")]

    // If true, the boss room will always be placed above another room.
    //
    // This makes the boss room need only a Down door.
    //
    // Example:
    //
    // Boss Room
    //    |
    // Parent Room
    //
    // Boss has Down connection.
    // Parent has Up connection.
    [Tooltip("Boss room will always be placed above another room, so the boss room only needs a Down door.")]
    public bool forceBossRoomAboveDungeon = true;

    // If true, the boss room is included in roomCount.
    //
    // Example:
    // roomCount = 12
    // bossCountsTowardRoomCount = true
    // Result = 11 normal/start rooms + 1 boss room
    //
    // If false:
    // roomCount = 12
    // bossCountsTowardRoomCount = false
    // Result = 12 normal/start rooms + 1 boss room
    [Tooltip("If true, the boss room is included inside roomCount. If false, boss is added on top of roomCount.")]
    public bool bossCountsTowardRoomCount = true;

    [Header("Generation Safety")]

    // Prevents an infinite loop if something goes wrong during generation.
    public int maxGenerationSafetySteps = 5000;

    // Main generation function.
    //
    // Other scripts call this to get the dungeon layout.
    //
    // Returns a list of RoomNodes.
    // DungeonSpawner then uses those nodes to spawn actual room prefabs.
    public List<RoomNode> Generate()
    {
        // Stores rooms by their grid position.
        //
        // Key = grid position.
        // Value = RoomNode at that position.
        Dictionary<Vector2Int, RoomNode> map = new Dictionary<Vector2Int, RoomNode>();

        // Create the starting room at the center of the dungeon grid.
        RoomNode start = new RoomNode
        {
            gridPos = Vector2Int.zero,
            isStart = true,
            isBoss = false
        };

        // Add the start room to the map.
        map[start.gridPos] = start;

        // Force the start room to connect in all four directions if enabled.
        if (forceStartRoomFourWay)
            CreateForcedStartConnections(start, map);

        // Work out how many non-boss rooms should be generated.
        int targetNormalRoomCount = roomCount;

        // If the boss room counts toward the total room count,
        // generate one fewer normal room because the boss will be added later.
        if (bossCountsTowardRoomCount)
            targetNormalRoomCount = Mathf.Max(5, roomCount - 1);

        // Make sure the minimum count still supports the forced four-way start.
        targetNormalRoomCount = Mathf.Max(targetNormalRoomCount, forceStartRoomFourWay ? 5 : 2);

        // Rooms that can still be expanded from.
        //
        // The generator randomly picks from this list and tries to add new rooms nearby.
        List<RoomNode> expandableRooms = new List<RoomNode>(map.Values);

        int safety = 0;

        // Keep adding rooms until the dungeon reaches the target count,
        // or until there are no rooms left that can expand.
        while (map.Count < targetNormalRoomCount && expandableRooms.Count > 0)
        {
            safety++;

            // Stop if the generator somehow runs too long.
            if (safety > maxGenerationSafetySteps)
            {
                Debug.LogWarning("Dungeon generation safety limit reached.");
                break;
            }

            // Pick a random existing room to expand from.
            RoomNode parent = expandableRooms[Random.Range(0, expandableRooms.Count)];

            // Find all empty directions around this room.
            List<Direction> validDirections = GetValidExpansionDirections(parent, map);

            // If this room has no empty neighbouring spaces,
            // remove it from the expandable list.
            if (validDirections.Count == 0)
            {
                expandableRooms.Remove(parent);
                continue;
            }

            // Pick one valid direction randomly.
            Direction direction = validDirections[Random.Range(0, validDirections.Count)];

            // Calculate the grid position of the new room.
            Vector2Int newPos = parent.gridPos + DirectionUtility.ToGridVector(direction);

            // Create the new room node.
            RoomNode newRoom = new RoomNode
            {
                gridPos = newPos,
                isStart = false,
                isBoss = false
            };

            // Add it to the map.
            map[newPos] = newRoom;

            // Connect the parent room to the new room.
            //
            // This also creates the opposite connection automatically.
            //
            // Example:
            // parent.Connect(Up, newRoom)
            // gives parent an Up connection
            // and newRoom a Down connection.
            parent.Connect(direction, newRoom);

            // The new room can now also be expanded from.
            expandableRooms.Add(newRoom);
        }

        // Place or mark the boss room.
        if (forceBossRoomAboveDungeon)
            CreateBossRoomAboveDungeon(map);
        else
            MarkFurthestRoomAsBoss(start);

        // Add optional loop connections after the main layout exists.
        AddOptionalLoops(map);

        // Return all generated rooms as a list.
        return new List<RoomNode>(map.Values);
    }

    // Creates four rooms around the start room.
    //
    // This forces the start room to have:
    // - Up connection
    // - Down connection
    // - Right connection
    // - Left connection
    private void CreateForcedStartConnections(RoomNode start, Dictionary<Vector2Int, RoomNode> map)
    {
        foreach (Direction direction in DirectionUtility.AllDirections)
        {
            // Find the position one step away from the start room.
            Vector2Int newPos = start.gridPos + DirectionUtility.ToGridVector(direction);

            // If there is somehow already a room there, skip it.
            if (map.ContainsKey(newPos))
                continue;

            // Create a normal room in that direction.
            RoomNode newRoom = new RoomNode
            {
                gridPos = newPos,
                isStart = false,
                isBoss = false
            };

            // Add the new room to the map.
            map[newPos] = newRoom;

            // Connect start to the new room.
            start.Connect(direction, newRoom);
        }
    }

    // Gets every direction around a room where there is currently no room.
    private List<Direction> GetValidExpansionDirections(RoomNode room, Dictionary<Vector2Int, RoomNode> map)
    {
        List<Direction> validDirections = new List<Direction>();

        foreach (Direction direction in DirectionUtility.AllDirections)
        {
            Vector2Int targetPos = room.gridPos + DirectionUtility.ToGridVector(direction);

            // If a room already exists in that direction, we cannot create another one there.
            if (map.ContainsKey(targetPos))
                continue;

            validDirections.Add(direction);
        }

        return validDirections;
    }

    // Creates the boss room above a valid parent room.
    //
    // This guarantees the boss room has only a Down door.
    private void CreateBossRoomAboveDungeon(Dictionary<Vector2Int, RoomNode> map)
    {
        // Find the best room to place the boss above.
        RoomNode bossParent = FindBestBossParent(map);

        if (bossParent == null)
        {
            Debug.LogError("Could not find a valid boss parent room.");
            return;
        }

        // Boss is placed one grid space above the parent room.
        Vector2Int bossPos = bossParent.gridPos + DirectionUtility.ToGridVector(Direction.Up);

        // Safety check.
        // This should not happen because FindBestBossParent already checks this.
        if (map.ContainsKey(bossPos))
        {
            Debug.LogError($"Could not place boss room above {bossParent.gridPos} because {bossPos} is already occupied.");
            return;
        }

        // Create the boss room.
        RoomNode bossRoom = new RoomNode
        {
            gridPos = bossPos,
            isStart = false,
            isBoss = true
        };

        // Add the boss room to the dungeon map.
        map[bossPos] = bossRoom;

        // Connect the parent room upward to the boss room.
        //
        // This means:
        // parent has Up door
        // boss has Down door
        bossParent.Connect(Direction.Up, bossRoom);

        Debug.Log($"Boss room placed at {bossPos}. Boss room mask should be 2, which is Down only.");
    }

    // Finds the best room to connect the boss room to.
    //
    // It looks for a non-start room with empty space above it.
    //
    // It prefers:
    // 1. The highest room on the grid
    // 2. If tied, the room furthest sideways
    private RoomNode FindBestBossParent(Dictionary<Vector2Int, RoomNode> map)
    {
        RoomNode bestRoom = null;

        foreach (RoomNode room in map.Values)
        {
            if (room == null)
                continue;

            // Do not attach the boss directly to the start room.
            if (room.isStart)
                continue;

            // Check if there is empty space above this room.
            Vector2Int bossPos = room.gridPos + DirectionUtility.ToGridVector(Direction.Up);

            if (map.ContainsKey(bossPos))
                continue;

            // First valid room becomes the current best room.
            if (bestRoom == null)
            {
                bestRoom = room;
                continue;
            }

            // Prefer the room with the highest Y grid value.
            //
            // Remember:
            // grid y = world Z.
            bool roomIsHigher = room.gridPos.y > bestRoom.gridPos.y;

            // If both rooms are equally high,
            // prefer the one further away sideways.
            bool roomIsFurtherSideways =
                room.gridPos.y == bestRoom.gridPos.y &&
                Mathf.Abs(room.gridPos.x) > Mathf.Abs(bestRoom.gridPos.x);

            if (roomIsHigher || roomIsFurtherSideways)
                bestRoom = room;
        }

        // Return the best room if one was found.
        if (bestRoom != null)
            return bestRoom;

        // Fallback:
        // If no ideal room was found, use any room with empty space above it.
        foreach (RoomNode room in map.Values)
        {
            if (room == null)
                continue;

            Vector2Int bossPos = room.gridPos + DirectionUtility.ToGridVector(Direction.Up);

            if (!map.ContainsKey(bossPos))
                return room;
        }

        // If every possible boss position is blocked, return null.
        return null;
    }

    // Alternative boss logic.
    //
    // If forceBossRoomAboveDungeon is false,
    // this marks the furthest existing room as the boss room.
    //
    // Warning:
    // This can require a boss prefab with Up, Down, Right, or Left doors depending on where it lands.
    private void MarkFurthestRoomAsBoss(RoomNode start)
    {
        RoomNode bossRoom = GetFurthestRoomByPathDistance(start);

        if (bossRoom == null)
            return;

        if (bossRoom == start)
            return;

        bossRoom.isBoss = true;
    }

    // Adds optional extra connections between rooms that are already neighbours.
    //
    // This creates loops in the dungeon so the layout is less linear.
    private void AddOptionalLoops(Dictionary<Vector2Int, RoomNode> map)
    {
        foreach (RoomNode room in map.Values)
        {
            // Do not add extra connections from the boss room.
            // This keeps the boss room as a single-entrance room.
            if (room.isBoss)
                continue;

            foreach (Direction direction in DirectionUtility.AllDirections)
            {
                // If this room is already connected in this direction, skip it.
                if (room.HasConnection(direction))
                    continue;

                // Check if there is already a neighbouring room in this direction.
                Vector2Int neighborPos = room.gridPos + DirectionUtility.ToGridVector(direction);

                if (!map.TryGetValue(neighborPos, out RoomNode neighbor))
                    continue;

                // Do not add extra connections to the boss room.
                if (neighbor.isBoss)
                    continue;

                Direction opposite = DirectionUtility.Opposite(direction);

                // If the neighbour already has the opposite connection, skip it.
                if (neighbor.HasConnection(opposite))
                    continue;

                // Randomly decide whether to add this loop connection.
                if (Random.value <= extraConnectionChance)
                    room.Connect(direction, neighbor);
            }
        }
    }

    // Finds the room that is the furthest walk-distance from the start room.
    //
    // This uses breadth-first search, so it measures path distance through connected rooms,
    // not just straight-line distance.
    private RoomNode GetFurthestRoomByPathDistance(RoomNode start)
    {
        Queue<RoomNode> queue = new Queue<RoomNode>();
        Dictionary<RoomNode, int> distances = new Dictionary<RoomNode, int>();

        queue.Enqueue(start);
        distances[start] = 0;

        RoomNode furthest = start;
        int furthestDistance = 0;

        while (queue.Count > 0)
        {
            RoomNode current = queue.Dequeue();
            int currentDistance = distances[current];

            // If this room is further away than the current furthest,
            // make it the new furthest room.
            if (currentDistance > furthestDistance)
            {
                furthestDistance = currentDistance;
                furthest = current;
            }

            // Visit every connected room.
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