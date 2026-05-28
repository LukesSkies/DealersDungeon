using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// This script controls the Game Over screen.
//
// It can:
// - show the game over screen
// - hide the game over screen
// - restart the current scene
// - return to a menu scene
// - quit the game
public class GameOverUI : MonoBehaviour
{
    // Singleton reference so PlayerHealth/GameManager can show the screen easily.
    public static GameOverUI Instance;

    [Header("UI")]

    // The root GameObject for the Game Over screen.
    //
    // This should be the full panel that appears when the player dies.
    [SerializeField] private GameObject gameOverScreen;

    [Header("Buttons")]

    // Optional restart button.
    [SerializeField] private Button restartButton;

    // Optional main menu button.
    [SerializeField] private Button mainMenuButton;

    // Optional quit button.
    [SerializeField] private Button quitButton;

    [Header("Scenes")]

    // Name of your main menu scene.
    //
    // Only needed if you use the main menu button.
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        // Set up singleton instance.
        Instance = this;

        // Hide the game over screen at the start.
        Hide();
    }

    private void OnEnable()
    {
        // Add button listeners if buttons are assigned.
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartCurrentScene);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
        // Remove button listeners to prevent duplicate events.
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartCurrentScene);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    // Shows the Game Over screen.
    public void Show()
    {
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    // Hides the Game Over screen.
    public void Hide()
    {
        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
    }

    // Restarts the current scene.
    public void RestartCurrentScene()
    {
        // Restore time before loading the scene.
        //
        // If Time.timeScale stays at 0, the restarted scene may appear frozen.
        if (GameManager.Instance != null)
            GameManager.Instance.ResetGameState(GameState.Exploring);
        else
            Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // Loads the main menu scene.
    public void ReturnToMainMenu()
    {
        // Restore time before loading the menu.
        if (GameManager.Instance != null)
            GameManager.Instance.ResetGameState(GameState.Exploring);
        else
            Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("Main menu scene name is empty.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Quits the game.
    public void QuitGame()
    {
        // Restore time before quitting/stopping play mode.
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}