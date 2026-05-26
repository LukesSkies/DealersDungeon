using UnityEngine;

public class RoomPrefab : MonoBehaviour
{
    [Header("Prefab Exits")]
    public bool north;
    public bool south;
    public bool east;
    public bool west;

    public int GetMask()
    {
        int mask = 0;

        if (north) mask |= DirectionUtility.ToMask(Direction.North);
        if (south) mask |= DirectionUtility.ToMask(Direction.South);
        if (east) mask |= DirectionUtility.ToMask(Direction.East);
        if (west) mask |= DirectionUtility.ToMask(Direction.West);

        return mask;
    }
}