using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    Up,     // World +Z
    Down,   // World -Z
    Right,  // World +X
    Left    // World -X
}

public static class DirectionUtility
{
    // A reusable list of every direction.
    // This lets other scripts loop through all directions without manually writing them every time.
    public static readonly Direction[] AllDirections =
    {
        Direction.Up,
        Direction.Down,
        Direction.Right,
        Direction.Left
    };

    // Converts a Direction into a grid movement.
    // The dungeon layout is stored using Vector2Int.
    public static Vector2Int ToGridVector(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Vector2Int.up,       // +Z
            Direction.Down => Vector2Int.down,   // -Z
            Direction.Right => Vector2Int.right, // +X
            Direction.Left => Vector2Int.left,   // -X
            _ => Vector2Int.zero
        };
    }

    // Converts a Direction into an actual world-space direction.
    //
    // This is useful for physical movement, raycasts, offsets, or debugging.
    public static Vector3 ToWorldVector(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Vector3.forward,     // +Z
            Direction.Down => Vector3.back,      // -Z
            Direction.Right => Vector3.right,    // +X
            Direction.Left => Vector3.left,      // -X
            _ => Vector3.zero
        };
    }

    // Returns the opposite direction.
    //
    // This is extremely important for connecting rooms correctly:
    //
    // If Room A connects Up to Room B,
    // then Room B must connect Down back to Room A.
    //
    // If Room A connects Right to Room B,
    // then Room B must connect Left back to Room A.
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

    // Tries to convert a grid movement into a Direction.
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

    // Converts a grid vector into a Direction.
    //
    // This is similar to TryFromGridVector, but it logs an error if the vector is invalid.
    // Use this when the vector should always be valid.
    public static Direction FromGridVector(Vector2Int vector)
    {
        if (TryFromGridVector(vector, out Direction direction))
            return direction;

        Debug.LogError($"Invalid grid direction vector: {vector}");
        return Direction.Up;
    }

    // Converts the player's world forward direction into a dungeon Direction.
    //
    // This is used by the player movement script.
    //
    // Example:
    // If the player is looking mostly toward world +Z, this returns Direction.Up.
    // If the player is looking mostly toward world +X, this returns Direction.Right.
    public static bool TryFromWorldForward(Vector3 forward, out Direction direction)
    {
        // Ignore vertical camera/object tilt.
        // Dungeon movement only cares about the flat X/Z plane.
        forward.y = 0f;

        // If the forward vector is basically zero, it cannot be converted.
        if (forward.sqrMagnitude < 0.001f)
        {
            direction = Direction.Up;
            return false;
        }

        forward.Normalize();

        // Compare the player's forward direction against each world direction.
        float upDot = Vector3.Dot(forward, Vector3.forward);
        float downDot = Vector3.Dot(forward, Vector3.back);
        float rightDot = Vector3.Dot(forward, Vector3.right);
        float leftDot = Vector3.Dot(forward, Vector3.left);

        // Start by assuming Up is the closest direction.
        float bestDot = upDot;
        direction = Direction.Up;

        // If Down is closer, use Down.
        if (downDot > bestDot)
        {
            bestDot = downDot;
            direction = Direction.Down;
        }

        // If Right is closer, use Right.
        if (rightDot > bestDot)
        {
            bestDot = rightDot;
            direction = Direction.Right;
        }

        // If Left is closer, use Left.
        if (leftDot > bestDot)
        {
            bestDot = leftDot;
            direction = Direction.Left;
        }

        // Only accept the direction if the player is looking clearly enough toward it.
        return bestDot > 0.5f;
    }

    // Converts each Direction into a unique bitmask value.
    //
    // These masks are used to identify what doors a room needs.
    //
    // Up    = 1
    // Down  = 2
    // Right = 4
    // Left  = 8
    //
    // Combinations are created by adding these together.
    //
    // Example:
    // Up + Down = 1 + 2 = 3
    // Down + Left = 2 + 8 = 10
    // All directions = 1 + 2 + 4 + 8 = 15
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

    // Gives each direction a readable debug name.
    //
    // This is mainly useful for Debug.Log messages.
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

// A RoomNode is not the visible room GameObject.
// It is the data version of a room inside the generated dungeon layout.
//
// The generator creates RoomNodes first.
// Then the spawner turns each RoomNode into an actual room prefab.
public class RoomNode
{
    // The room's position in dungeon grid space.
    //
    // gridPos.x controls world X.
    // gridPos.y controls world Z.
    public Vector2Int gridPos;

    // True if this is the starting room.
    public bool isStart;

    // True if this is the boss room.
    public bool isBoss;

    // Stores connected rooms by direction.
    public Dictionary<Direction, RoomNode> connections = new();

    // Returns true if this room has a connection in the given direction.
    // means this room should have a Right door.
    public bool HasConnection(Direction direction)
    {
        return connections.ContainsKey(direction);
    }

    // Safely tries to get the connected room in a given direction.
    //
    // Returns true if one exists.
    // Returns false if there is no room connected that way.
    public bool TryGetConnection(Direction direction, out RoomNode roomNode)
    {
        return connections.TryGetValue(direction, out roomNode);
    }

    // Gets the connected room in a direction.
    //
    // Returns null if there is no connection.
    public RoomNode GetConnection(Direction direction)
    {
        connections.TryGetValue(direction, out RoomNode roomNode);
        return roomNode;
    }

    // Connects this room to another room.
    //
    // This automatically creates the matching opposite connection too.
    public void Connect(Direction direction, RoomNode other)
    {
        if (other == null)
            return;

        Direction opposite = DirectionUtility.Opposite(direction);

        connections[direction] = other;
        other.connections[opposite] = this;
    }

    // Builds a door mask from this room's connections.
    //
    // This is used by DungeonSpawner to pick a room prefab with the correct door layout.
    //
    // Example:
    // If this room has Up and Right connections:
    // Up = 1
    // Right = 4
    // Mask = 5
    public int GetDoorMask()
    {
        int mask = 0;

        foreach (Direction direction in connections.Keys)
        {
            mask |= DirectionUtility.ToMask(direction);
        }

        return mask;
    }

    // Returns how many rooms this room connects to.
    //
    // A room with 1 connection is usually a dead-end room.
    // A room with 4 connections is a four-way room.
    public int GetConnectionCount()
    {
        return connections.Count;
    }

    // Decides which room owns the visible door object between two connected rooms.
    //
    // This prevents two doors spawning on top of each other.
    //
    // The rooms are still connected logically.
    // This only affects which duplicate door mesh is shown.
    public bool OwnsDoorVisual(Direction direction)
    {
        if (!connections.TryGetValue(direction, out RoomNode other))
            return false;

        return gridPos.x < other.gridPos.x || gridPos.y < other.gridPos.y;
    }
}