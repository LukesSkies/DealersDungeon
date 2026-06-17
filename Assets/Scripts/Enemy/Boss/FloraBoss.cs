using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Boss mechanic for Flora.
// Core mechanic: Living Bloom Minions.
// Flowers attack normally as enemies, but only heal Flora after surviving one Flora turn.
public class FloraBoss : BossMechanic
{
    [Header("Flower Minions")]
    [SerializeField] private GameObject flowerMinionPrefab;

    [Tooltip("Optional spawn points for Flower Minions. If empty, flowers spawn around Flora.")]
    [SerializeField] private Transform[] flowerSpawnPoints;

    [SerializeField] private int maxFlowerMinions = 4;
    [SerializeField] private int flowerMinionDamage = 1;
    [SerializeField] private float fallbackSpawnRadius = 2f;

    [Header("Bloom Healing")]
    [SerializeField] private int healWithOneMatureMinion = 2;
    [SerializeField] private int healWithTwoMatureMinions = 3;
    [SerializeField] private int healWithThreeMatureMinions = 4;
    [SerializeField] private int healWithFourMatureMinions = 5;

    [Header("Burn Weakness")]
    [SerializeField] private int extraBurnDamagePerTurn = 2;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text bossStateText;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private readonly List<FlowerBloom> activeFlowers = new List<FlowerBloom>();

    private int nextSpawnPointIndex = 0;
    private int flowerIdCounter = 0;

    private class FlowerBloom
    {
        public int id;
        public Enemy flowerEnemy;

        // A flower starts at 1 when summoned.
        // It becomes 0 at the end of that same Flora turn.
        // It only heals Flora on later Flora turns while this is 0.
        public int floraTurnsUntilHealing = 1;
    }

    protected override void Start()
    {
        base.Start();

        Log("FloraBoss started.");

        if (enemy == null)
            Log("ERROR: FloraBoss has no Enemy component.");

        if (flowerMinionPrefab == null)
            Log("ERROR: Flower Minion Prefab is not assigned.");

        UpdateVisuals();
    }

    // Flora attacks through Enemy.AttackPlayer().
    // After Flora attacks, she tries to summon one flower.
    public override void OnAfterAttack(Enemy actingEnemy)
    {
        Log("OnAfterAttack called. Flora will try to summon one Flower Minion.");

        SummonFlower();
        UpdateVisuals();
    }

    // Called at the end of Flora's enemy turn through Enemy.ProcessEffects().
    public override void OnEnemyTurnEnd(Enemy actingEnemy)
    {
        if (actingEnemy == null)
        {
            Log("ERROR: OnEnemyTurnEnd called with null actingEnemy.");
            return;
        }

        if (actingEnemy.GetCurrentHP() <= 0)
        {
            Log("Flora is dead. Skipping bloom healing.");
            return;
        }

        CleanupFlowerList();

        Log(
            "OnEnemyTurnEnd called. Flora HP before: " +
            actingEnemy.GetCurrentHP() + "/" + actingEnemy.maxHP
        );

        ApplyBurnWeaknessBonus(actingEnemy);

        if (actingEnemy == null || actingEnemy.GetCurrentHP() <= 0)
        {
            Log("Flora died from Burn weakness bonus before bloom healing.");
            return;
        }

        HealFromMatureLivingFlowers(actingEnemy);

        AgeNewFlowersAfterHealing();

        Log(
            "OnEnemyTurnEnd finished. Flora HP after: " +
            actingEnemy.GetCurrentHP() + "/" + actingEnemy.maxHP
        );

        UpdateVisuals();
    }

    private void SummonFlower()
    {
        CleanupFlowerList();

        int livingFlowerCount = GetLivingFlowerCount();

        if (flowerMinionPrefab == null)
        {
            Log("ERROR: Cannot summon flower because Flower Minion Prefab is missing.");
            return;
        }

        if (livingFlowerCount >= maxFlowerMinions)
        {
            Log(
                "Flora tried to summon, but living flower count is at max: " +
                livingFlowerCount + "/" + maxFlowerMinions
            );

            return;
        }

        Vector3 spawnPosition = GetFlowerSpawnPosition();
        Quaternion spawnRotation = Quaternion.identity;

        GameObject flowerObject = Instantiate(flowerMinionPrefab, spawnPosition, spawnRotation);

        if (flowerObject == null)
        {
            Log("ERROR: Instantiate returned null for Flower Minion.");
            return;
        }

        FacePlayer(flowerObject.transform);

        Enemy flowerEnemy = flowerObject.GetComponent<Enemy>();

        if (flowerEnemy == null)
        {
            Log("ERROR: Flower Minion Prefab has no Enemy component. Destroying spawned flower object.");
            Destroy(flowerObject);
            return;
        }

        flowerEnemy.SetRole(EnemyRole.WeakPhysicalAttacker);
        flowerEnemy.isBoss = false;
        flowerEnemy.attackDamage = Mathf.Max(0, flowerMinionDamage);

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(flowerEnemy);
            Log("Flower registered with EnemyManager.");
        }
        else
        {
            Log("WARNING: EnemyManager.Instance is null. Flower could not be manually registered.");
        }

