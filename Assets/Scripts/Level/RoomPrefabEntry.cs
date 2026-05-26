using UnityEngine;

// This controls how the DungeonSpawner checks if a room prefab
// can be used for a generated RoomNode.
public enum RoomPrefabMatchMode
{
    // The prefab must match the generated room's doors exactly.
    ExactOpenings,

    // The prefab only needs to support the required openings.
    SupportsRequiredOpenings
}

// This script goes on each room prefab.
// It tells the dungeon spawner what doorway slots this prefab supports.
public class RoomPrefab : MonoBehaviour
{
    [Header("Prefab Openings / Slots")]

    // True if this prefab has a possible doorway on the +Z side.
    public bool up;

    // True if this prefab has a possible doorway on the -Z side.
    public bool down;

    // True if this prefab has a possible doorway on the +X side.
    public bool right;

    // True if this prefab has a possible doorway on the -X side.
    public bool left;

    [Header("Matching")]

    // Controls how strictly this prefab must match a generated room.
    //
    // Use ExactOpenings if this is a baked room shape.
    // Example: a room prefab that physically only has one Down door.
    //
    // Use SupportsRequiredOpenings if this is a modular room.
    // Example: a room prefab with all four door/wall slots that can be turned on/off.
    public RoomPrefabMatchMode matchMode = RoomPrefabMatchMode.ExactOpenings;

    // Converts this prefab's doorway settings into a single number.
    //
    // Mask values:
    // Up    = 1
    // Down  = 2
    // Right = 4
    // Left  = 8
    //
    // Examples:
    // Up only              = 1
    // Down only            = 2
    // Up + Down            = 3
    // Right + Left         = 12
    // Up + Down + Right + Left = 15
    public int GetMask()
    {
        int mask = 0;

        if (up) mask |= DirectionUtility.ToMask(Direction.Up);
        if (down) mask |= DirectionUtility.ToMask(Direction.Down);
        if (right) mask |= DirectionUtility.ToMask(Direction.Right);
        if (left) mask |= DirectionUtility.ToMask(Direction.Left);

        return mask;
    }

    // Checks if this prefab can be used for a generated room.
    //
    // requiredMask comes from RoomNode.GetDoorMask().
    // It represents the doors the generated room needs.
    public bool MatchesRequiredMask(int requiredMask)
    {
        int prefabMask = GetMask();

        // Exact mode:
        // The prefab must have exactly the same doors as the generated room.
        //
        // Example:
        // Required = Up + Right
        // Prefab must = Up + Right
        if (matchMode == RoomPrefabMatchMode.ExactOpenings)
            return prefabMask == requiredMask;

        // Support mode:
        // The prefab can have extra door slots, as long as it includes all required ones.
        //
        // Example:
        // Required = Up + Right
        // Prefab = Up + Down + Right + Left
        // This is valid because the Room script can hide unused doors.
        return (prefabMask & requiredMask) == requiredMask;
    }
}