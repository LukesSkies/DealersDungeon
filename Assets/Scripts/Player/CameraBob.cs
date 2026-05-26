using UnityEngine;

public class CameraBob : MonoBehaviour
{
    public float bobAmount = 0.1f;
    public float bobSpeed = 8f;

    private float timer;
    private bool walking;

    void Update()
    {
        if (!walking)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                Vector3.zero,
                Time.deltaTime * 5f
            );
            return;
        }

        timer += Time.deltaTime * bobSpeed;

        float y = Mathf.Sin(timer) * bobAmount;

        transform.localPosition = new Vector3(0, y, 0);
    }

    public void StartWalking()
    {
        walking = true;
    }

    public void StopWalking()
    {
        walking = false;
    }
}