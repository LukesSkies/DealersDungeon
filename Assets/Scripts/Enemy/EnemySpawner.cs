using System.Collections.Generic;
using UnityEngine;

// This script handles spawning enemies for combat.
//
// It can spawn enemies in two ways:
// 1. Using preset enemy layouts.
// 2. Spawning enemies in a line facing the player.
//
// DungeonManager currently uses SpawnEnemiesFacingPlayer()
// when combat starts in a room.
public class EnemySpawner : MonoBehaviour
{
    // The enemy prefab that will be spawned.
    //
    // This prefab should have:
    // - Enemy component
    // - collider
    // - any enemy UI/visuals
    public GameObject enemyPrefab;

    // Default enemy count.
    //
    // This is not used by every spawn method,
    // but it can be used as a default value if needed.
    public int enemyCount = 3;

    // Fallback spawn position used by SpawnEnemies()
    // if no matching EnemyLayout is found.
    public Vector3 startPosition;

    [Header("Enemy Layouts")]

    // Preset enemy layouts.
    //
    // Example:
    // A layout with enemyCount = 3 can have 3 assigned positions.
    //
    // SpawnEnemies(3) will try to use that layout.
    public List<EnemyLayout> layouts = new List<EnemyLayout>();

    // Stores enemies spawned by this spawner.
    //
    // This lets the spawner clear old enemies before creating new ones.
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    // Spawns enemies using a preset layout if one exists.
    //
    // This is useful for fixed enemy positions.
    public void SpawnEnemies(int count)
    {
        // Remove any enemies this spawner already created.
        ClearEnemies();

        // Try to find a layout that matches the requested enemy count.
        EnemyLayout layout = layouts.Find(l => l.enemyCount == count);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos;

            // If a matching layout exists and it has a valid position for this enemy,
            // use that layout position.
            if (layout != null && i < layout.positions.Count && layout.positions[i] != null)
            {
                spawnPos = layout.positions[i].position;
            }
            else
            {
                // Fallback:
                // Spawn enemies in a simple centered horizontal line.
                //
                // Example with 3 enemies:
                // enemy 0 = left
                // enemy 1 = center
                // enemy 2 = right
                float spacing = 2.5f;
                float offset = (count - 1) * spacing * 0.5f;

                spawnPos = startPosition + new Vector3(i * spacing - offset, 0, 0);
            }

            // Create the enemy.
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // Store it so it can be cleared later.
            spawnedEnemies.Add(enemy);
        }
    }

    // Spawns enemies in front of the player.
    //
    // The enemies are placed in a line based on the player's forward and right directions.
    //
    // This means enemies appear in front of where the player is looking.
    public void SpawnEnemiesFacingPlayer(Transform player, int count)
    {
        // Remove any enemies this spawner already created.
        ClearEnemies();

        // Player's forward direction.
        Vector3 forward = player.forward;

        // Player's right direction.
        //
        // Used to spread enemies left/right across the player's view.
        Vector3 right = player.right;

        // How far in front of the player enemies appear.
        float distanceFromPlayer = 6f;

        // Space between enemies in the line.
        float spacing = 2f;

        // Center point of the enemy line.
        Vector3 basePosition = player.position + forward * distanceFromPlayer;

        for (int i = 0; i < count; i++)
        {
            // Centers the enemy line around the base position.
            //
            // Example with 3 enemies:
            // i = 0 gives negative offset
            // i = 1 gives center
            // i = 2 gives positive offset
            float offset = (i - (count - 1) / 2f) * spacing;

            // Final spawn position.
            Vector3 spawnPos = basePosition + right * offset;

            // Create the enemy.
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // Make the enemy face the player when spawned.
            enemy.transform.LookAt(player.position);

            // Store it so it can be cleared later.
            spawnedEnemies.Add(enemy);
        }
    }

    // Destroys all enemies spawned by this spawner.
    public void ClearEnemies()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();
    }

    // Returns the list of enemies spawned by this spawner.
    public List<GameObject> GetEnemies()
    {
        return spawnedEnemies;
    }
}