        flowerIdCounter++;

        FlowerBloom bloom = new FlowerBloom
        {
            id = flowerIdCounter,
            flowerEnemy = flowerEnemy,
            floraTurnsUntilHealing = 1
        };

        activeFlowers.Add(bloom);

        Log(
            "Summoned Flower #" + bloom.id +
            ". It will NOT heal Flora this turn. " +
            "TurnsUntilHealing: " + bloom.floraTurnsUntilHealing +
            ", Damage: " + flowerEnemy.attackDamage +
            ", CurrentHP right after spawn: " + flowerEnemy.GetCurrentHP() +
            ", MaxHP: " + flowerEnemy.maxHP
        );
    }

    private void HealFromMatureLivingFlowers(Enemy floraEnemy)
    {
        CleanupFlowerList();

        int matureLivingFlowerCount = GetMatureLivingFlowerCount();

        if (matureLivingFlowerCount <= 0)
        {
            Log("No mature living Flower Minions. Flora does not heal this turn.");
            return;
        }

        int healAmount = GetHealAmountForMatureFlowers(matureLivingFlowerCount);

        int hpBefore = floraEnemy.GetCurrentHP();

        floraEnemy.Heal(healAmount);

        int hpAfter = floraEnemy.GetCurrentHP();

        Log(
            "Flora healed from mature living flowers. " +
            "Mature Flowers: " + matureLivingFlowerCount +
            ", Heal Amount: " + healAmount +
            ", Flora HP: " + hpBefore + " -> " + hpAfter + "/" + floraEnemy.maxHP
        );

        if (hpBefore == hpAfter && hpAfter >= floraEnemy.maxHP)
            Log("NOTE: Flora healing triggered, but Flora was already at max HP.");
    }

    private void AgeNewFlowersAfterHealing()
    {
        for (int i = 0; i < activeFlowers.Count; i++)
        {
            FlowerBloom bloom = activeFlowers[i];

            if (bloom == null)
                continue;

            if (bloom.flowerEnemy == null)
                continue;

            if (bloom.flowerEnemy.GetCurrentHP() <= 0)
                continue;

            if (bloom.floraTurnsUntilHealing > 0)
            {
                bloom.floraTurnsUntilHealing--;

                Log(
                    "Flower #" + bloom.id +
                    " aged after Flora healing. TurnsUntilHealing is now " +
                    bloom.floraTurnsUntilHealing + "."
                );
            }
        }
    }

    private int GetHealAmountForMatureFlowers(int matureLivingFlowerCount)
    {
        if (matureLivingFlowerCount <= 0)
            return 0;

        if (matureLivingFlowerCount == 1)
            return healWithOneMatureMinion;

        if (matureLivingFlowerCount == 2)
            return healWithTwoMatureMinions;

        if (matureLivingFlowerCount == 3)
            return healWithThreeMatureMinions;

        return healWithFourMatureMinions;
    }

    private int GetLivingFlowerCount()
    {
        int count = 0;

        for (int i = 0; i < activeFlowers.Count; i++)
        {
            FlowerBloom bloom = activeFlowers[i];

            if (bloom == null)
                continue;

            if (bloom.flowerEnemy == null)
                continue;

            if (bloom.flowerEnemy.GetCurrentHP() <= 0)
                continue;

            count++;
        }

        return count;
    }

    private int GetMatureLivingFlowerCount()
    {
        int count = 0;

        for (int i = 0; i < activeFlowers.Count; i++)
        {
            FlowerBloom bloom = activeFlowers[i];

            if (bloom == null)
                continue;

            if (bloom.flowerEnemy == null)
                continue;

            if (bloom.flowerEnemy.GetCurrentHP() <= 0)
                continue;

            if (bloom.floraTurnsUntilHealing > 0)
                continue;

            count++;
        }

        return count;
    }

    private void ApplyBurnWeaknessBonus(Enemy floraEnemy)
    {
        if (floraEnemy == null)
            return;

        if (extraBurnDamagePerTurn <= 0)
            return;

        if (!floraEnemy.HasEffect(EffectType.Burn))
        {
            Log("Flora does not have Burn. No extra Burn weakness damage.");
            return;
        }

        int hpBefore = floraEnemy.GetCurrentHP();

        floraEnemy.TakeDamage(extraBurnDamagePerTurn, CardDamageType.True, true);

        int hpAfter = floraEnemy.GetCurrentHP();

        Log(
            "Flora Burn weakness triggered. Extra damage: " + extraBurnDamagePerTurn +
            ". Flora HP: " + hpBefore + " -> " + hpAfter + "/" + floraEnemy.maxHP
        );
    }

    private Vector3 GetFlowerSpawnPosition()
    {
        if (flowerSpawnPoints != null && flowerSpawnPoints.Length > 0)
        {
            for (int i = 0; i < flowerSpawnPoints.Length; i++)
            {
                int index = nextSpawnPointIndex % flowerSpawnPoints.Length;
                nextSpawnPointIndex++;

                if (flowerSpawnPoints[index] != null)
                {
                    Log("Using Flower Spawn Point index: " + index);
                    return flowerSpawnPoints[index].position;
                }
            }
        }

        float angle = activeFlowers.Count * 90f;
        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians),
            0f,
            Mathf.Sin(radians)
        ) * fallbackSpawnRadius;

        Vector3 fallbackPosition = transform.position + offset;

        Log("Using fallback flower spawn position: " + fallbackPosition);

        return fallbackPosition;
    }

    private void FacePlayer(Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        if (PlayerController.Instance == null)
            return;

        Vector3 lookPosition = PlayerController.Instance.transform.position;
        lookPosition.y = targetTransform.position.y;

        targetTransform.LookAt(lookPosition);
    }

    private void CleanupFlowerList()
    {
        for (int i = activeFlowers.Count - 1; i >= 0; i--)
        {
            FlowerBloom bloom = activeFlowers[i];

            if (bloom == null)
            {
                activeFlowers.RemoveAt(i);
                continue;
            }

            if (bloom.flowerEnemy == null)
            {
                activeFlowers.RemoveAt(i);
                continue;
            }
        }
    }

    private void UpdateVisuals()
    {
        CleanupFlowerList();

        if (bossStateText != null)
        {
            int livingFlowerCount = GetLivingFlowerCount();
            int matureFlowerCount = GetMatureLivingFlowerCount();
            int healAmount = GetHealAmountForMatureFlowers(matureFlowerCount);

            if (matureFlowerCount > 0)
            {
                bossStateText.text =
                    "Blooms: " + livingFlowerCount +
                    " | Healing: " + matureFlowerCount +
                    " | Heal: " + healAmount;
            }
            else
            {
                bossStateText.text = "Blooms: " + livingFlowerCount + " | Heal: 0";
            }
        }
    }

    [ContextMenu("Debug Print Flora State")]
    public void DebugPrintFloraState()
    {
        Debug.Log("========== FLORA DEBUG STATE ==========");
        Debug.Log("Flora Object: " + name);
        Debug.Log("Enemy Component: " + (enemy != null ? "Found" : "Missing"));

        if (enemy != null)
            Debug.Log("Flora HP: " + enemy.GetCurrentHP() + "/" + enemy.maxHP);

        Debug.Log("Flower Prefab Assigned: " + (flowerMinionPrefab != null));
        Debug.Log("Tracked Flowers: " + activeFlowers.Count);
        Debug.Log("Living Flowers: " + GetLivingFlowerCount());
        Debug.Log("Mature Living Flowers: " + GetMatureLivingFlowerCount());
        Debug.Log("Current Heal Amount: " + GetHealAmountForMatureFlowers(GetMatureLivingFlowerCount()));

        for (int i = 0; i < activeFlowers.Count; i++)
        {
            FlowerBloom bloom = activeFlowers[i];

            if (bloom == null)
            {
                Debug.Log("Flower entry " + i + ": null");
                continue;
            }

            if (bloom.flowerEnemy == null)
            {
                Debug.Log("Flower #" + bloom.id + ": enemy null/destroyed");
                continue;
            }

            Debug.Log(
                "Flower #" + bloom.id +
                " HP: " + bloom.flowerEnemy.GetCurrentHP() + "/" + bloom.flowerEnemy.maxHP +
                ", Damage: " + bloom.flowerEnemy.attackDamage +
                ", TurnsUntilHealing: " + bloom.floraTurnsUntilHealing +
                ", Mature: " + (bloom.floraTurnsUntilHealing <= 0) +
                ", Active: " + bloom.flowerEnemy.gameObject.activeInHierarchy +
                ", Enabled: " + bloom.flowerEnemy.enabled
            );
        }

        Debug.Log("=======================================");
    }

    // If Flora dies while Flower Minions are still alive,
    // destroy the flowers so EnemyManager sees the boss room as cleared.
    private void OnDestroy()
    {
        Log("FloraBoss OnDestroy called. Destroying tracked Flower Minions so boss room can end.");

        for (int i = activeFlowers.Count - 1; i >= 0; i--)
        {
            FlowerBloom bloom = activeFlowers[i];

            if (bloom == null)
                continue;

            if (bloom.flowerEnemy == null)
                continue;

            Enemy flowerEnemy = bloom.flowerEnemy;

            if (EnemyManager.Instance != null)
                EnemyManager.Instance.UnregisterEnemy(flowerEnemy);

            Destroy(flowerEnemy.gameObject);
        }

        activeFlowers.Clear();
    }

    private void Log(string message)
    {
        if (!debugLogging)
            return;

        Debug.Log("[FloraBoss] " + message);
    }
}