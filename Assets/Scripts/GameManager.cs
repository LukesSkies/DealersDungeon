using UnityEngine;

public enum GameState
{
    Exploring,
    Combat,
    Transition
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetState(GameState.Exploring);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
    }

    public bool IsExploring() => CurrentState == GameState.Exploring;
    public bool IsCombat() => CurrentState == GameState.Combat;
}