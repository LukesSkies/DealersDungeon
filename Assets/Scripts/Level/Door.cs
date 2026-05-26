using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    public Direction direction;

    [Header("Door Settings")]
    public float openAngle = 90f;
    public float openSpeed = 4f;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private Coroutine animRoutine;

    void Awake()
    {
        closedRotation = transform.localRotation;

        float angle = openAngle;

        if (direction == Direction.North ||
            direction == Direction.West)
        {
            angle *= -1f;
        }

        openRotation =
            closedRotation *
            Quaternion.Euler(0, angle, 0);
    }

    public void Open()
    {
        RotateTo(openRotation);
    }

    public void Close()
    {
        RotateTo(closedRotation);
    }

    void RotateTo(Quaternion target)
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(RotateRoutine(target));
    }

    IEnumerator RotateRoutine(Quaternion target)
    {
        while (Quaternion.Angle(transform.localRotation, target) > 0.5f)
        {
            transform.localRotation =
                Quaternion.Slerp(
                    transform.localRotation,
                    target,
                    Time.deltaTime * openSpeed
                );

            yield return null;
        }

        transform.localRotation = target;
    }
}