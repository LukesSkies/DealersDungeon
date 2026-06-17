using System.Collections.Generic;
using UnityEngine;

// Controls status effects on one enemy.
public class EnemyStatusController : MonoBehaviour
{
    [SerializeField] private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    public IReadOnlyList<ActiveEffect> ActiveEffects => activeEffects;

    public static EnemyStatusController Get(Enemy enemy)
    {
        if (enemy == null)
            return null;

        return enemy.GetComponent<EnemyStatusController>();
    }

    public static EnemyStatusController GetOrAdd(Enemy enemy)
    {
        if (enemy == null)
            return null;

        EnemyStatusController controller = enemy.GetComponent<EnemyStatusController>();

        if (controller == null)
            controller = enemy.gameObject.AddComponent<EnemyStatusController>();

        return controller;
    }

    public void ApplyEffect(Enemy enemy, EffectType type, int value, int secondaryValue, int duration, CardDamageType damageType, bool removeWhenDamaged)
    {
        if (type == EffectType.None)
            return;

        if (!IsEnemyStatusEffect(type))
            return;

        ActiveEffect existing = GetEffect(type);

        if (existing != null)
        {
            existing.value = Mathf.Max(existing.value, value);
            existing.secondaryValue = Mathf.Max(existing.secondaryValue, secondaryValue);
            existing.duration = Mathf.Max(existing.duration, duration);
            existing.stacks++;
            existing.damageType = damageType;
            existing.removeWhenDamaged = existing.removeWhenDamaged || removeWhenDamaged;
        }
        else
        {
            activeEffects.Add(new ActiveEffect(type, value, secondaryValue, duration, damageType, removeWhenDamaged));
        }
    }

    // Compatibility overload for older scripts that still build CardEffect runtime data.
    public void ApplyEffect(CardEffect effect, float sourceCardManaCost)
    {
        if (effect == null)
            return;

        ApplyEffect(
            GetComponent<Enemy>(),
            effect.effectType,
            effect.value,
            effect.secondaryValue,
            effect.duration,
            effect.damageType,
            effect.removeWhenDamaged
        );
    }

    public bool HasEffect(EffectType type)
    {
        return activeEffects.Exists(e => e != null && e.type == type && e.duration != 0);
    }

    public ActiveEffect GetEffect(EffectType type)
    {
        return activeEffects.Find(e => e != null && e.type == type && e.duration != 0);
    }

    public int GetEffectValue(EffectType type)
    {
        ActiveEffect effect = GetEffect(type);
        return effect == null ? 0 : effect.value;
    }

    public bool HasAnyDOT()
    {
        return HasEffect(EffectType.Poison) ||
               HasEffect(EffectType.Burn) ||
               HasEffect(EffectType.Bleed) ||
               HasEffect(EffectType.Leech) ||
               HasEffect(EffectType.Volatile) ||
               HasEffect(EffectType.Wildfire);
    }

    public void RemoveEffect(EffectType type)
    {
        activeEffects.RemoveAll(e => e.type == type);
    }

    public void RemoveSomeNegativeEffects(int count)
    {
        if (count <= 0)
            return;

        for (int i = activeEffects.Count - 1; i >= 0 && count > 0; i--)
        {
            if (IsNegativeEffect(activeEffects[i].type))
            {
                activeEffects.RemoveAt(i);
                count--;
            }
        }
    }

    public void RemoveAllNegativeEffects()
    {
        activeEffects.RemoveAll(e => IsNegativeEffect(e.type));
    }

    public void RemoveAllDOTs()
    {
        activeEffects.RemoveAll(e => IsDOT(e.type));
    }

    public int ConsumeDOTsForDamage(int damagePerStack)
    {
        int damage = 0;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (IsDOT(activeEffects[i].type))
            {
                damage += Mathf.Max(1, activeEffects[i].value) *
                          Mathf.Max(1, activeEffects[i].stacks) *
                          Mathf.Max(1, damagePerStack);

                activeEffects.RemoveAt(i);
            }
        }

