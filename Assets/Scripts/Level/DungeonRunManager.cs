using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Manages dungeon floor generation, difficulty progression, and floor changes.
public class DungeonRunManager : MonoBehaviour
{
    public static DungeonRunManager Instance;

    [Header("References")]
    [SerializeField] private DungeonGenerator generator;
    [SerializeField] private DungeonSpawner dungeonSpawner;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerController player;

    [Header("Start")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private Vector2Int startGridPosition = Vector2Int.zero;
    [SerializeField] private bool resetPlayerRotationOnFloorStart = true;

    [Header("Floor Progression")]
    [SerializeField] private int startingFloor = 1;
    [SerializeField] private int currentFloor = 1;

    [Tooltip("Enemy HP bonus gained every floor after floor 1.")]
    [SerializeField] private int enemyHealthIncreasePerFloor = 2;

    [Tooltip("Enemy damage bonus gained every other floor after floor 1. Floor 3 = first damage increase.")]
    [SerializeField] private int enemyDamageIncreaseEveryOtherFloor = 2;

    [Header("Enemy Layout Progression")]
    [SerializeField] private int floor1To2MinEnemies = 1;
    [SerializeField] private int floor1To2MaxEnemies = 3;

    [SerializeField] private int floor3To5MinEnemies = 2;
    [SerializeField] private int floor3To5MaxEnemies = 3;

    [SerializeField] private int floor6To8MinEnemies = 2;
    [SerializeField] private int floor6To8MaxEnemies = 4;

    [SerializeField] private int floor9PlusMinEnemies = 3;
    [SerializeField] private int floor9PlusMaxEnemies = 5;

    [Header("Floor Transition")]
    [SerializeField] private float nextFloorDelay = 1f;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text floorText;

    private Coroutine floorRoutine;

    public int CurrentFloor => Mathf.Max(1, currentFloor);
    public int EnemyHealthIncreasePerFloor => Mathf.Max(0, enemyHealthIncreasePerFloor);
    public int EnemyDamageIncreaseEveryOtherFloor => Mathf.Max(0, enemyDamageIncreaseEveryOtherFloor);

    private void Awake()
    {
        Instance = this;
        currentFloor = Mathf.Max(1, startingFloor);
    }

    private void Start()
    {
        ResolveReferences();
        UpdateFloorUI();

        if (generateOnStart)
            GenerateNewFloor();
    }

    // Finds missing scene references.
    private void ResolveReferences()
    {
        if (generator == null)
            generator = FindFirstObjectByType<DungeonGenerator>();

        if (dungeonSpawner == null)
            dungeonSpawner = FindFirstObjectByType<DungeonSpawner>();

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (player == null)
            player = PlayerController.Instance;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    // Starts a fresh run from the starting floor.
    public void StartNewRun()
    {
        currentFloor = Mathf.Max(1, startingFloor);

        ResolveReferences();

        if (enemySpawner != null)
            enemySpawner.ResetBossRunProgress();

        GenerateNewFloor();
    }

    // Generates the current floor.
    public void GenerateNewFloor()
    {
        ResolveReferences();
        UpdateFloorUI();

        if (generator == null)
        {
            Debug.LogError("No DungeonGenerator found.");
            return;
        }

        if (dungeonSpawner == null)
        {
            Debug.LogError("No DungeonSpawner found.");
            return;
        }

        if (enemySpawner != null)
            enemySpawner.ClearEnemies();

        List<RoomNode> dungeon = generator.Generate();

        dungeonSpawner.BuildDungeon(dungeon);

        if (DungeonMinimapUI.Instance != null)
            DungeonMinimapUI.Instance.BuildMap(dungeon, startGridPosition);

        MovePlayerToStartRoom();

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Exploring);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.EnterRoom(startGridPosition);
    }

    // Goes to the next floor using the default delay.
    public void GoToNextFloor()
    {
        GoToNextFloor(nextFloorDelay);
    }

    // Goes to the next floor after a delay.
    public void GoToNextFloor(float delay)
    {
        if (floorRoutine != null)
            StopCoroutine(floorRoutine);

        floorRoutine = StartCoroutine(GoToNextFloorRoutine(delay));
    }

    // Handles the delayed floor transition.
    private IEnumerator GoToNextFloorRoutine(float delay)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Transition);

