using UnityEngine;
using System.Collections;

// Controls one physical dungeon door.
public class Door : MonoBehaviour
{
    // Direction this door belongs to.
    public Direction direction;

    [Header("Door Settings")]

    // How far the door opens.
    public float openAngle = 90f;

    // How fast the door opens and closes.
    public float openSpeed = 4f;

    [Header("Swing Direction")]

    // Flip this if the door opens the wrong way.
    public bool invertSwing = false;

    // Closed door rotation.
    private Quaternion closedRotation;

    // Open door rotation.
    private Quaternion openRotation;

    // Current door animation.
    private Coroutine animRoutine;

    private void Awake()
    {
        RecalculateRotations();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            RecalculateRotations();
    }

    // Sets the door direction.
    public void SetDirection(Direction newDirection)
    {
        direction = newDirection;
        RecalculateRotations();
    }

    // Calculates open and closed rotations.
    private void RecalculateRotations()
    {
        closedRotation = transform.localRotation;

        float angle = openAngle;

        if (direction == Direction.Up || direction == Direction.Left)
            angle *= -1f;

        if (invertSwing)
            angle *= -1f;

        openRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
    }

    // Opens the door.
    public void Open()
    {
        if (!gameObject.activeInHierarchy)
            return;

        RotateTo(openRotation);
    }

    // Closes the door.
    public void Close()
    {
        if (!gameObject.activeInHierarchy)
            return;

        RotateTo(closedRotation);
    }

    // Starts rotating to a target rotation.
    private void RotateTo(Quaternion target)
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(RotateRoutine(target));
    }

    // Smoothly rotates the door.
    private IEnumerator RotateRoutine(Quaternion target)
    {
        while (Quaternion.Angle(transform.localRotation, target) > 0.5f)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                target,
                Time.deltaTime * openSpeed
            );

            yield return null;
        }

        transform.localRotation = target;
    }
}