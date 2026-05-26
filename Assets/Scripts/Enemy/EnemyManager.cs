using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private List<Enemy> enemies = new List<Enemy>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);

        if (enemies.Count == 0)
        {
            OnAllEnemiesDead();
        }
    }

    public List<Enemy> GetAllEnemies()
    {
        return enemies;
    }

    private IEnumerator GoToNextFloor()
    {
        GameManager.Instance.SetState(GameState.Transition);

        yield return new WaitForSeconds(1f);

        DungeonGenerator generator = FindFirstObjectByType<DungeonGenerator>();
        DungeonSpawner spawner = FindFirstObjectByType<DungeonSpawner>();

        var newDungeon = generator.Generate();
        spawner.BuildDungeon(newDungeon);

        GameManager.Instance.SetState(GameState.Exploring);
    }

    private void OnAllEnemiesDead()
    {
        if (HandManager.Instance != null)
            HandManager.Instance.ClearHand();

        if (ManaManager.Instance != null)
            ManaManager.Instance.ResetMana();

        GameManager.Instance.SetState(GameState.Exploring);

        if (DungeonManager.Instance.IsCurrentRoomBoss())
        {
            StartCoroutine(GoToNextFloor());
        }
    }
}