using System.Collections.Generic;
using UnityEngine;

// Controls how the generator chooses which room becomes the boss room.
public enum BossRoomSelectionMode
{
    // Picks a dead-end room that is the furthest walk-distance from the start.
    FurthestLeafFromStart,

    // Picks a random valid dead-end room.
    RandomLeaf
}

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
    // The boss room is now selected from the generated rooms.
    // This means roomCount already includes the boss room.
    //
    // Example:
    // roomCount = 12
    // Result = 1 start room + 10 normal rooms + 1 boss room
    [Min(6)]
    public int roomCount = 12;

    // Chance to add extra connections between rooms that are already next to each other.
    //
    // 0 = no extra loops, more linear dungeon.
    // 1 = lots of loops, more connected dungeon.
    //
    // Important:
    // The boss room is protected from extra loops.
    // This keeps the boss room as a single-entrance room.
    [Range(0f, 1f)]
    public float extraConnectionChance = 0.15f;

    [Header("Start Room")]

    // If true, the start room always has connections in all four directions:
    // Up, Down, Right, and Left.
    //
    // This means your start room prefab should support all four doors.
    public bool forceStartRoomFourWay = true;

    [Header("Boss Room")]

    // Controls how the boss room is chosen.
    //
    // FurthestLeafFromStart:
    // Picks one of the furthest dead-end rooms from the start.
    //
    // RandomLeaf:
    // Picks a random dead-end room.
    public BossRoomSelectionMode bossRoomSelectionMode = BossRoomSelectionMode.FurthestLeafFromStart;

    // The boss room must be at least this many room steps away from the start if possible.
    //
    // If no dead-end room is far enough away,
    // the generator will fall back to any available dead-end room.
    [Min(1)]
    public int minimumBossDistanceFromStart = 2;

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

        // Make sure the room count is high enough to support the forced four-way start.
        int targetRoomCount = Mathf.Max(roomCount, forceStartRoomFourWay ? 5 : 2);

        // Rooms that can still be expanded from.
        //
        // The generator randomly picks from this list and tries to add new rooms nearby.
        List<RoomNode> expandableRooms = new List<RoomNode>(map.Values);

        int safety = 0;

        // Keep adding rooms until the dungeon reaches the target count,
        // or until there are no rooms left that can expand.
        while (map.Count < targetRoomCount && expandableRooms.Count > 0)
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

        // Make sure no old boss flags exist before selecting the new boss room.
        //
        // This helps guarantee that only one boss room exists.
        ClearBossFlags(map);

        // Pick exactly one boss room.
        //
        // The boss room is selected from existing dead-end rooms,
        // so it can appear Up, Down, Right, or Left depending on the dungeon shape.
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

        // Add optional loop connections after the boss has been chosen.
        //
        // The boss room is protected so it stays a single-entrance room.
        AddOptionalLoops(map, bossRoom);

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

    // Removes boss flags from every room.
    //
    // This guarantees the generator starts clean each time.
    private void ClearBossFlags(Dictionary<Vector2Int, RoomNode> map)
    {
        foreach (RoomNode room in map.Values)
        {
            if (room == null)
                continue;

            room.isBoss = false;
        }
    }

    // Chooses exactly one room to become the boss room.
    //
    // The boss room should usually be a dead-end room.
    //
    // A dead-end room means:
    // - it has exactly one connection
    // - it only needs one door
    //
    // This is what allows you to use multiple boss prefabs:
    // - Up-only boss room
    // - Down-only boss room
    // - Right-only boss room
    // - Left-only boss room
    private RoomNode ChooseBossRoom(RoomNode start, Dictionary<Vector2Int, RoomNode> map)
    {
        // Calculate path distances from the start room.
        Dictionary<RoomNode, int> distances = GetDistancesFromStart(start);

        // Collect valid dead-end rooms.
        List<RoomNode> validLeafRooms = new List<RoomNode>();

        foreach (RoomNode room in map.Values)
        {
            if (room == null)
                continue;

            // Never choose the start room as the boss room.
            if (room.isStart)
                continue;

            // Boss should be a dead-end room.
            //
            // This keeps the boss room to one entrance only.
            if (room.GetConnectionCount() != 1)
                continue;

            int distance = distances.TryGetValue(room, out int value) ? value : 0;

            // Prefer rooms far enough away from the start.
            if (distance < minimumBossDistanceFromStart)
                continue;

            validLeafRooms.Add(room);
        }

        // Fallback:
        // If no dead-end room was far enough away,
        // allow any dead-end room that is not the start room.
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

        // Last-resort fallback:
        // If no leaf rooms exist for some reason,
        // allow any non-start room.
        //
        // This should be rare, but it prevents generation from completely failing.
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

        // Random boss mode.
        if (bossRoomSelectionMode == BossRoomSelectionMode.RandomLeaf)
            return validLeafRooms[Random.Range(0, validLeafRooms.Count)];

        // Default boss mode:
        // choose the furthest valid dead-end room from the start.
        return GetFurthestLeaf(validLeafRooms, distances);
    }

    // Finds the furthest room from a list of possible boss rooms.
    //
    // If multiple rooms tie for furthest distance,
    // one of them is chosen randomly.
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

    // Calculates the walk-distance from the start room to every connected room.
    //
    // This uses breadth-first search.
    //
    // It measures actual room path distance,
    // not straight-line distance.
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

    // Adds optional extra connections between rooms that are already neighbours.
    //
    // This creates loops in the dungeon so the layout is less linear.
    //
    // Important:
    // The boss room is skipped.
    // Rooms are also prevented from making new loop connections into the boss room.
    //
    // This keeps the boss room as a single-entrance room.
    private void AddOptionalLoops(Dictionary<Vector2Int, RoomNode> map, RoomNode bossRoom)
    {
        foreach (RoomNode room in map.Values)
        {
            if (room == null)
                continue;

            // Do not add extra connections from the boss room.
            if (room == bossRoom)
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

                // Do not add extra connections into the boss room.
                if (neighbor == bossRoom)
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
}