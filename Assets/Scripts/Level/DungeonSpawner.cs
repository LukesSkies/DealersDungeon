using System.Collections.Generic;
using UnityEngine;

// This script is responsible for turning generated RoomNode data
// into real room GameObjects in the Unity scene.
//
// DungeonGenerator creates the data layout.
// DungeonSpawner creates the visible dungeon from that layout.
public class DungeonSpawner : MonoBehaviour
{
    [Header("Room Prefabs")]

    // Normal room prefabs that can be used for non-start, non-boss rooms.
    //
    // Each prefab in this list should have:
    // - Room component
    // - RoomPrefab component
    //
    // If you are using modular rooms, these can support all four directions.
    public List<GameObject> roomPrefabs = new();

    // The room prefab used only for the start room.
    //
    // Because your start room is forced to connect in all four directions,
    // this should support Up, Down, Right, and Left.
    public GameObject startRoomPrefab;

    // The room prefab used only for the boss room.
    //
    // In your current setup, the boss room is placed above another room,
    // so it should only need a Down door.
    public GameObject bossRoomPrefab;

    [Header("Parent")]

    // Optional parent object for all spawned dungeon rooms.
    //
    // Keeps the hierarchy clean.
    // If this is empty, rooms are parented under this DungeonSpawner object.
    public Transform dungeonParent;

    [Header("Room Offset")]

    // Controls the spacing between rooms.
    //
    // roomOffset.x = distance between rooms on world X.
    // This is used for Right and Left connections.
    //
    // roomOffset.y = distance between rooms on world Z.
    // This is used for Up and Down connections.
    [Tooltip("X = spacing along world X for Right/Left. Y = spacing along world Z for Up/Down.")]
    public Vector2 roomOffset = new Vector2(20f, 20f);

    [Header("Door Cleanup")]

    // If true, only one visible door is kept between two connected rooms.
    //
    // Example:
    // Room A Right door connects to Room B Left door.
    // Without cleanup, both doors can sit on top of each other.
    //
    // This keeps one visible door and disables the duplicate one.
    public bool disableStackedDoorVisuals = true;

    [Header("Validation")]

    // If true, the spawner checks if connected doorway entry points line up.
    //
    // This is useful while building/testing room prefabs.
    public bool validateDoorAlignment = true;

    // Maximum allowed distance between two connected door entry points.
    //
    // If the distance is bigger than this, a warning is logged.
    public float doorAlignmentTolerance = 0.1f;

    // Stores the rooms that have been spawned.
    //
    // Key = grid position.
    // Value = actual spawned Room component.
    private readonly Dictionary<Vector2Int, Room> spawnedRooms = new();

    // Stores the spawned room GameObjects so they can be destroyed
    // when a new floor is generated.
    private readonly List<GameObject> spawnedObjects = new();

    // Allows other scripts to read the spawned room dictionary.
    public Dictionary<Vector2Int, Room> SpawnedRooms => spawnedRooms;

    // Builds the dungeon from a list of generated RoomNodes.
    //
    // This is normally called by DungeonRunManager.
    public void BuildDungeon(List<RoomNode> nodes)
    {
        // Remove any old dungeon from the scene before spawning a new one.
        ClearExistingDungeon();

        spawnedRooms.Clear();

        // Spawn one prefab for every generated room node.
        foreach (RoomNode node in nodes)
        {
            // Pick the correct prefab for this room based on its required door layout.
            GameObject prefab = GetPrefabForNode(node);

            if (prefab == null)
            {
                Debug.LogError($"No prefab found for room at {node.gridPos}.");
                continue;
            }

            // Convert the room's grid position into a world position.
            Vector3 worldPos = GridToWorldPosition(node.gridPos);

            // Use dungeonParent if assigned.
            // Otherwise parent rooms to this spawner.
            Transform parent = dungeonParent != null ? dungeonParent : transform;

            // Create the room in the scene.
            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, parent);

            // Store it so it can be destroyed later.
            spawnedObjects.Add(instance);

            // Get the Room component on the spawned prefab.
            Room room = instance.GetComponent<Room>();

            if (room == null)
            {
                Debug.LogError($"{prefab.name} is missing a Room component.");
                continue;
            }

            // Tell the room which doors/walls to show based on the RoomNode.
            room.Setup(node, disableStackedDoorVisuals);

            // Store this room by its grid position.
            spawnedRooms[node.gridPos] = room;
        }

