using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script manages all enemies currently alive in combat.
//
// It tracks:
// - enemies being added when they spawn
// - enemies being removed when they die
// - when all enemies are dead
// - clearing the player's hand after combat
// - resetting mana after combat
// - moving to the next floor if the current room was the boss room
public class EnemyManager : MonoBehaviour
{
    // Singleton reference so other scripts can call
    public static EnemyManager Instance;

    // List of all enemies currently alive.
    private List<Enemy> enemies = new List<Enemy>();

    // Prevents the combat-end logic from running more than once.
    private Coroutine endCombatCoroutine;

    private void Awake()
    {
        // Set up the singleton instance.
        Instance = this;
    }

    // Called by an Enemy when it spawns/starts.
    //
    // This adds the enemy to the active enemy list.
    public void RegisterEnemy(Enemy enemy)
    {
        // Safety check.
        if (enemy == null)
            return;

        // Only add the enemy if it is not already in the list.
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    // Called by an Enemy when it dies.
    //
    // This removes the enemy from the active enemy list.
    public void UnregisterEnemy(Enemy enemy)
    {
        // Safety check.
        if (enemy == null)
            return;

        // Remove the enemy if it exists in the list.
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);

        // Remove destroyed/null enemy references.
        enemies.RemoveAll(enemyInList => enemyInList == null);

        // If no enemies remain, combat is finished.
        if (enemies.Count == 0)
            OnAllEnemiesDead();
    }

    // Returns the current list of alive enemies.
    //
    // Before returning, it removes any null enemy references.
    // This prevents destroyed enemies from staying in the list.
    public List<Enemy> GetAllEnemies()
    {
        enemies.RemoveAll(enemy => enemy == null);
        return enemies;
    }

    // Called when every enemy in the current combat encounter is dead.
    private void OnAllEnemiesDead()
    {
        // Do not run the end-combat logic multiple times.
        if (endCombatCoroutine != null)
            return;

        // Delay cleanup by one frame.
        //
        // This fixes the bug where:
        // - the last enemy dies
        // - mana resets to 0
        // - the attack script continues
        // - the attack gives the player 1 mana afterward
        endCombatCoroutine = StartCoroutine(EndCombatAfterCurrentFrame());
    }

    private IEnumerator EndCombatAfterCurrentFrame()
    {
        // Wait for the current attack/card/action code to fully finish.
        yield return null;

        // Clean the list again in case anything changed this frame.
        enemies.RemoveAll(enemy => enemy == null);

        // If something spawned another enemy, combat is not actually over.
        if (enemies.Count > 0)
        {
            endCombatCoroutine = null;
            yield break;
        }

        // Clear the player's current hand after combat ends.
        if (HandManager.Instance != null)
            HandManager.Instance.ClearHand();

        // Reset mana after combat ends.
        //
        // Because this now happens one frame later, any mana gained during the killing attack
        // will also be cleared correctly.
        if (ManaManager.Instance != null)
            ManaManager.Instance.ResetMana();

        // Check if the player is currently in the boss room.
        //
        // If they are, clearing all enemies means the boss room is complete,
        // so the game should move to the next floor.
        if (DungeonManager.Instance != null && DungeonManager.Instance.IsCurrentRoomBoss())
        {
            // Use DungeonRunManager to create the next floor.
            if (DungeonRunManager.Instance != null)
            {
                DungeonRunManager.Instance.GoToNextFloor();
                endCombatCoroutine = null;
                yield break;
            }

            // If no DungeonRunManager exists.
            Debug.LogWarning("Boss defeated, but no DungeonRunManager exists to create the next floor.");
        }

        // If this was not the boss room, return to exploration.
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Exploring);

        endCombatCoroutine = null;
    }
}