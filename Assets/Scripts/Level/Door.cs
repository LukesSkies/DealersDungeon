using UnityEngine;
using System.Collections;

// This script controls one physical door in a room.
// Each door knows which dungeon direction it belongs to:
// Up, Down, Right, or Left.
public class Door : MonoBehaviour
{
    // The direction this door faces in the dungeon grid.
    //
    // Up    = +Z
    // Down  = -Z
    // Right = +X
    // Left  = -X
    public Direction direction;

    [Header("Door Settings")]

    // How far the door rotates when opened.
    // 90 degrees gives a normal full door swing.
    public float openAngle = 90f;

    // How quickly the door rotates open/closed.
    public float openSpeed = 4f;

    [Header("Swing Direction")]

    // Use this if this specific door opens the wrong way.
    // This is safer than constantly changing the code,
    // because door mesh pivots can be different between prefabs.
    public bool invertSwing = false;

    // The door's original local rotation.
    // This is saved when the door starts.
    private Quaternion closedRotation;

    // The target rotation when the door is open.
    private Quaternion openRotation;

    // Stores the currently running open/close animation.
    // This lets us stop the old animation before starting a new one.
    private Coroutine animRoutine;

    private void Awake()
    {
        // Calculate the closed and open rotations when the door is first created.
        RecalculateRotations();
    }

    private void OnValidate()
    {
        // Recalculate while in Play Mode if values are changed in the Inspector.
        //
        // This only runs during Play Mode because changing prefab rotations
        // while editing can give confusing results.
        if (Application.isPlaying)
            RecalculateRotations();
    }

    // Lets another script set this door's direction.
    //
    // Room.Setup() uses this so each door is automatically told whether it is
    // Up, Down, Right, or Left.
    public void SetDirection(Direction newDirection)
    {
        direction = newDirection;
        RecalculateRotations();
    }

    // Calculates the door's closed and open rotations.
    private void RecalculateRotations()
    {
        // The closed rotation is whatever rotation the door already has in the prefab.
        closedRotation = transform.localRotation;

        float angle = openAngle;

        // Default swing rule.
        //
        // Up and Left doors swing one way.
        // Down and Right doors swing the other way.
        //
        // Keep only ONE directional flip rule.
        if (direction == Direction.Up || direction == Direction.Left)
            angle *= -1f;

        // Optional per-door correction from the Inspector.
        //
        // Tick this on a door prefab if that door opens the wrong way.
        if (invertSwing)
            angle *= -1f;

        // The open rotation is the closed rotation plus a Y-axis swing.
        openRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
    }

    // Opens the door.
    public void Open()
    {
        // If this door object is disabled, do nothing.
        //
        // This matters because stacked/duplicate door visuals may be disabled.
        if (!gameObject.activeInHierarchy)
            return;

        RotateTo(openRotation);
    }

    // Closes the door.
    public void Close()
    {
        // If this door object is disabled, do nothing.
        if (!gameObject.activeInHierarchy)
            return;

        RotateTo(closedRotation);
    }

    // Starts rotating the door toward a target rotation.
    private void RotateTo(Quaternion target)
    {
        // Stop the previous animation if one is already running.
        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(RotateRoutine(target));
    }

    // Smoothly rotates the door until it reaches the target rotation.
    private IEnumerator RotateRoutine(Quaternion target)
    {
        // Keep rotating until the door is almost exactly at the target.
        while (Quaternion.Angle(transform.localRotation, target) > 0.5f)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                target,
                Time.deltaTime * openSpeed
            );

            yield return null;
        }

        // Snap exactly to the final rotation at the end.
        // This prevents tiny floating-point rotation errors.
        transform.localRotation = target;
    }
}