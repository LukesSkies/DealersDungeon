using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Boss Indicator")]
    public GameObject bossDoorIndicator;

    public bool isBossRoom;

    [Header("Enemy Spawn Points")]
    public Transform[] enemySpawnPoints;

    [Header("Doors")]
    public Door northDoor;
    public Door southDoor;
    public Door eastDoor;
    public Door westDoor;

    public Transform centerPoint;

    public Vector3 GetCenter()
    {
        return centerPoint != null ? centerPoint.position : transform.position;
    }

    public Vector3 GetEnemySpawnPosition(int index)
    {
        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
        {
            return enemySpawnPoints[index % enemySpawnPoints.Length].position;
        }

        return transform.position + Vector3.forward * 5f;
    }

    public void Setup(RoomNode node)
    {
        isBossRoom = node.isBoss;

        if (bossDoorIndicator != null)
            bossDoorIndicator.SetActive(node.isBoss);

        SetDoorActive(northDoor, node.HasConnection(Direction.North));
        SetDoorActive(southDoor, node.HasConnection(Direction.South));
        SetDoorActive(eastDoor, node.HasConnection(Direction.East));
        SetDoorActive(westDoor, node.HasConnection(Direction.West));
    }

    private void SetDoorActive(Door door, bool active)
    {
        if (door != null)
            door.gameObject.SetActive(active);
    }

    public Door GetDoor(Direction direction)
    {
        return direction switch
        {
            Direction.North => northDoor,
            Direction.South => southDoor,
            Direction.East => eastDoor,
            Direction.West => westDoor,
            _ => null
        };
    }

    public void OpenAllDoors()
    {
        northDoor?.Open();
        southDoor?.Open();
        eastDoor?.Open();
        westDoor?.Open();
    }

    public void CloseAllDoors()
    {
        northDoor?.Close();
        southDoor?.Close();
        eastDoor?.Close();
        westDoor?.Close();
    }
}