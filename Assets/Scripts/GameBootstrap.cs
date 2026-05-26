using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private void Start()
    {
        DungeonGenerator generator = FindFirstObjectByType<DungeonGenerator>();
        DungeonSpawner spawner = FindFirstObjectByType<DungeonSpawner>();

        if (generator == null)
        {
            Debug.LogError("No DungeonGenerator found.");
            return;
        }

        if (spawner == null)
        {
            Debug.LogError("No DungeonSpawner found.");
            return;
        }

        var dungeon = generator.Generate();
        spawner.BuildDungeon(dungeon);

        Vector2Int startPos = Vector2Int.zero;
        Vector3 center = DungeonManager.Instance.GetRoomCenter(startPos);

        PlayerController.Instance.transform.position = center;

        DungeonManager.Instance.EnterRoom(startPos);

        GameManager.Instance.SetState(GameState.Exploring);
    }
}