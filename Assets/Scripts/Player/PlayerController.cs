using UnityEngine;
using System.Collections;

// Controls player movement between rooms.
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    [Header("Movement")]

    // Movement speed between rooms.
    public float moveSpeed = 10f;

    // Current room grid position.
    private Vector2Int currentGridPos = Vector2Int.zero;

    // True while moving between rooms.
    private bool isMoving = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (!GameManager.Instance.IsExploring())
            return;

        if (isMoving)
            return;

        // Move forward.
        if (Input.GetKeyDown(KeyCode.W))
            TryMoveForward();

        // Turn left.
        if (Input.GetKeyDown(KeyCode.A))
            Rotate(-90);

        // Turn right.
        if (Input.GetKeyDown(KeyCode.D))
            Rotate(90);
    }

    // Sets the current grid position.
    public void SetGridPosition(Vector2Int gridPosition)
    {
        currentGridPos = gridPosition;
    }

    // Gets the current grid position.
    public Vector2Int GetGridPosition()
    {
        return currentGridPos;
    }

    // Tries to move into the room ahead.
    private void TryMoveForward()
    {
        if (!DirectionUtility.TryFromWorldForward(transform.forward, out Direction direction))
            return;

        Vector2Int gridDirection = DirectionUtility.ToGridVector(direction);

        Vector2Int targetGridPos = currentGridPos + gridDirection;

        if (DungeonManager.Instance == null)
            return;

        if (!DungeonManager.Instance.CanMove(currentGridPos, targetGridPos))
            return;

        Room currentRoom = DungeonManager.Instance.GetRoom(currentGridPos);
        Room nextRoom = DungeonManager.Instance.GetRoom(targetGridPos);

        Direction opposite = DirectionUtility.Opposite(direction);

        currentRoom?.OpenDoor(direction);
        nextRoom?.OpenDoor(opposite);

        Vector3 targetPosition = DungeonManager.Instance.GetRoomCenter(targetGridPos);

        StartCoroutine(MoveToRoom(
            targetPosition,
            targetGridPos,
            currentRoom,
            nextRoom,
            direction
        ));
    }

    // Moves the player to another room.
    private IEnumerator MoveToRoom(
        Vector3 targetPosition,
        Vector2Int targetGridPos,
        Room previousRoom,
        Room nextRoom,
        Direction moveDirection)
    {
        isMoving = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Transition);

        CameraBob camBob = GetComponentInChildren<CameraBob>();

        if (camBob != null)
            camBob.StartWalking();

        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;

        currentGridPos = targetGridPos;

        if (camBob != null)
            camBob.StopWalking();

        Direction opposite = DirectionUtility.Opposite(moveDirection);

        previousRoom?.CloseDoor(moveDirection);
        nextRoom?.CloseDoor(opposite);

        isMoving = false;

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.EnterRoom(targetGridPos);

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Transition)
            GameManager.Instance.SetState(GameState.Exploring);
    }

    // Rotates the player.
    private void Rotate(int angle)
    {
        transform.Rotate(Vector3.up, angle, Space.World);
    }
}