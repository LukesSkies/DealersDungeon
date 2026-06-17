using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controls the Game Over screen.
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [Header("UI")]

    // Game Over screen panel.
    [SerializeField] private GameObject gameOverScreen;

    [Header("Buttons")]

    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Scenes")]

    // Main menu scene name.
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        Instance = this;

        Hide();
    }

    private void OnEnable()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartCurrentScene);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
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
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}