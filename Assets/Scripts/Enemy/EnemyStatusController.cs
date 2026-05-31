using System.Collections.Generic;
using UnityEngine;

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

    public void ApplyEffect(CardEffect effect, float sourceCardManaCost)
    {
        if (effect == null || effect.effectType == EffectType.None)
            return;

        ActiveEffect existing = activeEffects.Find(e => e.type == effect.effectType);
        ActiveEffect newEffect = new ActiveEffect(effect, sourceCardManaCost);

        if (existing != null)
        {
            existing.value = Mathf.Max(existing.value, newEffect.value);
            existing.secondaryValue = Mathf.Max(existing.secondaryValue, newEffect.secondaryValue);
            existing.duration = Mathf.Max(existing.duration, newEffect.duration);
            existing.stacks++;
            existing.damageType = newEffect.damageType;
        }
        else
        {
            activeEffects.Add(newEffect);
        }
    }

    public bool HasEffect(EffectType type)
    {
        return activeEffects.Exists(e => e.type == type && e.duration != 0);
    }

    public ActiveEffect GetEffect(EffectType type)
    {
        return activeEffects.Find(e => e.type == type && e.duration != 0);
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
               HasEffect(EffectType.Rot) ||
               HasEffect(EffectType.LeechDOT);
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
                damage += Mathf.Max(1, activeEffects[i].value) * Mathf.Max(1, activeEffects[i].stacks) * Mathf.Max(1, damagePerStack);
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
                activeEffects[i].value += amount;
        }
    }

    public int ModifyIncomingDamage(Enemy enemy, int incomingDamage, CardDamageType damageType, bool trueDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        if (trueDamage || damageType == CardDamageType.True)
            return incomingDamage;

        float multiplier = 1f;
        int bonusFlatDamage = 0;

        if (HasEffect(EffectType.Petrify))
            multiplier *= 0.5f;

        if (HasEffect(EffectType.Vulnerable))
            multiplier *= 1.5f;

        if (HasEffect(EffectType.DefenseDebuff))
            multiplier *= 1.25f;

        if (HasEffect(EffectType.Curse))
            multiplier *= 1.25f;

        if (HasEffect(EffectType.Bleed))
            multiplier *= 1.2f;

        if (HasEffect(EffectType.Shock) && damageType == CardDamageType.Spell)
            multiplier *= 1.3f;

        if (HasEffect(EffectType.Softened) && damageType == CardDamageType.Physical)
            multiplier *= 1.3f;

        if (HasEffect(EffectType.Cripple) && enemy != null && enemy.GetCurrentHP() <= enemy.maxHP / 2)
            multiplier *= 1.5f;

        ActiveEffect marked = GetEffect(EffectType.Marked);
        if (marked != null)
        {
            bonusFlatDamage += Mathf.Max(1, marked.value);
            RemoveEffect(EffectType.Marked);
        }

        int result = Mathf.RoundToInt(incomingDamage * multiplier) + bonusFlatDamage;
        return Mathf.Max(0, result);
    }

    // Returns true if the enemy action has already been handled or should be skipped.
    public bool TryHandlePreAttack(Enemy enemy)
    {
        if (enemy == null)
            return true;

        if (HasEffect(EffectType.Stun) || HasEffect(EffectType.Sleep) || HasEffect(EffectType.Petrify))
            return true;

        ActiveEffect dazed = GetEffect(EffectType.Dazed);
        if (dazed != null)
        {
            float missChance = Mathf.Clamp01(Mathf.Max(1, dazed.value) / 100f);
            if (UnityEngine.Random.value <= missChance)
                return true;
        }

        ActiveEffect blind = GetEffect(EffectType.Blind);
        if (blind != null)
        {
            float missChance = Mathf.Clamp01(Mathf.Max(1, blind.value) / 100f);
            if (UnityEngine.Random.value <= missChance)
                return true;
        }

        ActiveEffect panicked = GetEffect(EffectType.Panicked);
        if (panicked != null)
        {
            float failActionChance = Mathf.Clamp01(Mathf.Max(1, panicked.value) / 100f);
            if (UnityEngine.Random.value <= failActionChance)
                return true;
        }

        ActiveEffect confusion = GetEffect(EffectType.Confusion);
        if (confusion != null)
        {
            float selfHitChance = Mathf.Clamp01(Mathf.Max(1, confusion.secondaryValue) / 100f);

            if (confusion.secondaryValue <= 0)
                selfHitChance = 0.5f;

            if (UnityEngine.Random.value <= selfHitChance)
            {
                int selfDamage = Mathf.Max(1, confusion.value);
                enemy.TakeDamage(selfDamage);
                return true;
            }
        }

        ActiveEffect charmed = GetEffect(EffectType.Charmed);
        if (charmed != null)
        {
            float charmChance = Mathf.Clamp01(Mathf.Max(1, charmed.secondaryValue) / 100f);

            if (charmed.secondaryValue <= 0)
                charmChance = 0.5f;

            if (UnityEngine.Random.value <= charmChance)
            {
                Enemy otherEnemy = PickRandomOtherEnemy(enemy);

                if (otherEnemy != null)
                    otherEnemy.TakeDamage(Mathf.Max(1, charmed.value));
                else
                    enemy.TakeDamage(Mathf.Max(1, charmed.value));

                return true;
            }
        }

        return false;
    }

    public void ProcessTurnEnd(Enemy enemy)
    {
        if (enemy == null)
            return;

        bool causedVolatileDeath = false;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];

            switch (effect.type)
            {
                case EffectType.Poison:
                case EffectType.Burn:
                case EffectType.Bleed:
                case EffectType.Rot:
                    enemy.TakeDamage(Mathf.Max(1, effect.value) * Mathf.Max(1, effect.stacks));
                    break;

                case EffectType.LeechDOT:
                    int leechDamage = Mathf.Max(1, effect.value) * Mathf.Max(1, effect.stacks);
                    enemy.TakeDamage(leechDamage);
                    if (PlayerHealth.Instance != null)
                        PlayerHealth.Instance.Heal(Mathf.Max(1, leechDamage / 2));
                    break;

                case EffectType.Doomed:
                    if (effect.duration == 1)
                        enemy.TakeDamage(Mathf.Max(1, effect.value));
                    break;

                case EffectType.Wildfire:
                    TrySpreadStatus(enemy, EffectType.Burn, effect.value, Mathf.Max(1, effect.secondaryValue));
                    break;
            }

            effect.duration--;

            if (effect.duration <= 0)
                activeEffects.RemoveAt(i);
        }

        if (enemy.GetCurrentHP() <= 0 && HasEffect(EffectType.VolatileDOT))
            causedVolatileDeath = true;

        if (causedVolatileDeath)
            ExplodeDOTs(enemy);
    }

    public static bool IsDOT(EffectType type)
    {
        return type == EffectType.Poison ||
               type == EffectType.Burn ||
               type == EffectType.Bleed ||
               type == EffectType.Rot ||
               type == EffectType.LeechDOT;
    }

    public static bool IsNegativeEffect(EffectType type)
    {
        switch (type)
        {
            case EffectType.Poison:
            case EffectType.Burn:
            case EffectType.Bleed:
            case EffectType.Blind:
            case EffectType.Curse:
            case EffectType.Petrify:
            case EffectType.Shock:
            case EffectType.Marked:
            case EffectType.Exposed:
            case EffectType.Weakened:
            case EffectType.Softened:
            case EffectType.Hexed:
            case EffectType.Doomed:
            case EffectType.Silenced:
            case EffectType.Dazed:
            case EffectType.Panicked:
            case EffectType.Taunted:
            case EffectType.Stun:
            case EffectType.Sleep:
            case EffectType.Confusion:
            case EffectType.Fear:
            case EffectType.Cripple:
            case EffectType.Vulnerable:
            case EffectType.Rot:
            case EffectType.LeechDOT:
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

        return enemies[UnityEngine.Random.Range(0, enemies.Count)];
    }

    private void TrySpreadStatus(Enemy sourceEnemy, EffectType statusType, int value, int duration)
    {
        Enemy target = PickRandomOtherEnemy(sourceEnemy);

        if (target == null)
            return;

        CardEffect spreadEffect = new CardEffect
        {
            effectType = statusType,
            value = value,
            duration = duration,
            targetType = TargetType.SingleEnemy
        };

        EnemyStatusController.GetOrAdd(target).ApplyEffect(spreadEffect, 0f);
    }

    private void ExplodeDOTs(Enemy sourceEnemy)
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies.RemoveAll(enemy => enemy == null || enemy == sourceEnemy || enemy.GetCurrentHP() <= 0);

        int explosionDamage = 0;

        foreach (ActiveEffect effect in activeEffects)
        {
            if (IsDOT(effect.type))
                explosionDamage += Mathf.Max(1, effect.value) * Mathf.Max(1, effect.stacks);
        }

        if (explosionDamage <= 0)
            explosionDamage = 1;

        foreach (Enemy enemy in enemies)
            enemy.TakeDamage(explosionDamage);
    }
}
