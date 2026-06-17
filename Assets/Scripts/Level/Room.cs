using UnityEngine;

// Controls one spawned room.
public class Room : MonoBehaviour
{
    [Header("Boss Indicator")]

    // Visual shown if this is the boss room.
    public GameObject bossDoorIndicator;

    // True if this room is the boss room.
    public bool isBossRoom;

    [Header("Enemy Spawn Points")]

    // Places enemies can spawn.
    public Transform[] enemySpawnPoints;

    [Header("Door Visuals")]

    public Door upDoor;
    public Door downDoor;
    public Door rightDoor;
    public Door leftDoor;

    [Header("Wall Visuals")]

    public GameObject upWall;
    public GameObject downWall;
    public GameObject rightWall;
    public GameObject leftWall;

    [Header("Door Entry Points")]

    public Transform upEntryPoint;
    public Transform downEntryPoint;
    public Transform rightEntryPoint;
    public Transform leftEntryPoint;

    [Header("Room Center")]

    // Center point of the room.
    public Transform centerPoint;

    // Gets the room center.
    public Vector3 GetCenter()
    {
        return centerPoint != null ? centerPoint.position : transform.position;
    }

    // Gets an enemy spawn position.
    public Vector3 GetEnemySpawnPosition(int index)
    {
        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
            return enemySpawnPoints[index % enemySpawnPoints.Length].position;

        return GetCenter() + Vector3.forward * 5f;
    }

    // Sets up doors, walls, and boss visuals.
    public void Setup(RoomNode node, bool disableStackedDoorVisuals)
    {
        isBossRoom = node.isBoss;

        if (bossDoorIndicator != null)
            bossDoorIndicator.SetActive(node.isBoss);

        ConfigureSide(Direction.Up, upDoor, upWall, node, disableStackedDoorVisuals);
        ConfigureSide(Direction.Down, downDoor, downWall, node, disableStackedDoorVisuals);
        ConfigureSide(Direction.Right, rightDoor, rightWall, node, disableStackedDoorVisuals);
        ConfigureSide(Direction.Left, leftDoor, leftWall, node, disableStackedDoorVisuals);
    }

    // Shows or hides one door/wall side.
    private void ConfigureSide(
        Direction direction,
        Door door,
        GameObject wall,
        RoomNode node,
        bool disableStackedDoorVisuals)
    {
        bool connected = node.HasConnection(direction);
        bool showDoor = connected;

        if (connected && disableStackedDoorVisuals)
            showDoor = node.OwnsDoorVisual(direction);

        if (door != null)
        {
            door.SetDirection(direction);
            door.gameObject.SetActive(showDoor);
        }

        if (wall != null)
            wall.SetActive(!connected);
    }

    // Gets a door by direction.
    public Door GetDoor(Direction direction)
    {
        return direction switch
        {
            Direction.Up => upDoor,
            Direction.Down => downDoor,
            Direction.Right => rightDoor,
            Direction.Left => leftDoor,
            _ => null
        };
    }

    // Gets a wall by direction.
    public GameObject GetWall(Direction direction)
    {
        return direction switch
        {
            Direction.Up => upWall,
            Direction.Down => downWall,
            Direction.Right => rightWall,
            Direction.Left => leftWall,
            _ => null
        };
    }

    // Gets a doorway entry point by direction.
    public Transform GetDoorEntryPoint(Direction direction)
    {
        return direction switch
        {
            Direction.Up => upEntryPoint,
            Direction.Down => downEntryPoint,
            Direction.Right => rightEntryPoint,
            Direction.Left => leftEntryPoint,
            _ => null
        };
    }

    // Gets a doorway entry point in world space.
    public bool TryGetDoorEntryWorld(Direction direction, out Vector3 position)
    {
        Transform entryPoint = GetDoorEntryPoint(direction);

        if (entryPoint != null)
        {
            position = entryPoint.position;
            return true;
        }

        Door door = GetDoor(direction);

        if (door != null)
        {
            position = door.transform.position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    // Gets a doorway entry point in local space.
    public bool TryGetDoorEntryLocal(Direction direction, out Vector3 position)
    {
        Transform entryPoint = GetDoorEntryPoint(direction);

        if (entryPoint != null)
        {
            position = transform.InverseTransformPoint(entryPoint.position);
            return true;
        }

        Door door = GetDoor(direction);

        if (door != null)
        {
            position = transform.InverseTransformPoint(door.transform.position);
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    // Opens one door.
    public void OpenDoor(Direction direction)
    {
        Door door = GetDoor(direction);

        if (door != null && door.gameObject.activeInHierarchy)
            door.Open();
    }

    // Closes one door.
    public void CloseDoor(Direction direction)
    {
        Door door = GetDoor(direction);

        if (door != null && door.gameObject.activeInHierarchy)
            door.Close();
    }

    // Opens all active doors.
    public void OpenAllDoors()
    {
        foreach (Direction direction in DirectionUtility.AllDirections)
        {
            OpenDoor(direction);
        }
    }

    // Closes all active doors.
    public void CloseAllDoors()
    {
        foreach (Direction direction in DirectionUtility.AllDirections)
        {
            CloseDoor(direction);
        }
    }
}