using UnityEngine;
using System.Collections;

// This script controls the player's room-to-room movement.
//
// The player does not freely walk through the dungeon grid.
// Instead, they:
// - rotate left/right
// - press W to move forward
// - move from the center of one room to the center of the next room
//
// Movement is only allowed if DungeonManager confirms that both rooms
// are connected by matching doors.
public class PlayerController : MonoBehaviour
{
    // Singleton reference so other scripts can access the player easily.
    public static PlayerController Instance;

    [Header("Movement")]

    // How fast the player moves between room centers.
    public float moveSpeed = 10f;

    // The player's current dungeon grid position.
    //
    // This must stay synced with the room the player is actually standing in.
    private Vector2Int currentGridPos = Vector2Int.zero;

    // True while the player is currently moving between rooms.
    //
    // This prevents the player from starting another movement
    // before the current movement is finished.
    private bool isMoving = false;

    private void Awake()
    {
        // Set up the singleton instance.
        Instance = this;
    }

    private void Update()
    {
        // Stop if there is no GameManager.
        if (GameManager.Instance == null)
            return;

        // The player can only move while exploring.
        //
        // This prevents movement during combat and floor transitions.
        if (!GameManager.Instance.IsExploring())
            return;

        // Do not allow input while already moving between rooms.
        if (isMoving)
            return;

        // W attempts to move forward into the room the player is facing.
        if (Input.GetKeyDown(KeyCode.W))
            TryMoveForward();

        // A rotates the player 90 degrees left.
        if (Input.GetKeyDown(KeyCode.A))
            Rotate(-90);

        // D rotates the player 90 degrees right.
        if (Input.GetKeyDown(KeyCode.D))
            Rotate(90);
    }

    // Sets the player's current grid position.
    //
    // DungeonRunManager calls this when placing the player in the start room.
    public void SetGridPosition(Vector2Int gridPosition)
    {
        currentGridPos = gridPosition;
    }

    // Returns the player's current grid position.
    public Vector2Int GetGridPosition()
    {
        return currentGridPos;
    }

    // Attempts to move the player forward into the next room.
    private void TryMoveForward()
    {
        // Convert the player's current facing direction into a dungeon direction.
        if (!DirectionUtility.TryFromWorldForward(transform.forward, out Direction direction))
            return;

        // Convert the dungeon direction into a grid movement.
        Vector2Int gridDirection = DirectionUtility.ToGridVector(direction);

        // Work out which grid position the player wants to move to.
        Vector2Int targetGridPos = currentGridPos + gridDirection;

        // Stop if there is no DungeonManager.
        if (DungeonManager.Instance == null)
            return;

        // Ask DungeonManager if movement between these rooms is allowed.
        //
        // This checks:
        // - the target room exists
        // - the current room has a door in that direction
        // - the target room has the opposite matching door
        if (!DungeonManager.Instance.CanMove(currentGridPos, targetGridPos))
            return;

        // Get the current room and next room.
        Room currentRoom = DungeonManager.Instance.GetRoom(currentGridPos);
        Room nextRoom = DungeonManager.Instance.GetRoom(targetGridPos);

        // Get the opposite direction for the door on the next room.
        Direction opposite = DirectionUtility.Opposite(direction);

        // Open the door on the current room.
        currentRoom?.OpenDoor(direction);

        // Open the matching door on the next room.
        nextRoom?.OpenDoor(opposite);

        // Get the world position of the next room's center.
        Vector3 targetPosition = DungeonManager.Instance.GetRoomCenter(targetGridPos);

        // Start moving the player to the next room center.
        StartCoroutine(MoveToRoom(
            targetPosition,
            targetGridPos,
            currentRoom,
            nextRoom,
            direction
        ));
    }

    // Moves the player smoothly from the current room center to the target room center.
    private IEnumerator MoveToRoom(
        Vector3 targetPosition,
        Vector2Int targetGridPos,
        Room previousRoom,
        Room nextRoom,
        Direction moveDirection)
    {
        // Block additional movement input.
        isMoving = true;

        // Put the game into Transition state while moving rooms.
        //
        // This stops combat/exploration logic from interfering during movement.
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Transition);

        // Find optional camera bob script.
        CameraBob camBob = GetComponentInChildren<CameraBob>();

        // Start camera bob while walking.
        if (camBob != null)
            camBob.StartWalking();

        // Move the player toward the target room center until close enough.
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        // Snap exactly to the target position.
        //
        // This prevents tiny movement inaccuracies from building up.
        transform.position = targetPosition;

        // Update the player's grid position now that they arrived.
        currentGridPos = targetGridPos;

        // Stop camera bob after arriving.
        if (camBob != null)
            camBob.StopWalking();

        // Get the opposite direction for the next room's door.
        Direction opposite = DirectionUtility.Opposite(moveDirection);

        // Close the door on the room the player came from.
        previousRoom?.CloseDoor(moveDirection);

        // Close the matching door on the room the player entered.
        nextRoom?.CloseDoor(opposite);

        // Allow movement input again.
        isMoving = false;

        // Tell DungeonManager the player has entered the new room.
        //
        // This may start combat if the room has not been visited before.
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.EnterRoom(targetGridPos);

        // If entering the room did not start combat,
        // return the game state to Exploring.
        //
        // If combat did start, DungeonManager will have changed the state to Combat,
        // so this check avoids overriding that.
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Transition)
            GameManager.Instance.SetState(GameState.Exploring);
    }

    // Rotates the player around the world Y axis.
    private void Rotate(int angle)
    {
        transform.Rotate(Vector3.up, angle, Space.World);
    }
}