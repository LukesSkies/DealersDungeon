using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

// Replacement for your Card script.
// Supports:
// - tap enemy = basic single attack, gain 1 mana
// - drag over enemies = split basic attack damage between dragged enemies, gain 0.5 mana
// - click card = cast unique spell
// - click card then enemy = targeted spell
// - target types, conditions, status effects, and card manipulation
public class Card : MonoBehaviour, IPointerClickHandler
{
    private bool isActive = false;
    private bool isHovered = false;
    private bool isWaitingForSpellTarget = false;

    private Vector3 basePosition;
    private SpriteRenderer spriteRenderer;
    private MeshRenderer meshRenderer;
    private Color originalColor = Color.white;

    [Header("Scale Settings")]
    [SerializeField] private float baseScale = 0.1f;
    [SerializeField] private float activeMultiplier = 2f;
    [SerializeField] private float hoverMultiplier = 2.5f;

    private Vector3 normalScale;
    private Vector3 activeScale;
    private Vector3 hoverScale;

    [Header("Card Data")]
    [SerializeField] private CardData cardData;

    private int temporaryDamageBonus = 0;
    private int temporaryEffectValueBonus = 0;
    private float temporaryManaCostReduction = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else if (meshRenderer != null)
            originalColor = meshRenderer.material.color;

