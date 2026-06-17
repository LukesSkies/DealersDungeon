using System.Collections.Generic;
using UnityEngine;

// Stores preset enemy spawn positions.
[System.Serializable]
public class EnemyLayout
{
    // Number of enemies this layout is for.
    public int enemyCount;

    // Spawn positions for this layout.
    public List<Transform> positions;
}