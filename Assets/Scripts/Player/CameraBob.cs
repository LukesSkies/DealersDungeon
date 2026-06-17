using UnityEngine;

// Adds a simple walking bob to the camera.
public class CameraBob : MonoBehaviour
{
    [Header("Bob Settings")]

    // How far the camera moves up and down.
    public float bobAmount = 0.1f;

    // How fast the bob moves.
    public float bobSpeed = 8f;

    // Bob timer.
    private float timer;

    // True while walking.
    private bool walking;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            StopWalking();
        }

        // Return camera to normal when not walking.
        if (!walking)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                Vector3.zero,
                Time.unscaledDeltaTime * 5f
            );

            return;
        }

        timer += Time.deltaTime * bobSpeed;

        float y = Mathf.Sin(timer) * bobAmount;

        transform.localPosition = new Vector3(0f, y, 0f);
    }

    // Starts camera bob.
    public void StartWalking()
    {
        walking = true;
    }

    // Stops camera bob.
    public void StopWalking()
    {
        walking = false;
    }
}