        UpdateScales();
    }

    private void UpdateScales()
    {
        normalScale = Vector3.one * baseScale;
        activeScale = normalScale * activeMultiplier;
        hoverScale = normalScale * hoverMultiplier;
        transform.localScale = normalScale;
    }

    public void SetCardData(CardData data)
    {
        cardData = data;
    }

    public CardData GetCardData()
    {
        return cardData;
    }

    public bool IsActive()
    {
        return isActive;
    }

    public bool IsWaitingForSpellTarget()
    {
        return isWaitingForSpellTarget;
    }

    public float GetCurrentManaCost()
    {
        if (cardData == null)
            return 0f;

        float cost = cardData.manaCost - temporaryManaCostReduction;

        if (BuffManager.Instance != null)
            cost = BuffManager.Instance.ModifyManaCost(cost);

        return Mathf.Max(0f, cost);
    }

    public void ModifyManaCostTemporary(float reduction)
    {
        temporaryManaCostReduction += Mathf.Max(0f, reduction);
    }

    public void UpgradeTemporary(int damageBonus, int effectValueBonus)
    {
        temporaryDamageBonus += damageBonus;
        temporaryEffectValueBonus += effectValueBonus;
    }

    public void CopyTemporaryStateFrom(Card other)
    {
        if (other == null)
            return;

        temporaryDamageBonus = other.temporaryDamageBonus;
        temporaryEffectValueBonus = other.temporaryEffectValueBonus;
        temporaryManaCostReduction = other.temporaryManaCostReduction;
    }

    public void SetHovered(bool hovered)
    {
        if (!isActive || isHovered == hovered)
            return;

        isHovered = hovered;
        transform.DOKill();

        if (hovered)
        {
            transform.DOMoveY(basePosition.y + 0.5f, 0.2f);
            transform.DOScale(hoverScale, 0.2f);
        }
        else
        {
            transform.DOMoveY(basePosition.y, 0.2f);
            transform.DOScale(activeScale, 0.2f);
        }
    }

    public void SetBasePosition(Vector3 pos)
    {
        basePosition = pos;

        if (!isHovered)
            transform.position = pos;
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (!active)
            isWaitingForSpellTarget = false;

        Color color = active ? originalColor : Color.grey;

        if (isWaitingForSpellTarget)
            color = Color.yellow;

        if (spriteRenderer != null)
            spriteRenderer.color = color;
        else if (meshRenderer != null)
            meshRenderer.material.color = color;

        transform.DOKill();
        transform.DOScale(active ? activeScale : normalScale, 0.2f);
    }

    public void SetUsed()
    {
        isActive = false;
        isHovered = false;
        isWaitingForSpellTarget = false;

        transform.DOKill();
        transform.DOScale(normalScale, 0.2f);
        transform.DOMove(basePosition, 0.25f);

        Color usedColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);

        if (spriteRenderer != null)
            spriteRenderer.color = usedColor;
        else if (meshRenderer != null)
            meshRenderer.material.color = usedColor;
    }

    // Clicking the card casts its spell, or arms a targeted spell.
    public void Click()
    {
        if (!isActive || cardData == null)
            return;

        if (!cardData.HasSpell())
            return;

        if (cardData.RequiresEnemyTargetForSpell())
        {
            isWaitingForSpellTarget = true;
            SetActive(true);
            return;
        }

        TryCastSpell(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Click();
    }

    // EnemyTargeting calls this after the player has clicked the card spell and then clicked an enemy.
    public bool TryCastQueuedSpell(Enemy chosenTarget)
    {
        if (!isActive || !isWaitingForSpellTarget || chosenTarget == null)
            return false;

        return TryCastSpell(chosenTarget);
    }

    private bool TryCastSpell(Enemy chosenTarget)
    {
        if (cardData == null || ManaManager.Instance == null)
            return false;

        float cost = GetCurrentManaCost();

        if (cardData.spendManaOnSpellCast && !ManaManager.Instance.TrySpendMana(cost))
            return false;

        isWaitingForSpellTarget = false;

        SpellResult result = ExecuteEffects(chosenTarget);

        // This allows a card to have RefundManaOnKill as a later effect.
        if (result.refundMana > 0f && ManaManager.Instance != null)
            ManaManager.Instance.AddMana(result.refundMana);

        HandManager.Instance.UseCurrentCard();
        return true;
    }

    // Tap enemy = full basic damage to that enemy, gain 1 mana.
    public void SingleAttack(Enemy target)
    {
        if (!isActive || cardData == null || target == null)
            return;

        if (isWaitingForSpellTarget)
        {
            TryCastQueuedSpell(target);
            return;
        }

        if (!cardData.canTapAttack)
            return;

        int damage = CalculateDamage(cardData.baseDamage + temporaryDamageBonus, cardData.basicAttackDamageType);
        DealEnemyDamage(target, damage, cardData.basicAttackDamageType, false);

        if (ManaManager.Instance != null)
        {
            float gain = BuffManager.Instance == null ? 1f : BuffManager.Instance.ModifyManaGain(1f);
            ManaManager.Instance.AddMana(gain);
        }

        HandManager.Instance.UseCurrentCard();
    }

    // Backwards-compatible overload. If old code calls MultiAttack(), it hits all enemies.
    public void MultiAttack()
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        MultiAttack(enemies);
    }

    // Drag = split damage between enemies actually dragged over, gain 0.5 mana.
    public void MultiAttack(List<Enemy> draggedEnemies)
    {
        if (!isActive || cardData == null || !cardData.canDragAttack)
            return;

        if (isWaitingForSpellTarget)
            return;

        if (draggedEnemies == null)
            return;

        draggedEnemies = draggedEnemies
            .Where(enemy => enemy != null && enemy.GetCurrentHP() > 0)
            .Distinct()
            .ToList();

        if (draggedEnemies.Count == 0)
            return;

        int totalDamage = CalculateDamage(cardData.baseDamage + temporaryDamageBonus, cardData.basicAttackDamageType);
        ApplySplitDamage(draggedEnemies, totalDamage, cardData.basicAttackDamageType);

        if (ManaManager.Instance != null)
        {
            float gain = BuffManager.Instance == null ? 0.5f : BuffManager.Instance.ModifyManaGain(0.5f);
            ManaManager.Instance.AddMana(gain);
        }

        HandManager.Instance.UseCurrentCard();
    }

    private SpellResult ExecuteEffects(Enemy chosenTarget)
    {
        SpellResult result = new SpellResult();

        if (cardData.effects == null)
            return result;

        for (int i = 0; i < cardData.effects.Count; i++)
        {
            CardEffect effect = cardData.effects[i];

            if (effect == null || effect.effectType == EffectType.None)
                continue;

            if (UnityEngine.Random.value > effect.chance)
                continue;

            if (IsPlayerEffect(effect.effectType) || IsCardManipulationEffect(effect.effectType))
            {
                ApplyPlayerOrCardEffect(effect, result);
                continue;
            }

            List<Enemy> targets = ResolveTargets(effect, chosenTarget);

            for (int t = 0; t < targets.Count; t++)
            {
                Enemy enemy = targets[t];

                if (enemy == null || enemy.GetCurrentHP() <= 0)
                    continue;

                if (!CheckCondition(effect, enemy))
                    continue;

                int beforeHP = enemy.GetCurrentHP();
                ApplyEnemyEffect(effect, enemy, targets, result);
                int afterHP = enemy.GetCurrentHP();

                result.totalDamageDealt += Mathf.Max(0, beforeHP - afterHP);

                if (beforeHP > 0 && afterHP <= 0)
                    result.killedEnemy = true;

                if (effect.removeRequiredStatusAfterUse && effect.requiredStatus != EffectType.None)
                {
                    EnemyStatusController controller = EnemyStatusController.Get(enemy);
                    if (controller != null)
                        controller.RemoveEffect(effect.requiredStatus);
                }
            }
        }

        if (result.killedEnemy && result.pendingKillRefund > 0f)
            result.refundMana += result.pendingKillRefund;

        return result;
    }

    private List<Enemy> ResolveTargets(CardEffect effect, Enemy chosenTarget)
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies = enemies.Where(enemy => enemy != null && enemy.GetCurrentHP() > 0).ToList();

        switch (effect.targetType)
        {
            case TargetType.None:
            case TargetType.Self:
            case TargetType.CurrentCard:
            case TargetType.PreviousCard:
            case TargetType.NextCard:
            case TargetType.RandomCardInHand:
                return new List<Enemy>();

            case TargetType.SingleEnemy:
                return chosenTarget == null ? new List<Enemy>() : new List<Enemy> { chosenTarget };

            case TargetType.AllEnemies:
            case TargetType.DraggedEnemies:
                return enemies;

            case TargetType.RandomEnemy:
                return PickRandomEnemies(enemies, 1);

            case TargetType.RandomEnemies:
                return PickRandomEnemies(enemies, Mathf.Max(1, effect.targetCount));

            case TargetType.LowestHPEnemy:
                return enemies.OrderBy(enemy => enemy.GetCurrentHP()).Take(1).ToList();

            case TargetType.HighestHPEnemy:
                return enemies.OrderByDescending(enemy => enemy.GetCurrentHP()).Take(1).ToList();

            case TargetType.EnemyWithStatus:
                return enemies.Where(enemy => EnemyHasStatus(enemy, effect.requiredStatus)).ToList();

            case TargetType.EnemyWithoutStatus:
                return enemies.Where(enemy => !EnemyHasStatus(enemy, effect.requiredStatus)).ToList();
        }

        return new List<Enemy>();
    }

    private List<Enemy> PickRandomEnemies(List<Enemy> enemies, int count)
    {
        List<Enemy> pool = new List<Enemy>(enemies);
        List<Enemy> result = new List<Enemy>();

        while (pool.Count > 0 && result.Count < count)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private bool CheckCondition(CardEffect effect, Enemy enemy)
    {
        if (effect == null)
            return true;

        switch (effect.condition)
        {
            case EffectCondition.None:
                return true;

            case EffectCondition.TargetBelowHalfHP:
                return enemy.GetCurrentHP() <= enemy.maxHP / 2;

            case EffectCondition.TargetAboveHalfHP:
                return enemy.GetCurrentHP() > enemy.maxHP / 2;

            case EffectCondition.TargetHasPoison:
                return EnemyHasStatus(enemy, EffectType.Poison);

            case EffectCondition.TargetHasBurn:
                return EnemyHasStatus(enemy, EffectType.Burn);

            case EffectCondition.TargetHasBleed:
                return EnemyHasStatus(enemy, EffectType.Bleed);

            case EffectCondition.TargetIsStunned:
                return EnemyHasStatus(enemy, EffectType.Stun);

            case EffectCondition.TargetIsBoss:
                return enemy.isBoss;

            case EffectCondition.TargetIsNotBoss:
                return !enemy.isBoss;

            case EffectCondition.TargetHasStatus:
                return EnemyHasStatus(enemy, effect.requiredStatus);

            case EffectCondition.TargetDoesNotHaveStatus:
                return !EnemyHasStatus(enemy, effect.requiredStatus);

            case EffectCondition.TargetHasAnyDOT:
                return EnemyHasAnyDOT(enemy);

            case EffectCondition.TargetHasNoDOT:
                return !EnemyHasAnyDOT(enemy);

            case EffectCondition.TargetIsAlive:
                return enemy.GetCurrentHP() > 0;

            case EffectCondition.PlayerHasShield:
                return PlayerShield.Instance != null && PlayerShield.Instance.currentShield > 0;

            case EffectCondition.PlayerHasNoShield:
                return PlayerShield.Instance == null || PlayerShield.Instance.currentShield <= 0;

            case EffectCondition.ManaAtLeast:
                return ManaManager.Instance != null && ManaManager.Instance.currentMana >= effect.requiredMana;
        }

        return true;
    }

    private void ApplyEnemyEffect(CardEffect effect, Enemy enemy, List<Enemy> resolvedTargets, SpellResult result)
    {
        int value = effect.value + temporaryEffectValueBonus;

        switch (effect.effectType)
        {
            case EffectType.Damage:
            case EffectType.AOE:
                DealEnemyDamage(enemy, CalculateDamage(value, effect.damageType), effect.damageType, false);
                break;

            case EffectType.SplitDamage:
                ApplySplitDamage(resolvedTargets, CalculateDamage(value, effect.damageType), effect.damageType);
                break;

            case EffectType.HalfHP:
                if (!enemy.isBoss || effect.ignoreBossImmunity)
                    DealEnemyDamage(enemy, Mathf.Max(1, enemy.GetCurrentHP() / 2), CardDamageType.True, true);
                break;

            case EffectType.SplashDamage:
                ApplySplashDamage(enemy, value, effect);
                break;

            case EffectType.ChainDamage:
                ApplyChainDamage(enemy, value, effect);
                break;

            case EffectType.RandomHits:
                ApplyRandomHits(effect);
                break;

            case EffectType.MultiHit:
                ApplyMultiHit(enemy, effect);
                break;

            case EffectType.PiercingDamage:
                DealEnemyDamage(enemy, CalculateDamage(value, effect.damageType), effect.damageType, true);
                break;

            case EffectType.TrueDamage:
                DealEnemyDamage(enemy, value, CardDamageType.True, true);
                break;

            case EffectType.RecoilDamage:
                DealEnemyDamage(enemy, CalculateDamage(value, effect.damageType), effect.damageType, false);
                DamagePlayer(effect.secondaryValue > 0 ? effect.secondaryValue : Mathf.CeilToInt(value * 0.25f));
                break;

            case EffectType.OverkillDamage:
                ApplyOverkillDamage(enemy, value, effect);
                break;

            case EffectType.ExecuteDamage:
                ApplyExecuteDamage(enemy, value, effect);
                break;

            case EffectType.OpeningStrike:
                ApplyOpeningStrike(enemy, value, effect);
                break;

            case EffectType.Flee:
                if (!enemy.isBoss || effect.ignoreBossImmunity)
                    enemy.TryFlee(effect.chance);
                break;

            case EffectType.DetonateDOT:
            case EffectType.ConsumeDOT:
                ConsumeDOT(enemy, value);
                break;

            case EffectType.AmplifyDOT:
                EnemyStatusController.GetOrAdd(enemy).AmplifyDOTs(Mathf.Max(1, value));
                break;

            case EffectType.PoisonCloud:
                ApplyStatus(enemy, EffectType.Poison, value, effect);
                break;

            case EffectType.BarrierBreak:
            case EffectType.Exposed:
                ApplyStatus(enemy, effect.effectType, value, effect);
                break;

            default:
                if (IsEnemyStatus(effect.effectType))
                    ApplyStatus(enemy, effect.effectType, value, effect);
                break;
        }
    }

    private void ApplyPlayerOrCardEffect(CardEffect effect, SpellResult result)
    {
        int value = effect.value + temporaryEffectValueBonus;

        switch (effect.effectType)
        {
            case EffectType.Heal:
                if (PlayerHealth.Instance != null)
                    PlayerHealth.Instance.Heal(value);
                break;

            case EffectType.FullHeal:
                if (PlayerHealth.Instance != null)
                    PlayerHealth.Instance.SendMessage("FullHeal", SendMessageOptions.DontRequireReceiver);
                break;

            case EffectType.Shield:
                if (PlayerShield.Instance != null)
                    PlayerShield.Instance.AddShield(value);
                break;

            case EffectType.HealFromDamage:
            case EffectType.Lifesteal:
                if (PlayerHealth.Instance != null)
                {
                    int healAmount = value <= 100
                        ? Mathf.RoundToInt(result.totalDamageDealt * (value / 100f))
                        : value;

                    PlayerHealth.Instance.Heal(Mathf.Max(0, healAmount));
                }
                break;

            case EffectType.Cleanse:
                CleanseEnemies(effect.requiredStatus, Mathf.Max(1, value));
                break;

            case EffectType.CleanseAll:
                CleanseAllEnemies();
                break;

            case EffectType.CleanseSome:
                CleanseSomeEnemies(Mathf.Max(1, value));
                break;

            case EffectType.Regeneration:
            case EffectType.DamageBuff:
            case EffectType.AttackBuff:
            case EffectType.SpellDamageBuff:
            case EffectType.CostReduction:
            case EffectType.DefenseBuff:
            case EffectType.EvasionBuff:
            case EffectType.ManaGainBuff:
            case EffectType.DrawBuff:
            case EffectType.CriticalBuff:
            case EffectType.CriticalDamageBuff:
            case EffectType.Guard:
            case EffectType.MagicShield:
            case EffectType.PhysicalShield:
            case EffectType.Reflect:
            case EffectType.CounterAttack:
            case EffectType.DodgeBuff:
            case EffectType.Invisibility:
                if (BuffManager.Instance != null)
                    BuffManager.Instance.ApplyBuff(effect.effectType, value, Mathf.Max(1, effect.duration));
                break;

            case EffectType.ShieldOverload:
                ApplyShieldOverload(value);
                break;

            case EffectType.Clone:
                if (HandManager.Instance != null)
                    HandManager.Instance.CloneCardAfterCurrent(this);
                break;

            case EffectType.UpgradeCardTemporary:
                ApplyTemporaryUpgrade(effect);
                break;

            case EffectType.ReduceRandomCardCost:
                if (HandManager.Instance != null)
                    HandManager.Instance.ReduceRandomCardCost(Mathf.Max(0f, value));
                break;

            case EffectType.RefundManaOnKill:
                result.pendingKillRefund += value;
                break;
        }
    }

    private int DealEnemyDamage(Enemy enemy, int damage, CardDamageType damageType, bool trueDamage)
    {
        if (enemy == null || damage <= 0)
            return 0;

        int beforeHP = enemy.GetCurrentHP();
        int finalDamage = damage;

        EnemyStatusController controller = EnemyStatusController.Get(enemy);
        if (controller != null)
            finalDamage = controller.ModifyIncomingDamage(enemy, finalDamage, damageType, trueDamage);

        enemy.TakeDamage(Mathf.Max(0, finalDamage));

        int afterHP = enemy.GetCurrentHP();
        return Mathf.Max(0, beforeHP - afterHP);
    }

    private void ApplySplitDamage(List<Enemy> enemies, int totalDamage, CardDamageType damageType)
    {
        if (enemies == null || enemies.Count == 0 || totalDamage <= 0)
            return;

        enemies = enemies.Where(enemy => enemy != null && enemy.GetCurrentHP() > 0).Distinct().ToList();

        if (enemies.Count == 0)
            return;

        int damagePerEnemy = totalDamage / enemies.Count;
        int remainder = totalDamage % enemies.Count;

        for (int i = 0; i < enemies.Count; i++)
        {
            int finalDamage = damagePerEnemy;

            if (remainder > 0)
            {
                finalDamage++;
                remainder--;
            }

            DealEnemyDamage(enemies[i], finalDamage, damageType, false);
        }
    }

    private void ApplySplashDamage(Enemy mainTarget, int value, CardEffect effect)
    {
        DealEnemyDamage(mainTarget, CalculateDamage(value, effect.damageType), effect.damageType, false);

        int splashDamage = effect.secondaryValue > 0
            ? effect.secondaryValue
            : Mathf.Max(1, value / 2);

        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && enemy != mainTarget && enemy.GetCurrentHP() > 0)
                DealEnemyDamage(enemy, CalculateDamage(splashDamage, effect.damageType), effect.damageType, false);
        }
    }

    private void ApplyChainDamage(Enemy firstTarget, int value, CardEffect effect)
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies = enemies.Where(enemy => enemy != null && enemy.GetCurrentHP() > 0).ToList();

        Enemy currentTarget = firstTarget;
        float currentDamage = value;
        int jumps = Mathf.Max(1, effect.hitCount);

        for (int i = 0; i < jumps; i++)
        {
            if (currentTarget == null || currentTarget.GetCurrentHP() <= 0)
                break;

            DealEnemyDamage(currentTarget, CalculateDamage(Mathf.RoundToInt(currentDamage), effect.damageType), effect.damageType, false);
            enemies.Remove(currentTarget);

            if (enemies.Count == 0)
                break;

            currentTarget = enemies[UnityEngine.Random.Range(0, enemies.Count)];
            currentDamage *= Mathf.Clamp01(effect.chainDecay);

            if (currentDamage < 1f)
                break;
        }
    }

    private void ApplyRandomHits(CardEffect effect)
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies = enemies.Where(enemy => enemy != null && enemy.GetCurrentHP() > 0).ToList();

        if (enemies.Count == 0)
            return;

        int hits = Mathf.Max(1, effect.hitCount);
        int value = effect.value + temporaryEffectValueBonus;

        for (int i = 0; i < hits; i++)
        {
            Enemy enemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];
            DealEnemyDamage(enemy, CalculateDamage(value, effect.damageType), effect.damageType, false);
        }
    }

    private void ApplyMultiHit(Enemy enemy, CardEffect effect)
    {
        int hits = Mathf.Max(1, effect.hitCount);
        int value = effect.value + temporaryEffectValueBonus;

        for (int i = 0; i < hits; i++)
            DealEnemyDamage(enemy, CalculateDamage(value, effect.damageType), effect.damageType, false);
    }

    private void ApplyOverkillDamage(Enemy enemy, int value, CardEffect effect)
    {
        int beforeHP = enemy.GetCurrentHP();
        int dealt = DealEnemyDamage(enemy, CalculateDamage(value, effect.damageType), effect.damageType, false);
        int leftover = Mathf.Max(0, CalculateDamage(value, effect.damageType) - beforeHP);

        if (dealt <= 0 || leftover <= 0)
            return;

        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies = enemies.Where(e => e != null && e != enemy && e.GetCurrentHP() > 0).OrderBy(e => e.GetCurrentHP()).ToList();

        if (enemies.Count > 0)
            DealEnemyDamage(enemies[0], leftover, effect.damageType, false);
    }

    private void ApplyExecuteDamage(Enemy enemy, int value, CardEffect effect)
    {
        int thresholdPercent = effect.secondaryValue > 0 ? effect.secondaryValue : 30;
        bool canExecute = enemy.GetCurrentHP() <= Mathf.CeilToInt(enemy.maxHP * (thresholdPercent / 100f));

        if (canExecute)
            DealEnemyDamage(enemy, CalculateDamage(value * 2, effect.damageType), effect.damageType, false);
        else
            DealEnemyDamage(enemy, CalculateDamage(value, effect.damageType), effect.damageType, false);
    }

    private void ApplyOpeningStrike(Enemy enemy, int value, CardEffect effect)
    {
        bool aboveHalf = enemy.GetCurrentHP() > enemy.maxHP / 2;
        int damage = aboveHalf ? value + Mathf.Max(1, effect.secondaryValue) : value;
        DealEnemyDamage(enemy, CalculateDamage(damage, effect.damageType), effect.damageType, false);
    }

    private void ApplyStatus(Enemy enemy, EffectType status, int value, CardEffect sourceEffect)
    {
        if (enemy == null)
            return;

        if (enemy.isBoss && !sourceEffect.ignoreBossImmunity)
        {
            if (status == EffectType.Fear || status == EffectType.Panicked || status == EffectType.Flee || status == EffectType.HalfHP)
                return;
        }

        CardEffect runtimeEffect = CloneEffect(sourceEffect);
        runtimeEffect.effectType = status;
        runtimeEffect.value = value;

        EnemyStatusController.GetOrAdd(enemy).ApplyEffect(runtimeEffect, GetCurrentManaCost());
    }

    private void ConsumeDOT(Enemy enemy, int multiplier)
    {
        EnemyStatusController controller = EnemyStatusController.Get(enemy);

        if (controller == null)
            return;

        int damage = controller.ConsumeDOTsForDamage(Mathf.Max(1, multiplier));
        DealEnemyDamage(enemy, damage, CardDamageType.True, true);
    }

    private void CleanseEnemies(EffectType status, int count)
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        foreach (Enemy enemy in enemies)
        {
            EnemyStatusController controller = EnemyStatusController.Get(enemy);

            if (controller == null)
                continue;

            if (status != EffectType.None)
                controller.RemoveEffect(status);
            else
                controller.RemoveSomeNegativeEffects(count);
        }
    }

    private void CleanseAllEnemies()
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        foreach (Enemy enemy in enemies)
        {
            EnemyStatusController controller = EnemyStatusController.Get(enemy);
            if (controller != null)
                controller.RemoveAllNegativeEffects();
        }
    }

    private void CleanseSomeEnemies(int count)
    {
        CleanseEnemies(EffectType.None, count);
    }

    private void ApplyShieldOverload(int value)
    {
        if (PlayerShield.Instance == null)
            return;

        // Your max shield is private in PlayerShield, so this simple version says:
        // if the player already has any shield, turn this effect into all-enemy damage.
        if (PlayerShield.Instance.currentShield > 0)
        {
            List<Enemy> enemies = EnemyManager.Instance == null
                ? new List<Enemy>()
                : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

            foreach (Enemy enemy in enemies)
            {
                if (enemy != null && enemy.GetCurrentHP() > 0)
                    DealEnemyDamage(enemy, Mathf.Max(1, value), CardDamageType.True, true);
            }
        }
        else
        {
            PlayerShield.Instance.AddShield(1);
        }
    }

    private void ApplyTemporaryUpgrade(CardEffect effect)
    {
        Card targetCard = this;

        if (HandManager.Instance != null)
        {
            switch (effect.targetType)
            {
                case TargetType.PreviousCard:
                    targetCard = HandManager.Instance.GetPreviousCard(this);
                    break;

                case TargetType.NextCard:
                    targetCard = HandManager.Instance.GetNextCard(this);
                    break;

                case TargetType.RandomCardInHand:
                    targetCard = HandManager.Instance.GetRandomCardInHand();
                    break;
            }
        }

        if (targetCard != null)
            targetCard.UpgradeTemporary(effect.value, effect.secondaryValue);
    }

    private int CalculateDamage(int baseValue, CardDamageType damageType)
    {
        float multiplier = BuffManager.Instance == null ? 1f : BuffManager.Instance.GetDamageMultiplier(damageType);
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * multiplier));
    }

    private void DamagePlayer(int amount)
    {
        if (amount <= 0 || PlayerHealth.Instance == null)
            return;

        PlayerHealth.Instance.SendMessage("TakeDamage", amount, SendMessageOptions.DontRequireReceiver);
    }

    private bool EnemyHasStatus(Enemy enemy, EffectType status)
    {
        if (enemy == null || status == EffectType.None)
            return false;

        EnemyStatusController controller = EnemyStatusController.Get(enemy);
        bool controllerHasStatus = controller != null && controller.HasEffect(status);

        if (controllerHasStatus)
            return true;

        // Backwards compatible with your old Enemy.HasEffect system.
        return enemy.HasEffect(status);
    }

    private bool EnemyHasAnyDOT(Enemy enemy)
    {
        if (enemy == null)
            return false;

        EnemyStatusController controller = EnemyStatusController.Get(enemy);

        if (controller != null && controller.HasAnyDOT())
            return true;

        return enemy.HasEffect(EffectType.Poison) || enemy.HasEffect(EffectType.Burn) || enemy.HasEffect(EffectType.Bleed);
    }

    private bool IsPlayerEffect(EffectType type)
    {
        switch (type)
        {
            case EffectType.Heal:
            case EffectType.Shield:
            case EffectType.Cleanse:
            case EffectType.CleanseAll:
            case EffectType.CleanseSome:
            case EffectType.Regeneration:
            case EffectType.FullHeal:
            case EffectType.HealFromDamage:
            case EffectType.Lifesteal:
            case EffectType.DamageBuff:
            case EffectType.AttackBuff:
            case EffectType.SpellDamageBuff:
            case EffectType.CostReduction:
            case EffectType.DefenseBuff:
            case EffectType.EvasionBuff:
            case EffectType.ManaGainBuff:
            case EffectType.DrawBuff:
            case EffectType.CriticalBuff:
            case EffectType.CriticalDamageBuff:
            case EffectType.Guard:
            case EffectType.MagicShield:
            case EffectType.PhysicalShield:
            case EffectType.Reflect:
            case EffectType.CounterAttack:
            case EffectType.DodgeBuff:
            case EffectType.Invisibility:
            case EffectType.ShieldOverload:
                return true;
        }

        return false;
    }

    private bool IsCardManipulationEffect(EffectType type)
    {
        return type == EffectType.Clone ||
               type == EffectType.UpgradeCardTemporary ||
               type == EffectType.ReduceRandomCardCost ||
               type == EffectType.RefundManaOnKill;
    }

    private bool IsEnemyStatus(EffectType type)
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
            case EffectType.Charmed:
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
            case EffectType.VolatileDOT:
            case EffectType.Wildfire:
            case EffectType.AttackDebuff:
            case EffectType.DefenseDebuff:
            case EffectType.EvasionDebuff:
            case EffectType.SpellPowerDebuff:
            case EffectType.HealingDebuff:
                return true;
        }

        return false;
    }

    private CardEffect CloneEffect(CardEffect source)
    {
        return new CardEffect
        {
            effectType = source.effectType,
            targetType = source.targetType,
            damageType = source.damageType,
            value = source.value,
            secondaryValue = source.secondaryValue,
            duration = source.duration,
            hitCount = source.hitCount,
            targetCount = source.targetCount,
            randomMinDuration = source.randomMinDuration,
            randomMaxDuration = source.randomMaxDuration,
            chance = source.chance,
            condition = source.condition,
            requiredStatus = source.requiredStatus,
            requiredMana = source.requiredMana,
            chainDecay = source.chainDecay,
            ignoreBossImmunity = source.ignoreBossImmunity,
            removeRequiredStatusAfterUse = source.removeRequiredStatusAfterUse,
            designerNote = source.designerNote
        };
    }

    private class SpellResult
    {
        public int totalDamageDealt = 0;
        public bool killedEnemy = false;
        public float refundMana = 0f;
        public float pendingKillRefund = 0f;
    }
}