        return damage;
    }

    public void AmplifyDOTs(int amount)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (IsDOT(activeEffects[i].type))
                activeEffects[i].value += Mathf.Max(1, amount);
        }
    }

    public int ModifyIncomingDamage(Enemy enemy, int incomingDamage, CardDamageType damageType, bool trueDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        if (trueDamage || damageType == CardDamageType.True)
            return incomingDamage;

        float multiplier = 1f;

        if (HasEffect(EffectType.Petrify))
            multiplier *= 0.5f;

        if (HasEffect(EffectType.Curse))
            multiplier *= 1.2f;

        if (HasEffect(EffectType.Softened))
            multiplier *= 1.25f;

        if (HasEffect(EffectType.Vulnerable))
            multiplier *= 1.3f;

        if (HasEffect(EffectType.Bleed) && damageType == CardDamageType.Physical)
            multiplier *= 1.1f;

        if (HasEffect(EffectType.Cripple) && enemy != null && enemy.GetCurrentHP() <= enemy.maxHP / 2)
            multiplier *= 1.4f;

        ActiveEffect marked = GetEffect(EffectType.Marked);

        if (marked != null && damageType == CardDamageType.Physical)
        {
            multiplier *= 1.5f;
            RemoveEffect(EffectType.Marked);
        }

        return Mathf.Max(0, Mathf.RoundToInt(incomingDamage * multiplier));
    }

    public bool TryHandlePreAttack(Enemy enemy)
    {
        if (enemy == null)
            return true;

        if (HasEffect(EffectType.Stun) || HasEffect(EffectType.Sleep) || HasEffect(EffectType.Petrify))
            return true;

        ActiveEffect dazed = GetEffect(EffectType.Dazed);

        if (dazed != null)
        {
            float loseTurnChance = Mathf.Clamp01(Mathf.Max(1, dazed.value) / 100f);

            if (Random.value <= loseTurnChance)
                return true;
        }

        ActiveEffect blind = GetEffect(EffectType.Blind);

        if (blind != null)
        {
            float missChance = Mathf.Clamp01(Mathf.Max(1, blind.value) / 100f);

            if (Random.value <= missChance)
                return true;
        }

        ActiveEffect charmed = GetEffect(EffectType.Charmed);

        if (charmed != null)
        {
            float charmChance = Mathf.Clamp01(Mathf.Max(1, charmed.secondaryValue) / 100f);

            if (charmed.secondaryValue <= 0)
                charmChance = 0.5f;

            if (Random.value <= charmChance)
            {
                Enemy otherEnemy = PickRandomOtherEnemy(enemy);
                int charmDamage = Mathf.Max(1, charmed.value);

                if (otherEnemy != null)
                    otherEnemy.TakeDamage(charmDamage, CardDamageType.Physical, false);
                else
                    enemy.TakeDamage(charmDamage, CardDamageType.Physical, false);

                return true;
            }
        }

        return false;
    }

    public float GetOutgoingDamageMultiplier()
    {
        float multiplier = 1f;

        if (HasEffect(EffectType.Curse))
            multiplier *= 0.8f;

        if (HasEffect(EffectType.Weakened))
            multiplier *= 0.7f;

        return Mathf.Max(0f, multiplier);
    }

    public void NotifyDamaged(Enemy enemy, int damageTaken)
    {
        if (damageTaken <= 0)
            return;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].removeWhenDamaged)
                activeEffects.RemoveAt(i);
        }
    }

    public void ProcessTurnEnd(Enemy enemy)
    {
        if (enemy == null)
            return;

        bool hadVolatile = HasEffect(EffectType.Volatile);

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];
            bool removeEffect = false;

            switch (effect.type)
            {
                case EffectType.Poison:
                case EffectType.Bleed:
                    enemy.TakeDamage(Mathf.Max(1, effect.value) * Mathf.Max(1, effect.stacks), effect.damageType, false);
                    effect.duration--;
                    break;

                case EffectType.Burn:
                    enemy.TakeDamage(Mathf.Max(1, effect.value) * Mathf.Max(1, effect.stacks), effect.damageType, false);
                    effect.duration--;

                    if (effect.duration <= 0)
                    {
                        float continueChance = effect.secondaryValue > 0 ? effect.secondaryValue / 100f : 0.5f;

                        if (enemy.isBoss)
                            continueChance *= 0.5f;

                        if (Random.value <= Mathf.Clamp01(continueChance))
                            effect.duration = 1;
                        else
                            removeEffect = true;
                    }
                    break;

                case EffectType.Leech:
                    int leechDamage = Mathf.Max(1, effect.value) * Mathf.Max(1, effect.stacks);
                    enemy.TakeDamage(leechDamage, effect.damageType, false);

                    if (PlayerHealth.Instance != null)
                        PlayerHealth.Instance.Heal(Mathf.Max(1, leechDamage / 2));

                    effect.duration--;
                    break;

                case EffectType.Volatile:
                    enemy.TakeDamage(Mathf.Max(1, effect.value) * Mathf.Max(1, effect.stacks), effect.damageType, false);
                    effect.duration--;
                    break;

                case EffectType.Wildfire:
                    enemy.TakeDamage(Mathf.Max(1, effect.value), effect.damageType, false);
                    TrySpreadStatus(enemy, EffectType.Burn, effect.value, 50, 1);
                    effect.duration--;
                    break;

                case EffectType.Doomed:
                    effect.duration--;

                    if (effect.duration <= 0)
                    {
                        int burstDamage = Mathf.Max(1, effect.secondaryValue > 0 ? effect.secondaryValue : effect.value);

                        if (enemy.isBoss)
                            burstDamage = Mathf.CeilToInt(burstDamage * 0.5f);

                        enemy.TakeDamage(burstDamage, CardDamageType.Magic, true);
                        removeEffect = true;
                    }
                    break;

                default:
                    if (effect.duration > 0)
                        effect.duration--;
                    break;
            }

            if (removeEffect || effect.duration <= 0)
                activeEffects.RemoveAt(i);
        }

        if (hadVolatile && enemy.GetCurrentHP() <= 0)
            ExplodeDOTs(enemy);
    }

    public static bool IsDOT(EffectType type)
    {
        return type == EffectType.Poison ||
               type == EffectType.Burn ||
               type == EffectType.Bleed ||
               type == EffectType.Leech ||
               type == EffectType.Volatile ||
               type == EffectType.Wildfire;
    }

    public static bool IsNegativeEffect(EffectType type)
    {
        switch (type)
        {
            case EffectType.Blind:
            case EffectType.Curse:
            case EffectType.Petrify:
            case EffectType.Marked:
            case EffectType.Exposed:
            case EffectType.Weakened:
            case EffectType.Softened:
            case EffectType.Doomed:
            case EffectType.Silenced:
            case EffectType.Dazed:
            case EffectType.Charmed:
            case EffectType.Poison:
            case EffectType.Burn:
            case EffectType.Bleed:
            case EffectType.Stun:
            case EffectType.Sleep:
            case EffectType.Cripple:
            case EffectType.Vulnerable:
            case EffectType.Leech:
            case EffectType.Volatile:
            case EffectType.Wildfire:
                return true;
        }

        return false;
    }

    public static bool IsEnemyStatusEffect(EffectType type)
    {
        switch (type)
        {
            case EffectType.Blind:
            case EffectType.Curse:
            case EffectType.Petrify:
            case EffectType.Marked:
            case EffectType.Exposed:
            case EffectType.Weakened:
            case EffectType.Softened:
            case EffectType.Doomed:
            case EffectType.Silenced:
            case EffectType.Dazed:
            case EffectType.Charmed:
            case EffectType.Poison:
            case EffectType.Burn:
            case EffectType.Bleed:
            case EffectType.Stun:
            case EffectType.Sleep:
            case EffectType.Cripple:
            case EffectType.Vulnerable:
            case EffectType.Leech:
            case EffectType.Volatile:
            case EffectType.Wildfire:
                return true;
        }

        return false;
    }

    private Enemy PickRandomOtherEnemy(Enemy self)
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies.RemoveAll(enemy => enemy == null || enemy == self || enemy.GetCurrentHP() <= 0);

        if (enemies.Count == 0)
            return null;

        return enemies[Random.Range(0, enemies.Count)];
    }

    private void TrySpreadStatus(Enemy sourceEnemy, EffectType statusType, int value, int secondaryValue, int duration)
    {
        Enemy target = PickRandomOtherEnemy(sourceEnemy);

        if (target == null)
            return;

        if (sourceEnemy != null && sourceEnemy.isBoss)
            return;

        EnemyStatusController.GetOrAdd(target).ApplyEffect(target, statusType, value, secondaryValue, duration, CardDamageType.Magic, false);
    }

    private void ExplodeDOTs(Enemy sourceEnemy)
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies.RemoveAll(enemy => enemy == null || enemy == sourceEnemy || enemy.GetCurrentHP() <= 0);

        int explosionDamage = 6;

        foreach (ActiveEffect effect in activeEffects)
        {
            if (IsDOT(effect.type))
                explosionDamage += Mathf.Max(1, effect.value) * Mathf.Max(1, effect.stacks);
        }

        if (sourceEnemy != null && sourceEnemy.isBoss)
            explosionDamage = Mathf.CeilToInt(explosionDamage * 0.5f);

        foreach (Enemy enemy in enemies)
            enemy.TakeDamage(explosionDamage, CardDamageType.Magic, true);
    }
}
