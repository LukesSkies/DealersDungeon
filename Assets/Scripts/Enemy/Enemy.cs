using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Main role used by enemy AI.
public enum EnemyRole
{
    PhysicalAttacker,
    WeakPhysicalAttacker,
    SupportShieldCaster,
    Boss
}

// Controls one enemy.
[RequireComponent(typeof(EnemyStatusController))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 10;
    private int currentHP;

    [Header("Shield")]
    [SerializeField] private int currentShield = 0;
    [SerializeField] private TMP_Text shieldText;

    [Header("UI")]
    [SerializeField] private TMP_Text hpText;

    [Header("Combat")]
    public int attackDamage = 2;
    public bool isBoss = false;

    [Header("Role")]
    [SerializeField] private EnemyRole role = EnemyRole.PhysicalAttacker;

    [Header("Floor Scaling")]
    [SerializeField] private float healthBonusMultiplier = 1f;
    [SerializeField] private float damageBonusMultiplier = 1f;

    [Header("Support Shield Caster AI")]
    [SerializeField] private int shieldAmount = 3;

    [Range(0f, 1f)]
    [SerializeField] private float shieldCastChance = 0.45f;

    [SerializeField] private bool canShieldSelf = true;
    [SerializeField] private bool canShieldOthers = true;

    [Range(0f, 1f)]
    [SerializeField] private float shieldOtherChance = 0.65f;

    [SerializeField] private int maxShieldBeforeSkippingShield = 6;

    private EnemyStatusController statusController;
    private BossMechanic bossMechanic;

    private void Awake()
    {
        statusController = EnemyStatusController.GetOrAdd(this);
        bossMechanic = GetComponent<BossMechanic>();
    }

    private void Start()
    {
        if (currentHP <= 0)
            currentHP = maxHP;
        else
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (role == EnemyRole.Boss)
            isBoss = true;

        if (isBoss && bossMechanic == null)
            bossMechanic = GetComponent<BossMechanic>();

        EnemyManager.Instance?.RegisterEnemy(this);

        UpdateUI();
    }

    public void SetRole(EnemyRole newRole)
    {
        role = newRole;

        if (role == EnemyRole.Boss)
            isBoss = true;
    }

    public void ApplyFloorScaling(int floorNumber, int healthBonus, int damageBonus)
    {
        floorNumber = Mathf.Max(1, floorNumber);

        int finalHealthBonus = Mathf.RoundToInt(healthBonus * Mathf.Max(0f, healthBonusMultiplier));
        int finalDamageBonus = Mathf.RoundToInt(damageBonus * Mathf.Max(0f, damageBonusMultiplier));

        maxHP += finalHealthBonus;
        attackDamage += finalDamageBonus;

        maxHP = Mathf.Max(1, maxHP);
        attackDamage = Mathf.Max(0, attackDamage);

        currentHP = maxHP;

        UpdateUI();
    }

    public int GetCurrentHP()
    {
        return currentHP;
    }

    public int GetCurrentShield()
    {
        return currentShield;
    }

    public EnemyRole GetRole()
    {
        return role;
    }

    public bool HasEffect(EffectType type)
    {
        if (statusController == null)
            statusController = EnemyStatusController.GetOrAdd(this);

        return statusController != null && statusController.HasEffect(type);
    }

    public void AddShield(int amount)
    {
        if (amount <= 0)
            return;

        currentShield += amount;

        UpdateUI();
    }

    public void ClearShield()
    {
        currentShield = 0;
        UpdateUI();
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        if (currentHP <= 0)
            return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, CardDamageType.True, true);
    }

    public int TakeDamage(int amount, CardDamageType damageType, bool ignoreShield)
    {
        if (amount <= 0 || currentHP <= 0)
            return 0;

        if (statusController == null)
            statusController = EnemyStatusController.GetOrAdd(this);

        if (bossMechanic == null)
            bossMechanic = GetComponent<BossMechanic>();

        int remainingDamage = amount;

        bool trueDamage = ignoreShield || damageType == CardDamageType.True;

        if (statusController != null)
        {
            remainingDamage = statusController.ModifyIncomingDamage(
                this,
                remainingDamage,
                damageType,
                trueDamage
            );
        }

        if (bossMechanic != null)
        {
            remainingDamage = bossMechanic.ModifyIncomingDamage(
                this,
                remainingDamage,
                damageType,
                trueDamage
            );
        }

        remainingDamage = Mathf.Max(0, remainingDamage);

        bool exposed = HasEffect(EffectType.Exposed);

        if (!trueDamage && !exposed && currentShield > 0)
        {
            int blocked = Mathf.Min(currentShield, remainingDamage);
            currentShield -= blocked;
            remainingDamage -= blocked;
        }

        int beforeHP = currentHP;

        if (remainingDamage > 0)
            currentHP -= remainingDamage;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        int damageDealtToHP = Mathf.Max(0, beforeHP - currentHP);

        if (damageDealtToHP > 0 && statusController != null)
            statusController.NotifyDamaged(this, damageDealtToHP);

        UpdateUI();

        if (currentHP <= 0)
            Die();

        return damageDealtToHP;
    }

    public void ApplyEffect(CardEffect effect)
    {
        ApplyEffect(effect, 0f);
    }

    public void ApplyEffect(CardEffect effect, float sourceCardManaCost)
    {
        if (effect == null)
            return;

        if (statusController == null)
            statusController = EnemyStatusController.GetOrAdd(this);

        statusController.ApplyEffect(effect, sourceCardManaCost);
    }

    public void ProcessEffects()
    {
        if (statusController == null)
            statusController = EnemyStatusController.GetOrAdd(this);

        if (bossMechanic == null)
            bossMechanic = GetComponent<BossMechanic>();

        if (bossMechanic != null)
            bossMechanic.OnEnemyTurnEnd(this);

        if (statusController != null)
            statusController.ProcessTurnEnd(this);
    }

    public void AttackPlayer()
    {
        if (currentHP <= 0)
            return;

        if (statusController == null)
            statusController = EnemyStatusController.GetOrAdd(this);

        if (bossMechanic == null)
            bossMechanic = GetComponent<BossMechanic>();

        if (statusController != null && statusController.TryHandlePreAttack(this))
            return;

        if (bossMechanic != null && bossMechanic.TryHandleTurnBeforeAttack(this))
            return;

        if (TryCastShieldInsteadOfAttacking())
            return;

        int finalDamage = attackDamage;

        if (statusController != null)
            finalDamage = Mathf.RoundToInt(finalDamage * statusController.GetOutgoingDamageMultiplier());

        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.TakeDamage(Mathf.Max(0, finalDamage));

        if (bossMechanic != null)
            bossMechanic.OnAfterAttack(this);
    }

    private bool TryCastShieldInsteadOfAttacking()
    {
        if (role != EnemyRole.SupportShieldCaster)
            return false;

        if (shieldAmount <= 0)
            return false;

        if (Random.value > shieldCastChance)
            return false;

        Enemy shieldTarget = ChooseShieldTarget();

        if (shieldTarget == null)
            return false;

        if (shieldTarget.GetCurrentShield() >= maxShieldBeforeSkippingShield)
            return false;

        shieldTarget.AddShield(shieldAmount);

        Debug.Log($"{name} cast Shield on {shieldTarget.name} for {shieldAmount}.");

        return true;
    }

    private Enemy ChooseShieldTarget()
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies.RemoveAll(enemy => enemy == null || enemy.GetCurrentHP() <= 0);

        if (enemies.Count == 0)
            return null;

        bool tryShieldOther = canShieldOthers && Random.value <= shieldOtherChance;

        if (tryShieldOther)
        {
            Enemy bestOther = null;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];

                if (enemy == this)
                    continue;

                if (bestOther == null)
                {
                    bestOther = enemy;
                    continue;
                }

                if (enemy.GetCurrentShield() < bestOther.GetCurrentShield())
                    bestOther = enemy;
            }

            if (bestOther != null)
                return bestOther;
        }

        if (canShieldSelf)
            return this;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != this)
                return enemies[i];
        }

        return null;
    }

    public void TryFlee(float chance)
    {
        if (isBoss)
            return;

        if (Random.value <= Mathf.Clamp01(chance))
            Die();
    }

    private void UpdateUI()
    {
        if (hpText != null)
            hpText.text = $"{currentHP}/{maxHP}";

        if (shieldText != null)
            shieldText.text = currentShield > 0 ? $"Shield: {currentShield}" : "";
    }

    private void Die()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        if (maxHP < 1)
            maxHP = 1;

        if (attackDamage < 0)
            attackDamage = 0;

        if (currentShield < 0)
            currentShield = 0;

        if (shieldAmount < 0)
            shieldAmount = 0;

        if (maxShieldBeforeSkippingShield < 0)
            maxShieldBeforeSkippingShield = 0;

        if (healthBonusMultiplier < 0f)
            healthBonusMultiplier = 0f;

        if (damageBonusMultiplier < 0f)
            damageBonusMultiplier = 0f;

        if (role == EnemyRole.Boss)
            isBoss = true;
    }
}