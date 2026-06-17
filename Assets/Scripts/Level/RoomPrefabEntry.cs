using UnityEngine;

// Controls how strictly a room prefab must match required doors.
public enum RoomPrefabMatchMode
{
    ExactOpenings,
    SupportsRequiredOpenings
}

// Stores what door openings a room prefab supports.
public class RoomPrefab : MonoBehaviour
{
    [Header("Prefab Openings / Slots")]

    // Door slots this prefab supports.
    public bool up;
    public bool down;
    public bool right;
    public bool left;

    [Header("Matching")]

    // Decides how this prefab is matched to generated rooms.
    public RoomPrefabMatchMode matchMode = RoomPrefabMatchMode.ExactOpenings;

    // Converts door bools into a mask number.
    public int GetMask()
    {
        int mask = 0;

        if (up) mask |= DirectionUtility.ToMask(Direction.Up);
        if (down) mask |= DirectionUtility.ToMask(Direction.Down);
        if (right) mask |= DirectionUtility.ToMask(Direction.Right);
        if (left) mask |= DirectionUtility.ToMask(Direction.Left);

        return mask;
    }

    // Checks if this prefab can be used for a room.
    public bool MatchesRequiredMask(int requiredMask)
    {
        int prefabMask = GetMask();

        if (matchMode == RoomPrefabMatchMode.ExactOpenings)
            return prefabMask == requiredMask;

        return (prefabMask & requiredMask) == requiredMask;
    }
}