        yield return new WaitForSeconds(delay);

        currentFloor++;
        GenerateNewFloor();

        floorRoutine = null;
    }

    // Gets the HP bonus for the current floor.
    public int GetEnemyHealthBonusForCurrentFloor()
    {
        return GetEnemyHealthBonusForFloor(CurrentFloor);
    }

    // Gets the damage bonus for the current floor.
    public int GetEnemyDamageBonusForCurrentFloor()
    {
        return GetEnemyDamageBonusForFloor(CurrentFloor);
    }

    // Every floor after floor 1 gives +HP.
    public int GetEnemyHealthBonusForFloor(int floorNumber)
    {
        floorNumber = Mathf.Max(1, floorNumber);

        return (floorNumber - 1) * EnemyHealthIncreasePerFloor;
    }

    // Every other floor after floor 1 gives +damage.
    // Floor 1 = +0
    // Floor 2 = +0
    // Floor 3 = +2
    // Floor 4 = +2
    // Floor 5 = +4
    public int GetEnemyDamageBonusForFloor(int floorNumber)
    {
        floorNumber = Mathf.Max(1, floorNumber);

        return ((floorNumber - 1) / 2) * EnemyDamageIncreaseEveryOtherFloor;
    }

    // Gets the enemy count range for a floor.
    public void GetEnemyCountRangeForFloor(int floorNumber, out int minEnemies, out int maxEnemies)
    {
        floorNumber = Mathf.Max(1, floorNumber);

        if (floorNumber <= 2)
        {
            minEnemies = floor1To2MinEnemies;
            maxEnemies = floor1To2MaxEnemies;
        }
        else if (floorNumber <= 5)
        {
            minEnemies = floor3To5MinEnemies;
            maxEnemies = floor3To5MaxEnemies;
        }
        else if (floorNumber <= 8)
        {
            minEnemies = floor6To8MinEnemies;
            maxEnemies = floor6To8MaxEnemies;
        }
        else
        {
            minEnemies = floor9PlusMinEnemies;
            maxEnemies = floor9PlusMaxEnemies;
        }

        minEnemies = Mathf.Max(1, minEnemies);
        maxEnemies = Mathf.Max(minEnemies, maxEnemies);
    }

    // Gets the current floor enemy count range.
    public void GetEnemyCountRangeForCurrentFloor(out int minEnemies, out int maxEnemies)
    {
        GetEnemyCountRangeForFloor(CurrentFloor, out minEnemies, out maxEnemies);
    }

    // Moves the player to the start room.
    private void MovePlayerToStartRoom()
    {
        if (player == null)
            return;

        if (DungeonManager.Instance == null)
            return;

        Vector3 center = DungeonManager.Instance.GetRoomCenter(startGridPosition);

        player.transform.position = center;
        player.SetGridPosition(startGridPosition);

        if (resetPlayerRotationOnFloorStart)
            player.transform.rotation = Quaternion.identity;
    }

    // Updates optional floor UI.
    private void UpdateFloorUI()
    {
        if (floorText != null)
            floorText.text = "Floor: " + CurrentFloor;
    }

    private void OnValidate()
    {
        if (startingFloor < 1)
            startingFloor = 1;

        if (currentFloor < 1)
            currentFloor = 1;

        if (enemyHealthIncreasePerFloor < 0)
            enemyHealthIncreasePerFloor = 0;

        if (enemyDamageIncreaseEveryOtherFloor < 0)
            enemyDamageIncreaseEveryOtherFloor = 0;

        if (nextFloorDelay < 0f)
            nextFloorDelay = 0f;

        ValidateEnemyRange(ref floor1To2MinEnemies, ref floor1To2MaxEnemies);
        ValidateEnemyRange(ref floor3To5MinEnemies, ref floor3To5MaxEnemies);
        ValidateEnemyRange(ref floor6To8MinEnemies, ref floor6To8MaxEnemies);
        ValidateEnemyRange(ref floor9PlusMinEnemies, ref floor9PlusMaxEnemies);
    }

    private void ValidateEnemyRange(ref int minEnemies, ref int maxEnemies)
    {
        if (minEnemies < 1)
            minEnemies = 1;

        if (maxEnemies < minEnemies)
            maxEnemies = minEnemies;
    }
}