        // Optional check to warn if connected doors are physically misaligned.
        if (validateDoorAlignment)
            ValidateDoorAlignment(nodes);

        // Register the spawned dungeon with DungeonManager,
        // so the player can move between rooms and combat can trigger.
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.RegisterRooms(spawnedRooms, nodes);
        else
            Debug.LogError("No DungeonManager found in scene.");
    }

    // Deletes all rooms spawned by this spawner.
    //
    // This is used when generating a new floor.
    public void ClearExistingDungeon()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null)
                continue;

            // Use Destroy during play mode.
            if (Application.isPlaying)
                Destroy(obj);
            // Use DestroyImmediate in edit mode.
            else
                DestroyImmediate(obj);
        }

        spawnedObjects.Clear();
        spawnedRooms.Clear();
    }

    // Converts a grid position into a world position.
    //
    // gridPos.x controls world X.
    // gridPos.y controls world Z.
    //
    // Example:
    // gridPos = (1, 0)
    // means one room to the Right.
    //
    // gridPos = (0, 1)
    // means one room Up.
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * roomOffset.x,
            0f,
            gridPos.y * roomOffset.y
        );
    }

    // Picks the prefab that should be spawned for a generated RoomNode.
    //
    // The RoomNode says what doors are required.
    // The RoomPrefab component on each prefab says what doors that prefab supports.
    private GameObject GetPrefabForNode(RoomNode node)
    {
        // Convert the node's connections into a door mask.
        //
        // Example:
        // Up + Right = 1 + 4 = 5
        int requiredMask = node.GetDoorMask();

        // Start room uses a special prefab.
        if (node.isStart)
        {
            ValidateSpecialPrefab(startRoomPrefab, requiredMask, "Start Room");
            return startRoomPrefab;
        }

        // Boss room uses a special prefab.
        if (node.isBoss)
        {
            ValidateSpecialPrefab(bossRoomPrefab, requiredMask, "Boss Room");
            return bossRoomPrefab;
        }

        // Exact matches have exactly the same openings as required.
        List<GameObject> exactMatches = new();

        // Compatible matches support all required openings,
        // but may also have extra openings that Room.Setup can hide.
        List<GameObject> compatibleMatches = new();

        foreach (GameObject prefab in roomPrefabs)
        {
            if (prefab == null)
                continue;

            RoomPrefab data = prefab.GetComponent<RoomPrefab>();

            if (data == null)
            {
                Debug.LogWarning($"{prefab.name} is missing RoomPrefab component.");
                continue;
            }

            int prefabMask = data.GetMask();

            // Prefer exact matches.
            if (prefabMask == requiredMask)
            {
                exactMatches.Add(prefab);
            }
            // Otherwise allow compatible modular prefabs.
            else if (data.MatchesRequiredMask(requiredMask))
            {
                compatibleMatches.Add(prefab);
            }
        }

        // Use a random exact match if possible.
        if (exactMatches.Count > 0)
            return exactMatches[Random.Range(0, exactMatches.Count)];

        // If no exact match exists, use a compatible modular room.
        if (compatibleMatches.Count > 0)
            return compatibleMatches[Random.Range(0, compatibleMatches.Count)];

        // If nothing matches, log an error.
        //
        // This usually means:
        // - roomPrefabs is missing a required prefab
        // - or the RoomPrefab bools are not set correctly
        // - or modular rooms are not set to SupportsRequiredOpenings
        Debug.LogError($"No valid room prefab found for door mask {requiredMask}. Required layout must match generated connections.");

        // Fallback so the generation does not completely crash.
        return roomPrefabs.Count > 0 ? roomPrefabs[0] : null;
    }

    // Checks whether a special prefab, like Start Room or Boss Room,
    // supports the required door layout.
    private void ValidateSpecialPrefab(GameObject prefab, int requiredMask, string label)
    {
        if (prefab == null)
        {
            Debug.LogError($"{label} prefab is missing.");
            return;
        }

        RoomPrefab data = prefab.GetComponent<RoomPrefab>();

        if (data == null)
        {
            Debug.LogWarning($"{label} prefab has no RoomPrefab component. Cannot validate openings.");
            return;
        }

        // If the prefab does not match the generated room's required doors,
        // log an error so you know the prefab setup is wrong.
        if (!data.MatchesRequiredMask(requiredMask))
        {
            Debug.LogError(
                $"{label} prefab does not match required exits. " +
                $"Required mask: {requiredMask}, Prefab mask: {data.GetMask()}."
            );
        }
    }

    // Checks whether connected door entry points line up in world space.
    //
    // This does not stop the dungeon from spawning.
    // It only logs warnings to help you fix roomOffset or prefab entry points.
    private void ValidateDoorAlignment(List<RoomNode> nodes)
    {
        foreach (RoomNode node in nodes)
        {
            if (!spawnedRooms.TryGetValue(node.gridPos, out Room roomA))
                continue;

            foreach (Direction direction in DirectionUtility.AllDirections)
            {
                // Only check actual connections.
                if (!node.HasConnection(direction))
                    continue;

                // Only check one side of the connection.
                //
                // Otherwise the same connection would be checked twice.
                if (!node.OwnsDoorVisual(direction))
                    continue;

                RoomNode connectedNode = node.GetConnection(direction);

                if (connectedNode == null)
                    continue;

                if (!spawnedRooms.TryGetValue(connectedNode.gridPos, out Room roomB))
                    continue;

                Direction opposite = DirectionUtility.Opposite(direction);

                // Get the doorway entry position from both rooms.
                bool hasPointA = roomA.TryGetDoorEntryWorld(direction, out Vector3 entryA);
                bool hasPointB = roomB.TryGetDoorEntryWorld(opposite, out Vector3 entryB);

                if (!hasPointA || !hasPointB)
                    continue;

                // Compare only X/Z because room connections are horizontal.
                Vector2 a = new Vector2(entryA.x, entryA.z);
                Vector2 b = new Vector2(entryB.x, entryB.z);

                float distance = Vector2.Distance(a, b);

                // Warn if the two connected doorway points are not close enough.
                if (distance > doorAlignmentTolerance)
                {
                    Debug.LogWarning(
                        $"Door alignment problem between room {node.gridPos} {DirectionUtility.ToDisplayName(direction)} " +
                        $"and room {connectedNode.gridPos} {DirectionUtility.ToDisplayName(opposite)}. " +
                        $"Distance: {distance}. Check roomOffset or door entry point transforms."
                    );
                }
            }
        }
    }

    // Right-click this component in the Inspector and select:
    // "Calculate Room Offset From Start Room Prefab"
    //
    // This calculates roomOffset using the start room's entry points.
    [ContextMenu("Calculate Room Offset From Start Room Prefab")]
    public void CalculateRoomOffsetFromStartRoomPrefab()
    {
        if (startRoomPrefab == null)
        {
            Debug.LogError("Cannot calculate offset because startRoomPrefab is missing.");
            return;
        }

        Room room = startRoomPrefab.GetComponent<Room>();

        if (room == null)
        {
            Debug.LogError("Cannot calculate offset because startRoomPrefab has no Room component.");
            return;
        }

        bool calculatedX = false;
        bool calculatedZ = false;

        // Calculate X spacing from Right and Left entry points.
        if (room.TryGetDoorEntryLocal(Direction.Right, out Vector3 right) &&
            room.TryGetDoorEntryLocal(Direction.Left, out Vector3 left))
        {
            roomOffset.x = Mathf.Abs(right.x - left.x);
            calculatedX = true;
        }

        // Calculate Z spacing from Up and Down entry points.
        if (room.TryGetDoorEntryLocal(Direction.Up, out Vector3 up) &&
            room.TryGetDoorEntryLocal(Direction.Down, out Vector3 down))
        {
            roomOffset.y = Mathf.Abs(up.z - down.z);
            calculatedZ = true;
        }

        if (calculatedX || calculatedZ)
        {
            Debug.Log($"Room offset calculated: X={roomOffset.x}, Z={roomOffset.y}");
        }
        else
        {
            Debug.LogWarning("Could not calculate room offset. Assign door entry point transforms on the Room component.");
        }
    }
}