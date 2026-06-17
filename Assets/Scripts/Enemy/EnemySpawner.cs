using System;
using System.Collections.Generic;
using UnityEngine;

// One boss spawn rule.
// This chooses one floor for this boss per run: Floor A or Floor B.
// Once the boss is defeated, it will not spawn again for that run.
[Serializable]
public class BossSpawnRule
{
    [Header("Boss")]
    public string bossId;
    public string bossName;
    public GameObject bossPrefab;

    [Header("Possible Floors")]
    [Min(1)] public int floorA = 1;
    [Min(1)] public int floorB = 2;

    [Header("Floor Choice")]
    [Tooltip("Chance this boss chooses Floor A. If this fails, it chooses Floor B.")]
    [Range(0f, 1f)] public float chanceToUseFloorA = 0.5f;

    [Header("Selection")]
    [Tooltip("Only matters if multiple bosses are scheduled for the same floor.")]
    [Min(0.01f)] public float selectionWeight = 1f;

    public string GetSafeId()
    {
        if (!string.IsNullOrWhiteSpace(bossId))
            return bossId;

        if (!string.IsNullOrWhiteSpace(bossName))
            return bossName;

        if (bossPrefab != null)
            return bossPrefab.name;

        return "UnnamedBoss";
    }

    public int ChooseScheduledFloor()
    {
        floorA = Mathf.Max(1, floorA);
        floorB = Mathf.Max(1, floorB);

        if (floorA == floorB)
            return floorA;

        return UnityEngine.Random.value <= chanceToUseFloorA ? floorA : floorB;
    }

    public bool IsValid()
    {
        return bossPrefab != null;
    }
}

