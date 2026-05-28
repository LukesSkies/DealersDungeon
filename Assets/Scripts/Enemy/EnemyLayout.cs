using System.Collections.Generic;
using UnityEngine;

// This class stores a preset enemy layout.
//
// It lets you set specific spawn positions for a certain number of enemies.
//
// Example:
// enemyCount = 3
// positions = three Transform points in the scene
//
// Then EnemySpawner.SpawnEnemies(3) can use those positions.
[System.Serializable]
public class EnemyLayout
{
    // The number of enemies this layout is for.
    //
    // Example:
    // enemyCount = 2 means this layout is used when spawning 2 enemies.
    public int enemyCount;

    // The spawn positions for this layout.
    //
    // The number of positions should usually match enemyCount.
    public List<Transform> positions;
}