using UnityEngine;
using System.Collections.Generic;

public enum Direction
{
    North,
    South,
    East,
    West
}

public static class DirectionUtility
{
    public static Vector2Int ToVector(Direction direction)
    {
        return direction switch
        {
            Direction.North => Vector2Int.up,
            Direction.South => Vector2Int.down,
            Direction.East => Vector2Int.right,
            Direction.West => Vector2Int.left,
            _ => Vector2Int.zero
        };
    }

    public static Direction Opposite(Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            _ => direction
        };
    }

    public static Direction FromVector(Vector2Int vector)
    {
        if (vector == Vector2Int.up) return Direction.North;
        if (vector == Vector2Int.down) return Direction.South;
        if (vector == Vector2Int.right) return Direction.East;
        if (vector == Vector2Int.left) return Direction.West;

        Debug.LogError($"Invalid direction vector: {vector}");
        return Direction.North;
    }

    public static int ToMask(Direction direction)
    {
        return direction switch
        {
            Direction.North => 1,
            Direction.South => 2,
            Direction.East => 4,
            Direction.West => 8,
            _ => 0
        };
    }
}

public class RoomNode
{
    public Vector2Int gridPos;

    public bool isStart;
    public bool isBoss;

    public Dictionary<Direction, RoomNode> connections = new();

    public bool HasConnection(Direction direction)
    {
        return connections.ContainsKey(direction);
    }

    public void Connect(Direction direction, RoomNode other)
    {
        Direction opposite = DirectionUtility.Opposite(direction);

        connections[direction] = other;
        other.connections[opposite] = this;
    }

    public int GetDoorMask()
    {
        int mask = 0;

        foreach (Direction direction in connections.Keys)
        {
            mask |= DirectionUtility.ToMask(direction);
        }

        return mask;
    }
}