// Spawns enemies for combat.
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Fallback Enemy Prefab")]
    [Tooltip("Used if the typed enemy prefabs are not assigned.")]
    public GameObject enemyPrefab;

    [Header("Typed Enemy Prefabs")]
    [SerializeField] private GameObject physicalEnemyPrefab;
    [SerializeField] private GameObject weakPhysicalEnemyPrefab;
    [SerializeField] private GameObject supportShieldCasterPrefab;

    [Header("Default Boss")]
    [Tooltip("Used when no scheduled boss is available for this floor.")]
    [SerializeField] private GameObject bossEnemyPrefab;

    [Header("Boss Spawn Rules")]
    [Tooltip("Each boss chooses either Floor A or Floor B once per run.")]
    [SerializeField] private List<BossSpawnRule> bossSpawnRules = new List<BossSpawnRule>();

    [Header("Fallback Normal Room Enemy Count")]
    [Tooltip("Used only if no DungeonRunManager exists.")]
    [SerializeField] private int minNormalEnemies = 1;

    [Tooltip("Used only if no DungeonRunManager exists.")]
    [SerializeField] private int maxNormalEnemies = 3;

    [Header("Normal Room Enemy Mix")]
    [Range(0f, 1f)]
    [SerializeField] private float supportEnemyChance = 0.25f;

    [Range(0f, 1f)]
    [SerializeField] private float weakEnemyChance = 0.35f;

    [Header("Boss Room")]
    [SerializeField] private bool spawnBossOnly = true;
    [SerializeField] private int bossMinionCount = 2;

    [Header("Fallback Spawn")]
    public int enemyCount = 3;
    public Vector3 startPosition;

    [Header("Enemy Layouts")]
    public List<EnemyLayout> layouts = new List<EnemyLayout>();

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    // Boss run state.
    private readonly Dictionary<string, int> scheduledBossFloors = new Dictionary<string, int>();
    private readonly HashSet<string> defeatedBosses = new HashSet<string>();

    private string currentBossId = "";
    private bool currentBossCameFromRule = false;

    private class BossSpawnCandidate
    {
        public BossSpawnRule rule;
        public float weight;
    }

    private void Awake()
    {
        Instance = this;
    }

    // Resets boss scheduling and defeated bosses.
    // Call this when starting a brand new run.
    public void ResetBossRunProgress()
    {
        scheduledBossFloors.Clear();
        defeatedBosses.Clear();

        currentBossId = "";
        currentBossCameFromRule = false;
    }

    // Called when the current boss room is cleared.
    public void MarkCurrentBossDefeated()
    {
        if (!currentBossCameFromRule)
        {
            currentBossId = "";
            return;
        }

        if (string.IsNullOrWhiteSpace(currentBossId))
            return;

        if (!defeatedBosses.Contains(currentBossId))
            defeatedBosses.Add(currentBossId);

        Debug.Log("Boss defeated and removed from future spawns: " + currentBossId);

        currentBossId = "";
        currentBossCameFromRule = false;
    }

    // Spawns enemies for a dungeon room.
    public void SpawnEnemiesForRoom(Room room, Transform player, int floorNumber)
    {
        ClearEnemies();

        if (room == null)
            return;

        floorNumber = Mathf.Max(1, floorNumber);

        if (room.isBossRoom)
        {
            SpawnBossRoom(room, player, floorNumber);
            return;
        }

        SpawnNormalRoom(room, player, floorNumber);
    }

    // Spawns a normal combat room.
    private void SpawnNormalRoom(Room room, Transform player, int floorNumber)
    {
        int min;
        int max;

        GetEnemyCountRangeForFloor(floorNumber, out min, out max);

        int count = UnityEngine.Random.Range(min, max + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = PickNormalEnemyPrefab(out EnemyRole forcedRole);
            Vector3 position = GetRoomSpawnPosition(room, i, count, player);

            SpawnEnemy(prefab, position, player, floorNumber, forcedRole);
        }
    }

    // Spawns the boss room.
    private void SpawnBossRoom(Room room, Transform player, int floorNumber)
    {
        GameObject bossPrefab = PickBossPrefabForFloor(floorNumber);

        SpawnEnemy(
            bossPrefab,
            GetRoomSpawnPosition(room, 0, 1, player),
            player,
            floorNumber,
            EnemyRole.Boss
        );

        if (spawnBossOnly)
            return;

        for (int i = 0; i < bossMinionCount; i++)
        {
            GameObject prefab = PickNormalEnemyPrefab(out EnemyRole forcedRole);
            Vector3 position = GetRoomSpawnPosition(room, i + 1, bossMinionCount + 1, player);

            SpawnEnemy(prefab, position, player, floorNumber, forcedRole);
        }
    }

    // Chooses a boss prefab based on the current floor and run schedule.
    private GameObject PickBossPrefabForFloor(int floorNumber)
    {
        currentBossId = "";
        currentBossCameFromRule = false;

        List<BossSpawnCandidate> candidates = new List<BossSpawnCandidate>();

        for (int i = 0; i < bossSpawnRules.Count; i++)
        {
            BossSpawnRule rule = bossSpawnRules[i];

            if (rule == null || !rule.IsValid())
                continue;

            string bossId = rule.GetSafeId();

            if (defeatedBosses.Contains(bossId))
                continue;

            if (!scheduledBossFloors.ContainsKey(bossId))
            {
                int chosenFloor = rule.ChooseScheduledFloor();
                scheduledBossFloors.Add(bossId, chosenFloor);

                Debug.Log($"{bossId} scheduled for floor {chosenFloor}.");
            }

            int scheduledFloor = scheduledBossFloors[bossId];

            if (scheduledFloor != floorNumber)
                continue;

            candidates.Add(new BossSpawnCandidate
            {
                rule = rule,
                weight = Mathf.Max(0.01f, rule.selectionWeight)
            });
        }

        if (candidates.Count > 0)
        {
            BossSpawnCandidate picked = PickWeightedBossCandidate(candidates);

            if (picked != null && picked.rule != null && picked.rule.bossPrefab != null)
            {
                currentBossId = picked.rule.GetSafeId();
                currentBossCameFromRule = true;

                Debug.Log($"Spawning scheduled boss: {currentBossId}");

                return picked.rule.bossPrefab;
            }
        }

        currentBossId = "";
        currentBossCameFromRule = false;

        if (bossEnemyPrefab != null)
        {
            Debug.Log("No scheduled boss for this floor. Spawning default boss.");
            return bossEnemyPrefab;
        }

        Debug.LogWarning("No scheduled boss or default boss assigned. Falling back to enemyPrefab.");
        return enemyPrefab;
    }

    // Picks one boss from valid candidates by weight.
    private BossSpawnCandidate PickWeightedBossCandidate(List<BossSpawnCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] == null || candidates[i].rule == null)
                continue;

            totalWeight += Mathf.Max(0.01f, candidates[i].weight);
        }

        if (totalWeight <= 0f)
            return candidates[0];

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float runningTotal = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] == null || candidates[i].rule == null)
                continue;

            runningTotal += Mathf.Max(0.01f, candidates[i].weight);

            if (roll <= runningTotal)
                return candidates[i];
        }

        return candidates[0];
    }

    // Gets enemy count range from DungeonRunManager if available.
    private void GetEnemyCountRangeForFloor(int floorNumber, out int min, out int max)
    {
        if (DungeonRunManager.Instance != null)
        {
            DungeonRunManager.Instance.GetEnemyCountRangeForFloor(floorNumber, out min, out max);
            return;
        }

        min = Mathf.Max(1, minNormalEnemies);
        max = Mathf.Max(min, maxNormalEnemies);
    }

    // Picks a normal enemy prefab by weighted chance.
    private GameObject PickNormalEnemyPrefab(out EnemyRole forcedRole)
    {
        float roll = UnityEngine.Random.value;

        if (supportShieldCasterPrefab != null && roll <= supportEnemyChance)
        {
            forcedRole = EnemyRole.SupportShieldCaster;
            return supportShieldCasterPrefab;
        }

        roll = UnityEngine.Random.value;

        if (weakPhysicalEnemyPrefab != null && roll <= weakEnemyChance)
        {
            forcedRole = EnemyRole.WeakPhysicalAttacker;
            return weakPhysicalEnemyPrefab;
        }

        forcedRole = EnemyRole.PhysicalAttacker;

        if (physicalEnemyPrefab != null)
            return physicalEnemyPrefab;

        if (enemyPrefab != null)
            return enemyPrefab;

        if (supportShieldCasterPrefab != null)
        {
            forcedRole = EnemyRole.SupportShieldCaster;
            return supportShieldCasterPrefab;
        }

        if (weakPhysicalEnemyPrefab != null)
        {
            forcedRole = EnemyRole.WeakPhysicalAttacker;
            return weakPhysicalEnemyPrefab;
        }

        return null;
    }

    // Spawns one enemy.
    private GameObject SpawnEnemy(GameObject prefab, Vector3 position, Transform player, int floorNumber, EnemyRole forcedRole)
    {
        if (prefab == null)
        {
            Debug.LogError("EnemySpawner has no valid enemy prefab assigned.");
            return null;
        }

        GameObject enemyObject = Instantiate(prefab, position, Quaternion.identity);

        if (player != null)
        {
            Vector3 lookPosition = player.position;
            lookPosition.y = enemyObject.transform.position.y;
            enemyObject.transform.LookAt(lookPosition);
        }

        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.SetRole(forcedRole);

            int healthBonus = DungeonRunManager.Instance == null
                ? GetFallbackHealthBonus(floorNumber)
                : DungeonRunManager.Instance.GetEnemyHealthBonusForFloor(floorNumber);

            int damageBonus = DungeonRunManager.Instance == null
                ? GetFallbackDamageBonus(floorNumber)
                : DungeonRunManager.Instance.GetEnemyDamageBonusForFloor(floorNumber);

            enemy.ApplyFloorScaling(floorNumber, healthBonus, damageBonus);
        }

        spawnedEnemies.Add(enemyObject);

        return enemyObject;
    }

    // Gets a spawn position using the room's spawn points first.
    private Vector3 GetRoomSpawnPosition(Room room, int index, int totalCount, Transform player)
    {
        if (room != null && room.enemySpawnPoints != null && room.enemySpawnPoints.Length > 0)
            return room.GetEnemySpawnPosition(index);

        if (player != null)
        {
            Vector3 forward = player.forward;
            Vector3 right = player.right;

            float distanceFromPlayer = 6f;
            float spacing = 2f;
            float offset = (index - (totalCount - 1) / 2f) * spacing;

            return player.position + forward * distanceFromPlayer + right * offset;
        }

        float fallbackSpacing = 2.5f;
        float fallbackOffset = (totalCount - 1) * fallbackSpacing * 0.5f;

        return startPosition + new Vector3(index * fallbackSpacing - fallbackOffset, 0f, 0f);
    }

    // Old compatibility method.
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
                float spacing = 2.5f;
                float offset = (count - 1) * spacing * 0.5f;

                spawnPos = startPosition + new Vector3(i * spacing - offset, 0f, 0f);
            }

            GameObject prefab = PickNormalEnemyPrefab(out EnemyRole forcedRole);
            SpawnEnemy(prefab, spawnPos, null, GetCurrentFloor(), forcedRole);
        }
    }

    // Old compatibility method.
    public void SpawnEnemiesFacingPlayer(Transform player, int count)
    {
        SpawnEnemiesFacingPlayer(player, count, GetCurrentFloor());
    }

    // Old compatibility method with floor support.
    public void SpawnEnemiesFacingPlayer(Transform player, int count, int floorNumber)
    {
        ClearEnemies();

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetRoomSpawnPosition(null, i, count, player);
            GameObject prefab = PickNormalEnemyPrefab(out EnemyRole forcedRole);

            SpawnEnemy(prefab, spawnPos, player, floorNumber, forcedRole);
        }
    }

    // Removes all spawned enemies.
    public void ClearEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.ClearEnemyList();
    }

    // Returns all spawned enemies.
    public List<GameObject> GetEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        return spawnedEnemies;
    }

    // Gets current floor safely.
    private int GetCurrentFloor()
    {
        if (DungeonRunManager.Instance == null)
            return 1;

        return DungeonRunManager.Instance.CurrentFloor;
    }

    // Fallback formula if no DungeonRunManager exists.
    private int GetFallbackHealthBonus(int floorNumber)
    {
        floorNumber = Mathf.Max(1, floorNumber);
        return (floorNumber - 1) * 2;
    }

    // Fallback formula if no DungeonRunManager exists.
    private int GetFallbackDamageBonus(int floorNumber)
    {
        floorNumber = Mathf.Max(1, floorNumber);
        return ((floorNumber - 1) / 2) * 2;
    }

    [ContextMenu("Test Spawn Boss")]
    public void TestSpawnBoss()
    {
        ClearEnemies();

        Transform player = PlayerController.Instance == null
            ? null
            : PlayerController.Instance.transform;

        GameObject bossPrefab = PickBossPrefabForFloor(GetCurrentFloor());

        Vector3 spawnPosition = startPosition;

        if (player != null)
            spawnPosition = player.position + player.forward * 6f;

        SpawnEnemy(
            bossPrefab,
            spawnPosition,
            player,
            GetCurrentFloor(),
            EnemyRole.Boss
        );
    }

    [ContextMenu("Reset Boss Run Progress")]
    public void DebugResetBossRunProgress()
    {
        ResetBossRunProgress();
    }

    private void OnValidate()
    {
        if (minNormalEnemies < 1)
            minNormalEnemies = 1;

        if (maxNormalEnemies < minNormalEnemies)
            maxNormalEnemies = minNormalEnemies;

        if (enemyCount < 1)
            enemyCount = 1;

        if (bossMinionCount < 0)
            bossMinionCount = 0;

        if (bossSpawnRules != null)
        {
            for (int i = 0; i < bossSpawnRules.Count; i++)
            {
                BossSpawnRule rule = bossSpawnRules[i];

                if (rule == null)
                    continue;

                if (rule.floorA < 1)
                    rule.floorA = 1;

                if (rule.floorB < 1)
                    rule.floorB = 1;

                if (rule.selectionWeight <= 0f)
                    rule.selectionWeight = 0.01f;
            }
        }
    }
}