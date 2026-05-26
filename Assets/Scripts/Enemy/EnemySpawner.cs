using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 3;

    public Vector3 startPosition;

    [Header("Enemy Layouts")]
    public List<EnemyLayout> layouts = new List<EnemyLayout>();

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    public void SpawnEnemies(int count)
    {
        ClearEnemies();

        EnemyLayout layout = layouts.Find(l => l.enemyCount == count);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos;

            if (layout != null && i < layout.positions.Count && layout.positions[i] != null)
            {
                spawnPos = layout.positions[i].position;
            }
            else
            {
                // Fallback (simple centered line)
                float spacing = 2.5f;
                float offset = (count - 1) * spacing * 0.5f;
                spawnPos = startPosition + new Vector3(i * spacing - offset, 0, 0);
            }

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            spawnedEnemies.Add(enemy);
        }
    }

    public void SpawnEnemiesFacingPlayer(Transform player, int count)
    {
        ClearEnemies();

        Vector3 forward = player.forward;
        Vector3 right = player.right;

        float distanceFromPlayer = 6f;
        float spacing = 2f;

        Vector3 basePosition = player.position + forward * distanceFromPlayer;

        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) / 2f) * spacing;

            Vector3 spawnPos = basePosition + right * offset;

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            enemy.transform.LookAt(player.position);

            spawnedEnemies.Add(enemy);
        }
    }

    public void ClearEnemies()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();
    }

    public List<GameObject> GetEnemies()
    {
        return spawnedEnemies;
    }
}