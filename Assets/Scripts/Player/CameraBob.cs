using UnityEngine;

// This script adds a simple walking bob to the camera.
//
// PlayerController starts the bob when moving between rooms.
// PlayerController stops the bob when arriving.
public class CameraBob : MonoBehaviour
{
    [Header("Bob Settings")]

    // How high/low the camera moves while bobbing.
    public float bobAmount = 0.1f;

    // How fast the bob animation moves.
    public float bobSpeed = 8f;

    // Internal timer used for the sine wave.
    private float timer;

    // True while the player is walking.
    private bool walking;

    private void Update()
    {
        // If the game is over, stop bobbing and return camera to normal position.
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            StopWalking();
        }

        // If not walking, smoothly return the camera to its original local position.
        if (!walking)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                Vector3.zero,
                Time.unscaledDeltaTime * 5f
            );

            return;
        }

        // Advance bob timer.
        timer += Time.deltaTime * bobSpeed;

        // Calculate up/down bob using a sine wave.
        float y = Mathf.Sin(timer) * bobAmount;

        // Apply local camera bob.
        transform.localPosition = new Vector3(0f, y, 0f);
    }

    // Starts the walking bob.
    public void StartWalking()
    {
        walking = true;
    }

    // Stops the walking bob.
    public void StopWalking()
    {
        walking = false;
    }
}