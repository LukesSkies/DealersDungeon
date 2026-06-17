using System.Collections.Generic;
using UnityEngine;

// Spawns the generated dungeon into the scene.
public class DungeonSpawner : MonoBehaviour
{
    [Header("Room Prefabs")]

    // Normal room prefabs.
    public List<GameObject> roomPrefabs = new();

    // Start room prefab.
    public GameObject startRoomPrefab;

    [Header("Boss Room Prefabs")]

    // Boss room prefabs.
    public List<GameObject> bossRoomPrefabs = new();

    [Header("Parent")]

    // Parent for spawned rooms.
    public Transform dungeonParent;

    [Header("Room Offset")]

    // Room spacing on X and Z.
    [Tooltip("X = spacing along world X for Right/Left. Y = spacing along world Z for Up/Down.")]
    public Vector2 roomOffset = new Vector2(20f, 20f);

    [Header("Door Cleanup")]

    // Removes duplicate door visuals between connected rooms.
    public bool disableStackedDoorVisuals = true;

    [Header("Validation")]

    // Checks if connected door points line up.
    public bool validateDoorAlignment = true;

    // Allowed doorway alignment difference.
    public float doorAlignmentTolerance = 0.1f;

    // Spawned rooms by grid position.
    private readonly Dictionary<Vector2Int, Room> spawnedRooms = new();

    // Spawned room objects.
    private readonly List<GameObject> spawnedObjects = new();

    // Lets other scripts read spawned rooms.
    public Dictionary<Vector2Int, Room> SpawnedRooms => spawnedRooms;

    // Builds the dungeon from generated room nodes.
    public void BuildDungeon(List<RoomNode> nodes)
    {
        ClearExistingDungeon();

        spawnedRooms.Clear();

        foreach (RoomNode node in nodes)
        {
            GameObject prefab = GetPrefabForNode(node);

            if (prefab == null)
            {
                Debug.LogError($"No prefab found for room at {node.gridPos}.");
                continue;
            }

            Vector3 worldPos = GridToWorldPosition(node.gridPos);

            Transform parent = dungeonParent != null ? dungeonParent : transform;

            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, parent);

            spawnedObjects.Add(instance);

            Room room = instance.GetComponent<Room>();

            if (room == null)
            {
                Debug.LogError($"{prefab.name} is missing a Room component.");
                continue;
            }

            room.Setup(node, disableStackedDoorVisuals);

            spawnedRooms[node.gridPos] = room;
        }

        if (validateDoorAlignment)
            ValidateDoorAlignment(nodes);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.RegisterRooms(spawnedRooms, nodes);
        else
            Debug.LogError("No DungeonManager found in scene.");
    }

    // Deletes the current spawned dungeon.
    public void ClearExistingDungeon()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null)
                continue;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        spawnedObjects.Clear();
        spawnedRooms.Clear();
    }

    // Converts grid position to world position.
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * roomOffset.x,
            0f,
            gridPos.y * roomOffset.y
        );
    }

    // Picks the prefab for a room node.
    private GameObject GetPrefabForNode(RoomNode node)
    {
        int requiredMask = node.GetDoorMask();

        if (node.isStart)
        {
            ValidateSpecialPrefab(startRoomPrefab, requiredMask, "Start Room");
            return startRoomPrefab;
        }

        if (node.isBoss)
        {
            return GetBossPrefabForMask(requiredMask);
        }

        return GetNormalRoomPrefabForMask(requiredMask);
    }

    // Finds a normal room prefab.
    private GameObject GetNormalRoomPrefabForMask(int requiredMask)
    {
        List<GameObject> exactMatches = new List<GameObject>();
        List<GameObject> compatibleMatches = new List<GameObject>();

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

            if (prefabMask == requiredMask)
            {
                exactMatches.Add(prefab);
            }
            else if (data.MatchesRequiredMask(requiredMask))
            {
                compatibleMatches.Add(prefab);
            }
        }

        if (exactMatches.Count > 0)
            return exactMatches[Random.Range(0, exactMatches.Count)];

        if (compatibleMatches.Count > 0)
            return compatibleMatches[Random.Range(0, compatibleMatches.Count)];

        Debug.LogError($"No valid normal room prefab found for door mask {requiredMask}.");

        return roomPrefabs.Count > 0 ? roomPrefabs[0] : null;
    }

    // Finds a boss room prefab.
    private GameObject GetBossPrefabForMask(int requiredMask)
    {
        List<GameObject> exactMatches = new List<GameObject>();
        List<GameObject> compatibleMatches = new List<GameObject>();

        foreach (GameObject prefab in bossRoomPrefabs)
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

            if (prefabMask == requiredMask)
            {
                exactMatches.Add(prefab);
            }
            else if (data.MatchesRequiredMask(requiredMask))
            {
                compatibleMatches.Add(prefab);
            }
        }

        if (exactMatches.Count > 0)
            return exactMatches[Random.Range(0, exactMatches.Count)];

        if (compatibleMatches.Count > 0)
            return compatibleMatches[Random.Range(0, compatibleMatches.Count)];

        Debug.LogError(
            $"No valid boss room prefab found for door mask {requiredMask}. " +
            $"Add a boss prefab with this exact opening direction."
        );

        return bossRoomPrefabs.Count > 0 ? bossRoomPrefabs[0] : null;
    }

    // Checks if a special prefab supports the required doors.
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

        if (!data.MatchesRequiredMask(requiredMask))
        {
            Debug.LogError(
                $"{label} prefab does not match required exits. " +
                $"Required mask: {requiredMask}, Prefab mask: {data.GetMask()}."
            );
        }
    }

    // Warns if connected door entry points do not align.
    private void ValidateDoorAlignment(List<RoomNode> nodes)
    {
        foreach (RoomNode node in nodes)
        {
            if (!spawnedRooms.TryGetValue(node.gridPos, out Room roomA))
                continue;

            foreach (Direction direction in DirectionUtility.AllDirections)
            {
                if (!node.HasConnection(direction))
                    continue;

                if (!node.OwnsDoorVisual(direction))
                    continue;

                RoomNode connectedNode = node.GetConnection(direction);

                if (connectedNode == null)
                    continue;

                if (!spawnedRooms.TryGetValue(connectedNode.gridPos, out Room roomB))
                    continue;

                Direction opposite = DirectionUtility.Opposite(direction);

                bool hasPointA = roomA.TryGetDoorEntryWorld(direction, out Vector3 entryA);
                bool hasPointB = roomB.TryGetDoorEntryWorld(opposite, out Vector3 entryB);

                if (!hasPointA || !hasPointB)
                    continue;

                Vector2 a = new Vector2(entryA.x, entryA.z);
                Vector2 b = new Vector2(entryB.x, entryB.z);

                float distance = Vector2.Distance(a, b);

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

    // Calculates room spacing from the start room prefab.
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

        if (room.TryGetDoorEntryLocal(Direction.Right, out Vector3 right) &&
            room.TryGetDoorEntryLocal(Direction.Left, out Vector3 left))
        {
            roomOffset.x = Mathf.Abs(right.x - left.x);
            calculatedX = true;
        }

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