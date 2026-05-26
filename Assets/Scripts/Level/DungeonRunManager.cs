using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script manages the overall dungeon run.
//
// It controls:
// - generating the first floor
// - generating new floors after the boss room is cleared
// - finding the needed dungeon/player references
// - moving the player back to the start room when a new floor begins
public class DungeonRunManager : MonoBehaviour
{
    // Singleton reference so other scripts can call it.
    public static DungeonRunManager Instance;

    [Header("References")]

    // The script that creates the dungeon layout data.
    [SerializeField] private DungeonGenerator generator;

    // The script that turns the generated layout into real room prefabs.
    [SerializeField] private DungeonSpawner spawner;

    // The player controller.
    // Used so the run manager can move the player to the start room.
    [SerializeField] private PlayerController player;

    [Header("Start")]

    // If true, a dungeon floor is generated automatically when the scene starts.
    [SerializeField] private bool generateOnStart = true;

    // The grid position where the player starts.
    //
    // Usually this should stay at zero because the start room is generated at (0, 0).
    [SerializeField] private Vector2Int startGridPosition = Vector2Int.zero;

    // If true, the player's rotation is reset when a new floor starts.
    //
    // Quaternion.identity means the player faces world +Z.
    [SerializeField] private bool resetPlayerRotationOnFloorStart = true;

    [Header("Floor Transition")]

    // Delay before generating the next floor after the boss is cleared.
    [SerializeField] private float nextFloorDelay = 1f;

    // Stores the current floor transition coroutine.
    //
    // This prevents multiple floor transition routines from running at the same time.
    private Coroutine floorRoutine;

    private void Awake()
    {
        // Set up singleton reference.
        Instance = this;
    }

    private void Start()
    {
        // Find any missing references before doing anything.
        ResolveReferences();

        // Generate the first dungeon floor automatically if enabled.
        if (generateOnStart)
            GenerateNewFloor();
    }

    // Finds missing scene references automatically.
    //
    // This means you can either assign references manually in the Inspector,
    // or let this script find them at runtime.
    private void ResolveReferences()
    {
        // Find the dungeon generator if one was not assigned.
        if (generator == null)
            generator = FindFirstObjectByType<DungeonGenerator>();

        // Find the dungeon spawner if one was not assigned.
        if (spawner == null)
            spawner = FindFirstObjectByType<DungeonSpawner>();

        // Try to get the player from the PlayerController singleton.
        if (player == null)
            player = PlayerController.Instance;

        // Fallback: find the player in the scene.
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    // Generates a brand new dungeon floor.
    //
    // This is used:
    // - at the start of the game
    // - after the boss room is cleared
    public void GenerateNewFloor()
    {
        // Make sure references are still valid.
        ResolveReferences();

        // Stop if there is no generator.
        if (generator == null)
        {
            Debug.LogError("No DungeonGenerator found.");
            return;
        }

        // Stop if there is no spawner.
        if (spawner == null)
        {
            Debug.LogError("No DungeonSpawner found.");
            return;
        }

        // Generate the dungeon layout data.
        //
        // This creates RoomNodes, but does not spawn visible rooms yet.
        List<RoomNode> dungeon = generator.Generate();

        // Spawn visible room prefabs from the generated RoomNodes.
        //
        // DungeonSpawner also registers the rooms with DungeonManager.
        spawner.BuildDungeon(dungeon);

        // Move the player to the center of the start room.
        MovePlayerToStartRoom();

        // Set the game back to Exploring after the floor has been generated.
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Exploring);

        // Tell DungeonManager that the player has entered the start room.
        //
        // This also marks it as visited.
        // Usually combat will not start here because startRoomStartsCombat is false.
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.EnterRoom(startGridPosition);
    }

    // Starts the next floor transition using the default delay.
    public void GoToNextFloor()
    {
        GoToNextFloor(nextFloorDelay);
    }

    // Starts the next floor transition with a custom delay.
    public void GoToNextFloor(float delay)
    {
        // If a floor transition is already running, stop it first.
        //
        // This prevents generating multiple floors at once.
        if (floorRoutine != null)
            StopCoroutine(floorRoutine);

        floorRoutine = StartCoroutine(GoToNextFloorRoutine(delay));
    }

    // Handles the actual delay before generating the next floor.
    private IEnumerator GoToNextFloorRoutine(float delay)
    {
        // Put the game into Transition state so player movement/combat can be paused.
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Transition);

        // Wait before generating the next floor.
        yield return new WaitForSeconds(delay);

        // Generate the next floor.
        GenerateNewFloor();

        // Clear the coroutine reference because the transition is finished.
        floorRoutine = null;
    }

    // Moves the player to the start room's center point.
    private void MovePlayerToStartRoom()
    {
        // Stop if no player was found.
        if (player == null)
            return;

        // Stop if DungeonManager does not exist.
        //
        // DungeonManager is needed because it knows where the spawned start room is.
        if (DungeonManager.Instance == null)
            return;

        // Get the world position of the start room center.
        Vector3 center = DungeonManager.Instance.GetRoomCenter(startGridPosition);

        // Move the player to the center of the start room.
        player.transform.position = center;

        // Tell the player controller which grid room it is currently in.
        //
        // This keeps player movement logic synced with the dungeon grid.
        player.SetGridPosition(startGridPosition);

        // Optionally reset the player's rotation.
        //
        // This means the player starts each floor facing world +Z.
        if (resetPlayerRotationOnFloorStart)
            player.transform.rotation = Quaternion.identity;
    }
}