using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Tracks enemies during combat.
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private readonly List<Enemy> enemies = new List<Enemy>();

    private Coroutine endCombatCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    // Adds an enemy to the list.
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemies.RemoveAll(enemyInList => enemyInList == null);

        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    // Removes an enemy from the list.
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (enemies.Contains(enemy))
            enemies.Remove(enemy);

        enemies.RemoveAll(enemyInList => enemyInList == null);

        if (enemies.Count == 0)
            OnAllEnemiesDead();
    }

    // Clears the list without ending combat.
    // Used when changing floor or clearing spawned enemies.
    public void ClearEnemyList()
    {
        enemies.Clear();

        if (endCombatCoroutine != null)
        {
            StopCoroutine(endCombatCoroutine);
            endCombatCoroutine = null;
        }
    }

    // Gets all current enemies.
    public List<Enemy> GetAllEnemies()
    {
        enemies.RemoveAll(enemy => enemy == null);
        return enemies;
    }

    // Makes all enemies take their action.
    // Call this from your combat/turn flow when the enemy turn starts.
    public void EnemiesTakeTurn()
    {
        enemies.RemoveAll(enemy => enemy == null);

        List<Enemy> enemiesThisTurn = new List<Enemy>(enemies);

        for (int i = 0; i < enemiesThisTurn.Count; i++)
        {
            Enemy enemy = enemiesThisTurn[i];

            if (enemy == null)
                continue;

            if (enemy.GetCurrentHP() <= 0)
                continue;

            enemy.AttackPlayer();
        }

        ProcessEnemyEffects();
    }

    // Processes status effects at the end of enemy turn.
    public void ProcessEnemyEffects()
    {
        enemies.RemoveAll(enemy => enemy == null);

        List<Enemy> enemiesToProcess = new List<Enemy>(enemies);

        for (int i = 0; i < enemiesToProcess.Count; i++)
        {
            Enemy enemy = enemiesToProcess[i];

            if (enemy == null)
                continue;

            if (enemy.GetCurrentHP() <= 0)
                continue;

            enemy.ProcessEffects();
        }
    }

    // Called when combat enemies are gone.
    private void OnAllEnemiesDead()
    {
        if (endCombatCoroutine != null)
            return;

        endCombatCoroutine = StartCoroutine(EndCombatAfterCurrentFrame());
    }

    // Ends combat after the current frame.
    private IEnumerator EndCombatAfterCurrentFrame()
    {
        yield return null;

        enemies.RemoveAll(enemy => enemy == null);

        if (enemies.Count > 0)
        {
            endCombatCoroutine = null;
            yield break;
        }

        if (HandManager.Instance != null)
            HandManager.Instance.ClearHand();

        if (ManaManager.Instance != null)
            ManaManager.Instance.ResetMana();

        if (PlayerShield.Instance != null)
            PlayerShield.Instance.ClearShield();

        if (DungeonManager.Instance != null && DungeonManager.Instance.IsCurrentRoomBoss())
        {
            if (EnemySpawner.Instance != null)
                EnemySpawner.Instance.MarkCurrentBossDefeated();

            if (DungeonRunManager.Instance != null)
            {
                DungeonRunManager.Instance.GoToNextFloor();
                endCombatCoroutine = null;
                yield break;
            }

            Debug.LogWarning("Boss defeated, but no DungeonRunManager exists to create the next floor.");
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Exploring);

        endCombatCoroutine = null;
    }
}