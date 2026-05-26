using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    private Vector2Int currentGridPos = Vector2Int.zero;
    private bool isMoving = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsExploring())
            return;

        if (Input.GetKeyDown(KeyCode.W))
            TryMoveForward();

        if (Input.GetKeyDown(KeyCode.A))
            Rotate(-90);

        if (Input.GetKeyDown(KeyCode.D))
            Rotate(90);
    }

    private void TryMoveForward()
    {
        if (isMoving)
            return;

        Vector2Int dir = GetGridDirection();

        if (dir == Vector2Int.zero)
            return;

        Vector2Int target = currentGridPos + dir;

        if (DungeonManager.Instance.CanMove(currentGridPos, target))
        {
            Room currentRoom = DungeonManager.Instance.GetRoom(currentGridPos);
            Room nextRoom = DungeonManager.Instance.GetRoom(target);

            Direction moveDir = DirectionUtility.FromVector(dir);
            Direction opposite = DirectionUtility.Opposite(moveDir);

            currentRoom?.GetDoor(moveDir)?.Open();
            nextRoom?.GetDoor(opposite)?.Open();

            currentGridPos = target;

            Vector3 targetPos = DungeonManager.Instance.GetRoomCenter(target);

            StartCoroutine(
                MoveToRoom(
                    targetPos,
                    target,
                    currentRoom,
                    nextRoom,
                    moveDir
                )
            );
        }
    }

    private IEnumerator MoveToRoom(
        Vector3 targetPos,
        Vector2Int gridPos,
        Room previousRoom,
        Room nextRoom,
        Direction moveDir)
    {
        isMoving = true;

        float speed = 10f;

        CameraBob camBob = GetComponentInChildren<CameraBob>();

        if (camBob != null)
            camBob.StartWalking();

        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                speed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;

        if (camBob != null)
            camBob.StopWalking();

        Direction opposite = DirectionUtility.Opposite(moveDir);

        previousRoom?.GetDoor(moveDir)?.Close();
        nextRoom?.GetDoor(opposite)?.Close();

        isMoving = false;

        DungeonManager.Instance.EnterRoom(gridPos);
    }

    private void Rotate(int angle)
    {
        transform.Rotate(Vector3.up, angle);
    }

    private Vector2Int GetGridDirection()
    {
        Vector3 forward = transform.forward;

        if (Vector3.Dot(forward, Vector3.forward) > 0.5f)
            return Vector2Int.up;

        if (Vector3.Dot(forward, Vector3.back) > 0.5f)
            return Vector2Int.down;

        if (Vector3.Dot(forward, Vector3.right) > 0.5f)
            return Vector2Int.right;

        if (Vector3.Dot(forward, Vector3.left) > 0.5f)
            return Vector2Int.left;

        return Vector2Int.zero;
    }
}