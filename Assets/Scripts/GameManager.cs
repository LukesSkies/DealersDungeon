using UnityEngine;

// These are the main high-level game states.
//
// Exploring   = player can move between rooms.
// Combat      = player is fighting enemies.
// Transition  = player is moving between rooms / floors.
// GameOver    = player is dead and the game is stopped.
public enum GameState
{
    Exploring,
    Combat,
    Transition,
    GameOver
}

// This script manages the overall game state.
//
// Other scripts check this before allowing:
// - movement
// - combat
// - transitions
//
// It also handles Game Over.
public class GameManager : MonoBehaviour
{
    // Singleton reference so other scripts can call:
    // GameManager.Instance.SetState(...)
    // GameManager.Instance.GameOver()
    public static GameManager Instance;

    // The current state of the game.
    public GameState CurrentState { get; private set; }

    [Header("Game Over")]

    // If true, Time.timeScale becomes 0 when the player dies.
    //
    // This pauses most gameplay movement, animations, timers, enemy turns, etc.
    [SerializeField] private bool pauseTimeOnGameOver = true;

    // Stores the previous time scale so it can be restored later.
    private float previousTimeScale = 1f;

    private void Awake()
    {
        // Set up singleton instance.
        Instance = this;
    }

    private void Start()
    {
        // Make sure time is normal when the game starts.
        Time.timeScale = 1f;

        // Start in exploring state.
        SetState(GameState.Exploring);
    }

    // Changes the current game state.
    public void SetState(GameState newState)
    {
        // Once the game is over, do not let other scripts accidentally
        // switch the game back to Exploring or Combat.
        //
        // Example:
        // EnemyManager might try to set Exploring after enemies die.
        // This prevents that from overriding Game Over.
        if (CurrentState == GameState.GameOver && newState != GameState.GameOver)
            return;

        CurrentState = newState;
    }

    // Called when the player dies.
    public void GameOver()
    {
        // If already game over, do nothing.
        if (CurrentState == GameState.GameOver)
            return;

        // Save current time scale before pausing.
        previousTimeScale = Time.timeScale;

        // Set state to GameOver.
        CurrentState = GameState.GameOver;

        // Pause the game if enabled.
        if (pauseTimeOnGameOver)
            Time.timeScale = 0f;

        // Show the game over UI.
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Show();
        else
            Debug.LogWarning("Player died, but no GameOverUI exists in the scene.");
    }

    // Restores normal time.
    //
    // This is useful before restarting a scene or loading a menu.
    public void RestoreTime()
    {
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
    }

    // Resets the game state after game over.
    //
    // Use this before restarting gameplay.
    public void ResetGameState(GameState newState = GameState.Exploring)
    {
        RestoreTime();
        CurrentState = newState;
    }

    // Returns true if the game is in Exploring state.
    public bool IsExploring()
    {
        return CurrentState == GameState.Exploring;
    }

    // Returns true if the game is in Combat state.
    public bool IsCombat()
    {
        return CurrentState == GameState.Combat;
    }

    // Returns true if the game is in Transition state.
    public bool IsTransition()
    {
        return CurrentState == GameState.Transition;
    }

    // Returns true if the game is over.
    public bool IsGameOver()
    {
        return CurrentState == GameState.GameOver;
    }
}