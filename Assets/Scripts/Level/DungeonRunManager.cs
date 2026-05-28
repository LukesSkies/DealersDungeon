using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script manages full dungeon runs/floor generation.
//
// It handles:
// - finding the generator, spawner, and player
// - generating a new floor
// - moving the player to the start room
// - moving to the next floor after boss completion
public class DungeonRunManager : MonoBehaviour
{
    // Singleton reference.
    public static DungeonRunManager Instance;

    [Header("References")]
    [SerializeField] private DungeonGenerator generator;
    [SerializeField] private DungeonSpawner spawner;
    [SerializeField] private PlayerController player;

    [Header("Start")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private Vector2Int startGridPosition = Vector2Int.zero;
    [SerializeField] private bool resetPlayerRotationOnFloorStart = true;

    [Header("Floor Transition")]
    [SerializeField] private float nextFloorDelay = 1f;

    private Coroutine floorRoutine;

    private void Awake()
    {
        // Set up singleton reference.
        Instance = this;
    }

    private void Start()
    {
        ResolveReferences();

        if (generateOnStart)
            GenerateNewFloor();
    }

    // Finds references automatically if they were not assigned in the Inspector.
    private void ResolveReferences()
    {
        if (generator == null)
            generator = FindFirstObjectByType<DungeonGenerator>();

        if (spawner == null)
            spawner = FindFirstObjectByType<DungeonSpawner>();

        if (player == null)
            player = PlayerController.Instance;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    // Generates and starts a new floor.
    public void GenerateNewFloor()
    {
        ResolveReferences();

        if (generator == null)
        {
            Debug.LogError("No DungeonGenerator found.");
            return;
        }

        if (spawner == null)
        {
            Debug.LogError("No DungeonSpawner found.");
            return;
        }

        // Generate the logical dungeon.
        List<RoomNode> dungeon = generator.Generate();

        // Spawn the dungeon rooms in the world.
        spawner.BuildDungeon(dungeon);

        // Build the minimap from the exact same dungeon layout.
        //
        // This makes the minimap match the current level layout exactly.
        //
        // The startGridPosition is passed in so the start room can appear
        // dead centre on the minimap.
        if (DungeonMinimapUI.Instance != null)
            DungeonMinimapUI.Instance.BuildMap(dungeon, startGridPosition);

        // Move player into the start room.
        MovePlayerToStartRoom();

        // Set state to exploring before entering the start room.
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Exploring);

        // Enter the start room.
        //
        // This marks the room as visited and reveals the minimap from that room.
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.EnterRoom(startGridPosition);
    }

    // Moves to the next floor using the default delay.
    public void GoToNextFloor()
    {
        GoToNextFloor(nextFloorDelay);
    }

    // Moves to the next floor using a specified delay.
    public void GoToNextFloor(float delay)
    {
        if (floorRoutine != null)
            StopCoroutine(floorRoutine);

        floorRoutine = StartCoroutine(GoToNextFloorRoutine(delay));
    }

    // Waits, then generates the next floor.
    private IEnumerator GoToNextFloorRoutine(float delay)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Transition);

        yield return new WaitForSeconds(delay);

        GenerateNewFloor();

        floorRoutine = null;
    }

    // Moves the player to the center of the start room.
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
}