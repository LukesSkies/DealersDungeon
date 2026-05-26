using UnityEngine;

// This script goes on every room prefab.
//
// It controls:
// - whether the room is a boss room
// - which doors are visible
// - which walls are visible
// - where the center of the room is
// - where the doorway entry points are
// - where enemies can spawn
public class Room : MonoBehaviour
{
    [Header("Boss Indicator")]

    // Visual object used to show that this room is the boss room.
    public GameObject bossDoorIndicator;

    // True if this spawned room is the boss room.
    // DungeonManager checks this when deciding if the player cleared the floor.
    public bool isBossRoom;

    [Header("Enemy Spawn Points")]

    // Optional enemy spawn points inside this room.
    // If this is empty, the room uses a simple fallback spawn position.
    public Transform[] enemySpawnPoints;

    [Header("Door Visuals")]

    // The visible door object on the +Z side of the room.
    public Door upDoor;

    // The visible door object on the -Z side of the room.
    public Door downDoor;

    // The visible door object on the +X side of the room.
    public Door rightDoor;

    // The visible door object on the -X side of the room.
    public Door leftDoor;

    [Header("Wall Visuals")]

    // Wall object that blocks the +Z side when there is no Up connection.
    public GameObject upWall;

    // Wall object that blocks the -Z side when there is no Down connection.
    public GameObject downWall;

    // Wall object that blocks the +X side when there is no Right connection.
    public GameObject rightWall;

    // Wall object that blocks the -X side when there is no Left connection.
    public GameObject leftWall;

    [Header("Door Entry Points")]

    // Empty Transform placed at the center of the Up doorway.
    // Used for offset calculation and door alignment checking.
    public Transform upEntryPoint;

    // Empty Transform placed at the center of the Down doorway.
    // Used for offset calculation and door alignment checking.
    public Transform downEntryPoint;

    // Empty Transform placed at the center of the Right doorway.
    // Used for offset calculation and door alignment checking.
    public Transform rightEntryPoint;

    // Empty Transform placed at the center of the Left doorway.
    // Used for offset calculation and door alignment checking.
    public Transform leftEntryPoint;

    [Header("Room Center")]

    // Empty Transform placed at the exact center of the room.
    // The player moves from one room center to the next room center.
    public Transform centerPoint;

    // Returns the world position of the room center.
    //
    // If centerPoint is assigned, it uses that.
    // If not, it falls back to the room object's transform position.
    public Vector3 GetCenter()
    {
        return centerPoint != null ? centerPoint.position : transform.position;
    }

    // Returns an enemy spawn position.
    //
    // If enemySpawnPoints are assigned, it cycles through them.
    // If none are assigned, it uses a fallback position in front of the room center.
    public Vector3 GetEnemySpawnPosition(int index)
    {
        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
            return enemySpawnPoints[index % enemySpawnPoints.Length].position;

        return GetCenter() + Vector3.forward * 5f;
    }

    // Sets up this room after it has been spawned.
    //
    // The RoomNode tells this room:
    // - is it the boss room?
    // - which directions connect to other rooms?
    // - which sides should be doors?
    // - which sides should be walls?
    public void Setup(RoomNode node, bool disableStackedDoorVisuals)
    {
        // Store whether this room is the boss room.
        isBossRoom = node.isBoss;

        // Show or hide the boss indicator.
        if (bossDoorIndicator != null)
            bossDoorIndicator.SetActive(node.isBoss);

        // Configure each side of the room.
        ConfigureSide(Direction.Up, upDoor, upWall, node, disableStackedDoorVisuals);
        ConfigureSide(Direction.Down, downDoor, downWall, node, disableStackedDoorVisuals);
        ConfigureSide(Direction.Right, rightDoor, rightWall, node, disableStackedDoorVisuals);
        ConfigureSide(Direction.Left, leftDoor, leftWall, node, disableStackedDoorVisuals);
    }

    // Sets one side of the room to either show a door or show a wall.
    //
    // If the room has a connection in this direction:
    // - the wall is hidden
    // - the door can be shown
    //
    // If the room does not have a connection:
    // - the wall is shown
    // - the door is hidden
    private void ConfigureSide(
        Direction direction,
        Door door,
        GameObject wall,
        RoomNode node,
        bool disableStackedDoorVisuals)
    {
        // True if this room is connected to another room in this direction.
        bool connected = node.HasConnection(direction);

        // By default, show a door if this side is connected.
        bool showDoor = connected;

        // If two connected rooms both have door objects facing each other,
        // they can overlap/stack in the same doorway.
        //
        // This lets only one of the two rooms show the visible door object.
        if (connected && disableStackedDoorVisuals)
            showDoor = node.OwnsDoorVisual(direction);

        // Configure the door object.
        if (door != null)
        {
            // Make sure this door knows which direction it belongs to.
            door.SetDirection(direction);

            // Show the door only if this side is connected and this room owns the visible door.
            door.gameObject.SetActive(showDoor);
        }

        // Configure the wall object.
        //
        // Wall is active when there is no connection.
        // Wall is inactive when there is a connection.
        if (wall != null)
            wall.SetActive(!connected);
    }

    // Gets the Door component for a direction.
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

    // Gets the wall object for a direction.
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

    // Gets the door entry point for a direction.
    //
    // These are empty Transforms placed in the middle of each doorway.
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

    // Tries to get the world-space position of a doorway entry point.
    //
    // First it uses the assigned entry point.
    // If no entry point exists, it falls back to the door object's position.
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

    // Tries to get the local-space position of a doorway entry point.
    //
    // This is used when calculating the room offset from a prefab.
    //
    // Local space means the position relative to the room prefab root.
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

    // Opens one door on this room.
    public void OpenDoor(Direction direction)
    {
        Door door = GetDoor(direction);

        // Only open the door if it exists and is active.
        //
        // Some connected doors may be disabled because stacked door visuals are removed.
        if (door != null && door.gameObject.activeInHierarchy)
            door.Open();
    }

    // Closes one door on this room.
    public void CloseDoor(Direction direction)
    {
        Door door = GetDoor(direction);

        // Only close the door if it exists and is active.
        if (door != null && door.gameObject.activeInHierarchy)
            door.Close();
    }

    // Opens every active door in this room.
    public void OpenAllDoors()
    {
        foreach (Direction direction in DirectionUtility.AllDirections)
        {
            OpenDoor(direction);
        }
    }

    // Closes every active door in this room.
    public void CloseAllDoors()
    {
        foreach (Direction direction in DirectionUtility.AllDirections)
        {
            CloseDoor(direction);
        }
    }
}