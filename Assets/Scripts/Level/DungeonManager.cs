using System.Collections.Generic;
using UnityEngine;

// This script manages the dungeon after it has been generated and spawned.
//
// It keeps track of:
// - which rooms exist
// - which RoomNode data belongs to each room
// - which room the player is currently in
// - which rooms have already been visited
// - whether the player is allowed to move between two rooms
// - when combat should start
public class DungeonManager : MonoBehaviour
{
    // Singleton reference so other scripts can easily access the DungeonManager.
    public static DungeonManager Instance;

    [Header("Combat")]

    // If true, the starting room can begin combat when first entered.
    //
    // Usually this should be false because the start room is a safe starting area.
    public bool startRoomStartsCombat = false;

    // If true, combat starts the first time the player enters a new room.
    //
    // If false, entering rooms will not automatically start combat.
    public bool firstVisitStartsCombat = true;

    // Stores the actual spawned Room components by grid position.
    //
    // Key = grid position.
    // Value = spawned Room component in the scene.
    private Dictionary<Vector2Int, Room> rooms = new();

    // Stores the generated RoomNode data by grid position.
    //
    // This is used for connection checks.
    //
    // The Room tells us about the actual prefab.
    // The RoomNode tells us what rooms connect logically.
    private Dictionary<Vector2Int, RoomNode> roomNodes = new();

    // Tracks which rooms the player has already entered.
    //
    // This prevents combat starting every time the player walks back into a room.
    private HashSet<Vector2Int> visited = new();

    // The grid position of the room the player is currently inside.
    private Vector2Int currentRoom;

    private void Awake()
    {
        // Set up the singleton instance.
        Instance = this;
    }

    // Called by DungeonSpawner after it has spawned all rooms.
    //
    // This gives the manager:
    // - the actual spawned Room objects
    // - the RoomNode layout data
    public void RegisterRooms(Dictionary<Vector2Int, Room> spawnedRooms, List<RoomNode> nodes)
    {
        // Copy the spawned rooms dictionary.
        rooms = new Dictionary<Vector2Int, Room>(spawnedRooms);

        // Clear old floor data.
        roomNodes.Clear();
        visited.Clear();

        // Store each RoomNode by its grid position.
        foreach (RoomNode node in nodes)
        {
            roomNodes[node.gridPos] = node;
        }

        // The player starts at grid position zero.
        currentRoom = Vector2Int.zero;
    }

    // Gets the spawned Room at a grid position.
    //
    // Returns null if there is no room there.
    public Room GetRoom(Vector2Int position)
    {
        rooms.TryGetValue(position, out Room room);
        return room;
    }

    // Returns true if a spawned room exists at this grid position.
    public bool HasRoom(Vector2Int position)
    {
        return rooms.ContainsKey(position);
    }

    // Checks if the player is allowed to move from one room to another.
    //
    // This prevents:
    // - moving into empty space
    // - walking through walls
    // - moving through a door that does not connect correctly
    public bool CanMove(Vector2Int from, Vector2Int to)
    {
        // Make sure the starting room exists in the RoomNode data.
        if (!roomNodes.TryGetValue(from, out RoomNode fromNode))
            return false;

        // Make sure the target room exists in the RoomNode data.
        if (!roomNodes.TryGetValue(to, out RoomNode toNode))
            return false;

        // Work out the movement direction.
        //
        // Example:
        // from = (0, 0)
        // to   = (0, 1)
        // delta = (0, 1), which means Direction.Up.
        Vector2Int delta = to - from;

        // Convert the movement delta into a dungeon direction.
        //
        // If the delta is invalid, movement is blocked.
        // Invalid examples:
        // (0, 0), (1, 1), (2, 0), etc.
        if (!DirectionUtility.TryFromGridVector(delta, out Direction direction))
            return false;

        // Get the opposite direction.
        //
        // Example:
        // If moving Up, the target room must have a Down connection.
        Direction opposite = DirectionUtility.Opposite(direction);

        // Check that the current room has a connection in the movement direction.
        //
        // Example:
        // If the player is moving Right,
        // the current room must have a Right connection.
        if (!fromNode.TryGetConnection(direction, out RoomNode connectedNode))
            return false;

        // Make sure the room connected in that direction is actually the target room.
        //
        // This is an extra safety check that prevents mismatched connections.
        if (connectedNode != toNode)
            return false;

        // Finally, make sure the target room has the matching opposite connection.
        //
        // Example:
        // Current room has Right connection.
        // Target room must have Left connection.
        return toNode.HasConnection(opposite);
    }

    // Called when the player enters a room.
    //
    // This updates the current room,
    // records the room as visited,
    // and optionally starts combat.
    public void EnterRoom(Vector2Int position)
    {
        // Store the player's current room position.
        currentRoom = position;

        // If no room exists here, do nothing.
        if (!rooms.ContainsKey(position))
            return;

        // If this room was already visited, do not start combat again.
        if (visited.Contains(position))
            return;

        // Mark this room as visited.
        visited.Add(position);

        // If combat is disabled on first visit, stop here.
        if (!firstVisitStartsCombat)
            return;

        // If this is the start room and start room combat is disabled, stop here.
        if (position == Vector2Int.zero && !startRoomStartsCombat)
            return;

        // Start combat in this room.
        StartCombat(position);
    }

    // Gets the center position of a room.
    //
    // Player movement uses this so the player moves from room center to room center.
    public Vector3 GetRoomCenter(Vector2Int position)
    {
        if (rooms.TryGetValue(position, out Room room))
            return room.GetCenter();

        return Vector3.zero;
    }

    // Returns true if the player's current room is the boss room.
    //
    // EnemyManager uses this to know when clearing enemies should move to the next floor.
    public bool IsCurrentRoomBoss()
    {
        if (!rooms.TryGetValue(currentRoom, out Room room))
            return false;

        return room.isBossRoom;
    }

    // Returns the grid position of the room the player is currently in.
    public Vector2Int GetCurrentRoomPosition()
    {
        return currentRoom;
    }

    // Starts combat inside a room.
    //
    // This:
    // - changes the game state to Combat
    // - spawns enemies
    // - starts the player's combat hand
    private void StartCombat(Vector2Int position)
    {
        // Tell the game it is now in combat.
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Combat);

        // Make sure the room exists.
        if (rooms.TryGetValue(position, out Room room))
        {
            // Find the EnemySpawner in the scene.
            EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();

            // Spawn enemies if both the spawner and player exist.
            if (spawner != null && PlayerController.Instance != null)
            {
                // Random enemy count from 1 to 3.
                int count = Random.Range(1, 4);

                // Spawn enemies in front of the player.
                spawner.SpawnEnemiesFacingPlayer(
                    PlayerController.Instance.transform,
                    count
                );
            }
        }

        // Start the player's combat hand.
        if (HandManager.Instance != null)
            HandManager.Instance.StartCombatHand();
    }
}