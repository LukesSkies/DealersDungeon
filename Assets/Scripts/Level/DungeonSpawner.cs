using System.Collections.Generic;
using UnityEngine;

public class DungeonSpawner : MonoBehaviour
{
    [Header("Room Prefabs")]
    public List<GameObject> roomPrefabs;

    public GameObject startRoomPrefab;
    public GameObject bossRoomPrefab;

    [Header("Settings")]
    public float roomSize = 20f;

    private Dictionary<Vector2Int, Room> spawnedRooms = new();

    public void BuildDungeon(List<RoomNode> nodes)
    {
        spawnedRooms.Clear();

        foreach (RoomNode node in nodes)
        {
            GameObject prefab = GetPrefabForNode(node);

            Vector3 worldPos = new Vector3(
                node.gridPos.x * roomSize,
                0f,
                node.gridPos.y * roomSize
            );

            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);

            Room room = instance.GetComponent<Room>();

            if (room == null)
            {
                Debug.LogError($"{prefab.name} is missing Room component.");
                continue;
            }

            room.Setup(node);
            spawnedRooms[node.gridPos] = room;
        }

        DungeonManager.Instance.RegisterRooms(spawnedRooms, nodes);
    }

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
            ValidateSpecialPrefab(bossRoomPrefab, requiredMask, "Boss Room");
            return bossRoomPrefab;
        }

        List<GameObject> validPrefabs = new();

        foreach (GameObject prefab in roomPrefabs)
        {
            RoomPrefab data = prefab.GetComponent<RoomPrefab>();

            if (data == null)
            {
                Debug.LogWarning($"{prefab.name} is missing RoomPrefab component.");
                continue;
            }

            if (data.GetMask() == requiredMask)
            {
                validPrefabs.Add(prefab);
            }
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogError($"No valid prefab found for door mask {requiredMask}. Make a prefab with that exact exit layout.");
            return roomPrefabs[0];
        }

        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

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
            Debug.LogWarning($"{label} prefab has no RoomPrefab component. Cannot validate exits.");
            return;
        }

        int prefabMask = data.GetMask();

        if (prefabMask != requiredMask)
        {
            Debug.LogError($"{label} prefab has wrong exits. Required mask: {requiredMask}, Prefab mask: {prefabMask}");
        }
    }
}