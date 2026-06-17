using System.Collections.Generic;
using UnityEngine;

// Dungeon directions.
public enum Direction
{
    Up,
    Down,
    Right,
    Left
}

// Helper methods for dungeon directions.
public static class DirectionUtility
{
    // Every direction.
    public static readonly Direction[] AllDirections =
    {
        Direction.Up,
        Direction.Down,
        Direction.Right,
        Direction.Left
    };

    // Converts a direction to grid movement.
    public static Vector2Int ToGridVector(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Vector2Int.up,
            Direction.Down => Vector2Int.down,
            Direction.Right => Vector2Int.right,
            Direction.Left => Vector2Int.left,
            _ => Vector2Int.zero
        };
    }

    // Converts a direction to world movement.
    public static Vector3 ToWorldVector(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Right => Vector3.right,
            Direction.Left => Vector3.left,
            _ => Vector3.zero
        };
    }

    // Gets the opposite direction.
    public static Direction Opposite(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Right => Direction.Left,
            Direction.Left => Direction.Right,
            _ => direction
        };
    }

    // Converts grid movement into a direction.
    public static bool TryFromGridVector(Vector2Int vector, out Direction direction)
    {
        if (vector == Vector2Int.up)
        {
            direction = Direction.Up;
            return true;
        }

        if (vector == Vector2Int.down)
        {
            direction = Direction.Down;
            return true;
        }

        if (vector == Vector2Int.right)
        {
            direction = Direction.Right;
            return true;
        }

        if (vector == Vector2Int.left)
        {
            direction = Direction.Left;
            return true;
        }

        direction = Direction.Up;
        return false;
    }

    // Converts grid movement into a direction and logs if invalid.
    public static Direction FromGridVector(Vector2Int vector)
    {
        if (TryFromGridVector(vector, out Direction direction))
            return direction;

        Debug.LogError($"Invalid grid direction vector: {vector}");
        return Direction.Up;
    }

    // Converts world forward direction into a dungeon direction.
    public static bool TryFromWorldForward(Vector3 forward, out Direction direction)
    {
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            direction = Direction.Up;
            return false;
        }

        forward.Normalize();

        float upDot = Vector3.Dot(forward, Vector3.forward);
        float downDot = Vector3.Dot(forward, Vector3.back);
        float rightDot = Vector3.Dot(forward, Vector3.right);
        float leftDot = Vector3.Dot(forward, Vector3.left);

        float bestDot = upDot;
        direction = Direction.Up;

        if (downDot > bestDot)
        {
            bestDot = downDot;
            direction = Direction.Down;
        }

        if (rightDot > bestDot)
        {
            bestDot = rightDot;
            direction = Direction.Right;
        }

        if (leftDot > bestDot)
        {
            bestDot = leftDot;
            direction = Direction.Left;
        }

        return bestDot > 0.5f;
    }

    // Converts a direction into a mask value.
    public static int ToMask(Direction direction)
    {
        return direction switch
        {
            Direction.Up => 1,
            Direction.Down => 2,
            Direction.Right => 4,
            Direction.Left => 8,
            _ => 0
        };
    }

    // Gets a readable direction name.
    public static string ToDisplayName(Direction direction)
    {
        return direction switch
        {
            Direction.Up => "Up (+Z)",
            Direction.Down => "Down (-Z)",
            Direction.Right => "Right (+X)",
            Direction.Left => "Left (-X)",
            _ => direction.ToString()
        };
    }
}

// Data version of one dungeon room.
public class RoomNode
{
    // Grid position.
    public Vector2Int gridPos;

    // True if this is the start room.
    public bool isStart;

    // True if this is the boss room.
    public bool isBoss;

    // Connected rooms by direction.
    public Dictionary<Direction, RoomNode> connections = new();

    // Checks if this room has a connection.
    public bool HasConnection(Direction direction)
    {
        return connections.ContainsKey(direction);
    }

    // Tries to get a connected room.
    public bool TryGetConnection(Direction direction, out RoomNode roomNode)
    {
        return connections.TryGetValue(direction, out roomNode);
    }

    // Gets a connected room.
    public RoomNode GetConnection(Direction direction)
    {
        connections.TryGetValue(direction, out RoomNode roomNode);
        return roomNode;
    }

    // Connects this room to another room.
    public void Connect(Direction direction, RoomNode other)
    {
        if (other == null)
            return;

        Direction opposite = DirectionUtility.Opposite(direction);

        connections[direction] = other;
        other.connections[opposite] = this;
    }

    // Gets the room's door mask.
    public int GetDoorMask()
    {
        int mask = 0;

        foreach (Direction direction in connections.Keys)
        {
            mask |= DirectionUtility.ToMask(direction);
        }

        return mask;
    }

    // Gets how many connections this room has.
    public int GetConnectionCount()
    {
        return connections.Count;
    }

    // Decides which room shows the shared door visual.
    public bool OwnsDoorVisual(Direction direction)
    {
        if (!connections.TryGetValue(direction, out RoomNode other))
            return false;

        return gridPos.x < other.gridPos.x || gridPos.y < other.gridPos.y;
    }
}