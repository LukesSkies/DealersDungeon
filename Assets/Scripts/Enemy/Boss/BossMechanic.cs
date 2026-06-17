using UnityEngine;

// Base class for custom boss mechanics.
// Add one derived boss script to each boss prefab.
public abstract class BossMechanic : MonoBehaviour
{
    protected Enemy enemy;

    protected virtual void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    protected virtual void Start()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.SetRole(EnemyRole.Boss);
            enemy.isBoss = true;
        }
    }

    // Called inside Enemy.AttackPlayer before the boss performs its normal attack.
    // Return true if this boss mechanic handled/skipped the attack.
    public virtual bool TryHandleTurnBeforeAttack(Enemy actingEnemy)
    {
        return false;
    }

    // Called after the boss performs its normal attack.
    public virtual void OnAfterAttack(Enemy actingEnemy)
    {
    }

    // Called from Enemy.ProcessEffects during the enemy turn cleanup.
    // This happens before normal status ticks if you add the Enemy.cs change below.
    public virtual void OnEnemyTurnEnd(Enemy actingEnemy)
    {
    }

    // Allows a boss to change incoming damage.
    public virtual int ModifyIncomingDamage(
        Enemy targetEnemy,
        int incomingDamage,
        CardDamageType damageType,
        bool ignoreShield)
    {
        return incomingDamage;
    }
}