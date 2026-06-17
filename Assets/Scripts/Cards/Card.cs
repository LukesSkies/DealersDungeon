using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// Controls one combat card.
public class Card : MonoBehaviour, IPointerClickHandler
{
    private bool isActive = false;
    private bool isHovered = false;
    private bool isWaitingForSkillTarget = false;
    private bool isCastingSkill = false;

    private int lastClickFrame = -1;
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

    private int temporaryBasicDamageBonus = 0;
    private int temporarySkillValueBonus = 0;
    private float temporaryManaCostReduction = 0f;

    private class SkillCastContext
    {
        public Enemy chosenTarget;
        public MiniGameResult miniGameResult;
        public float manaSpent;
        public int totalDamageDealt;
        public int lastDamageDealt;
        public bool killedEnemy;
    }

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
        return isWaitingForSkillTarget;
    }

    public bool IsWaitingForSkillTarget()
    {
        return isWaitingForSkillTarget;
    }

    public float GetCurrentManaCost()
    {
        if (cardData == null)
            return 0f;

        if (cardData.manaCostMode == CardManaCostMode.AllRemaining)
        {
            if (ManaManager.Instance == null)
                return 0f;

            return Mathf.Max(0f, ManaManager.Instance.currentMana);
        }

        float cost = cardData.manaCost - temporaryManaCostReduction;

        if (BuffManager.Instance != null)
            cost = BuffManager.Instance.ModifyManaCost(cost);

        return Mathf.Max(0f, cost);
    }

    public void ModifyManaCostTemporary(float reduction)
    {
        temporaryManaCostReduction += Mathf.Max(0f, reduction);
    }

    public void UpgradeTemporary(int basicDamageBonus, int skillValueBonus)
    {
        temporaryBasicDamageBonus += basicDamageBonus;
        temporarySkillValueBonus += skillValueBonus;
    }

    public void CopyTemporaryStateFrom(Card other)
    {
        if (other == null)
            return;

        temporaryBasicDamageBonus = other.temporaryBasicDamageBonus;
        temporarySkillValueBonus = other.temporarySkillValueBonus;
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
        {
            isHovered = false;
            isWaitingForSkillTarget = false;
            isCastingSkill = false;
        }

        RefreshVisualColor();

        transform.DOKill();
        transform.DOScale(active ? activeScale : normalScale, 0.2f);
    }

    public void SetUsed()
    {
        isActive = false;
        isHovered = false;
        isWaitingForSkillTarget = false;
        isCastingSkill = false;

        transform.DOKill();
        transform.DOScale(normalScale, 0.2f);
        transform.DOMove(basePosition, 0.25f);

        SetRendererColor(new Color(0.3f, 0.3f, 0.3f, 0.6f));
    }

    public void CancelQueuedSpell()
    {
        if (!isWaitingForSkillTarget)
            return;

        isWaitingForSkillTarget = false;
        RefreshVisualColor();
    }

    public void CancelQueuedSkill()
    {
        CancelQueuedSpell();
    }

    public void Click()
    {
        if (lastClickFrame == Time.frameCount)
            return;

        lastClickFrame = Time.frameCount;

        if (!isActive || cardData == null || isCastingSkill)
            return;

        if (!cardData.HasSkill())
            return;

        if (isWaitingForSkillTarget)
        {
            CancelQueuedSpell();
            return;
        }

        if (!CanAffordSkill())
        {
            FlashNoMana();
            return;
        }

        if (cardData.RequiresEnemyTargetForSkill())
        {
            isWaitingForSkillTarget = true;
            RefreshVisualColor();
            return;
        }

        TryCastSkill(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Click();
    }

    public bool TryCastQueuedSpell(Enemy chosenTarget)
    {
        return TryCastQueuedSkill(chosenTarget);
    }

    public bool TryCastQueuedSkill(Enemy chosenTarget)
    {
        if (!isActive || !isWaitingForSkillTarget || isCastingSkill)
            return false;

        if (chosenTarget == null)
        {
            CancelQueuedSpell();
            return false;
        }

        return TryCastSkill(chosenTarget);
    }

    private bool TryCastSkill(Enemy chosenTarget)
    {
        if (cardData == null || isCastingSkill)
            return false;

        if (cardData.RequiresEnemyTargetForSkill() && chosenTarget == null)
            return false;

        float manaSpent = 0f;

        if (cardData.spendManaOnSkillCast)
        {
            if (ManaManager.Instance == null)
            {
                FlashNoMana();
                return false;
            }

            if (cardData.manaCostMode == CardManaCostMode.AllRemaining)
            {
                if (ManaManager.Instance.currentMana < cardData.minimumManaToCast)
                {
                    FlashNoMana();
                    return false;
                }

                manaSpent = ManaManager.Instance.SpendAllMana();
            }
            else
            {
                float cost = GetCurrentManaCost();

                if (!ManaManager.Instance.TrySpendMana(cost))
                {
                    FlashNoMana();
                    return false;
                }

                manaSpent = cost;
            }
        }

        isWaitingForSkillTarget = false;
        RefreshVisualColor();

        if (cardData.HasMiniGame() && MiniGameManager.Instance != null)
        {
            isCastingSkill = true;
            RefreshVisualColor();

            MiniGameManager.Instance.PlayMiniGameResult(cardData, miniGameResult =>
            {
                FinishCastSkill(chosenTarget, miniGameResult, manaSpent);
            });

            return true;
        }

        FinishCastSkill(chosenTarget, MiniGameResult.None(), manaSpent);
        return true;
    }

    private void FinishCastSkill(Enemy chosenTarget, MiniGameResult miniGameResult, float manaSpent)
    {
        if (this == null)
            return;

        isCastingSkill = false;
        RefreshVisualColor();

        SkillCastContext context = new SkillCastContext
        {
            chosenTarget = chosenTarget,
            miniGameResult = miniGameResult ?? MiniGameResult.None(),
            manaSpent = manaSpent
        };

        ExecuteEffects(context);

        if (HandManager.Instance != null)
            HandManager.Instance.UseCurrentCard();
    }

    public void SingleAttack(Enemy target)
    {
        if (!isActive || cardData == null || target == null || isCastingSkill)
            return;

        if (isWaitingForSkillTarget)
        {
            TryCastQueuedSkill(target);
            return;
        }

        if (!cardData.CanTapAttack())
            return;

        int damage = CalculateBasicDamage(cardData.baseDamage + temporaryBasicDamageBonus, cardData.basicAttackDamageType);
        DealEnemyDamage(target, damage, cardData.basicAttackDamageType, false, null);

        if (ManaManager.Instance != null)
        {
            float gain = BuffManager.Instance == null ? 1f : BuffManager.Instance.ModifyManaGain(1f);
            ManaManager.Instance.AddMana(gain);
        }

        if (HandManager.Instance != null)
            HandManager.Instance.UseCurrentCard();
    }

    public void MultiAttack()
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        MultiAttack(enemies);
    }

    public void MultiAttack(List<Enemy> draggedEnemies)
    {
        if (!isActive || cardData == null || isCastingSkill)
            return;

        if (isWaitingForSkillTarget)
            return;

        if (!cardData.CanDragAttack())
            return;

        if (draggedEnemies == null)
            return;

        draggedEnemies = draggedEnemies
            .Where(enemy => enemy != null && enemy.GetCurrentHP() > 0)
            .Distinct()
            .ToList();

        if (draggedEnemies.Count == 0)
            return;

        int totalDamage = CalculateBasicDamage(cardData.baseDamage + temporaryBasicDamageBonus, cardData.basicAttackDamageType);
        ApplySplitBasicDamage(draggedEnemies, totalDamage, cardData.basicAttackDamageType);

        if (ManaManager.Instance != null)
        {
            float gain = BuffManager.Instance == null ? 0.5f : BuffManager.Instance.ModifyManaGain(0.5f);
            ManaManager.Instance.AddMana(gain);
        }

        if (HandManager.Instance != null)
            HandManager.Instance.UseCurrentCard();
    }

    private bool CanAffordSkill()
    {
        if (cardData == null)
            return false;

        if (!cardData.spendManaOnSkillCast)
            return true;

        if (ManaManager.Instance == null)
            return false;

        if (cardData.manaCostMode == CardManaCostMode.AllRemaining)
            return ManaManager.Instance.currentMana >= cardData.minimumManaToCast;

        float cost = GetCurrentManaCost();

        if (cost <= 0f)
            return true;

        return ManaManager.Instance.currentMana >= cost;
    }

    private void ExecuteEffects(SkillCastContext context)
    {
        if (cardData == null || cardData.effects == null)
            return;

        for (int i = 0; i < cardData.effects.Count; i++)
        {
            CardEffect effect = cardData.effects[i];

            if (effect == null || effect.effectType == EffectType.None)
                continue;

            if (IsPlayerEffect(effect.effectType) || IsCardManipulationEffect(effect.effectType))
            {
                ApplyPlayerOrCardEffect(effect, context);
                continue;
            }

            if (effect.effectType == EffectType.RandomHits)
            {
                ApplyRandomHits(effect, context);
                continue;
            }

            List<Enemy> targets = ResolveTargets(effect, context.chosenTarget);

            for (int t = 0; t < targets.Count; t++)
            {
                Enemy enemy = targets[t];

                if (enemy == null || enemy.GetCurrentHP() <= 0)
                    continue;

                if (!EffectCanHitTarget(effect, enemy))
                    continue;

                float chance = enemy.isBoss ? effect.chance * effect.bossChanceMultiplier : effect.chance;

                if (Random.value > Mathf.Clamp01(chance))
                    continue;

                ApplyEnemyEffect(effect, enemy, context);
            }
        }
    }

    private List<Enemy> ResolveTargets(CardEffect effect, Enemy chosenTarget)
    {
        CardTargetType targetType = effect.GetTargetType(cardData);
        List<Enemy> enemies = GetAliveEnemies();

        switch (targetType)
        {
            case CardTargetType.SelectedEnemy:
                return chosenTarget == null ? new List<Enemy>() : new List<Enemy> { chosenTarget };

            case CardTargetType.RandomEnemy:
                return PickRandomEnemies(enemies, 1);

            case CardTargetType.RandomNonBossEnemy:
                return PickRandomEnemies(enemies.Where(enemy => !enemy.isBoss).ToList(), 1);

            case CardTargetType.LowestHPEnemy:
                return enemies.OrderBy(enemy => enemy.GetCurrentHP()).Take(1).ToList();

            case CardTargetType.HighestHPEnemy:
                return enemies.OrderByDescending(enemy => enemy.GetCurrentHP()).Take(1).ToList();

            case CardTargetType.AdjacentEnemies:
                return GetAdjacentEnemies(chosenTarget);

            case CardTargetType.AllNonBossEnemies:
                return enemies.Where(enemy => !enemy.isBoss).ToList();

            case CardTargetType.AllEnemies:
                return enemies;
        }

        return new List<Enemy>();
    }

    private List<Enemy> PickRandomEnemies(List<Enemy> enemies, int count)
    {
        List<Enemy> pool = new List<Enemy>(enemies);
        List<Enemy> result = new List<Enemy>();

        while (pool.Count > 0 && result.Count < count)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private List<Enemy> GetAdjacentEnemies(Enemy chosenTarget)
    {
        if (chosenTarget == null)
            return new List<Enemy>();

        List<Enemy> enemies = GetAliveEnemies()
            .OrderBy(enemy => enemy.transform.position.x)
            .ToList();

        int index = enemies.IndexOf(chosenTarget);
        List<Enemy> result = new List<Enemy>();

        if (index > 0)
            result.Add(enemies[index - 1]);

        if (index >= 0 && index < enemies.Count - 1)
            result.Add(enemies[index + 1]);

        return result;
    }

    private bool EffectCanHitTarget(CardEffect effect, Enemy enemy)
    {
        if (enemy == null)
            return false;

        if (enemy.isBoss && !effect.worksOnBosses)
            return false;

        return true;
    }

    private void ApplyEnemyEffect(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        switch (effect.effectType)
        {
            case EffectType.Damage:
                DealEnemyDamage(enemy, CalculateSkillValue(effect, enemy, context), effect.damageType, false, context);
                break;

            case EffectType.PiercingDamage:
                DealEnemyDamage(enemy, CalculateSkillValue(effect, enemy, context), effect.damageType, true, context);
                break;

            case EffectType.PercentMissingHPDamage:
                ApplyPercentMissingHPDamage(effect, enemy, context);
                break;

            case EffectType.PercentMaxHPDamage:
                ApplyPercentMaxHPDamage(effect, enemy, context);
                break;

            case EffectType.AdjacentSplashDamage:
                ApplyAdjacentSplashDamage(effect, context);
                break;

            case EffectType.LifeStealDamage:
                ApplyLifeStealDamage(effect, enemy, context);
                break;

            case EffectType.AllInDamage:
                ApplyAllInDamage(effect, enemy, context);
                break;

            case EffectType.Flee:
                ApplyFlee(effect, enemy, context);
                break;

            case EffectType.RandomStatus:
                ApplyRandomStatus(effect, enemy, context);
                break;

            case EffectType.Exposed:
                enemy.ClearShield();
                ApplyStatus(effect, enemy, context);
                break;

            default:
                if (EnemyStatusController.IsEnemyStatusEffect(effect.effectType))
                    ApplyStatus(effect, enemy, context);
                break;
        }
    }

    private void ApplyRandomHits(CardEffect effect, SkillCastContext context)
    {
        int hits = effect.GetHitCount();

        for (int i = 0; i < hits; i++)
        {
            List<Enemy> enemies = GetAliveEnemies();

            if (enemies.Count == 0)
                return;

            Enemy enemy = enemies[Random.Range(0, enemies.Count)];

            if (!EffectCanHitTarget(effect, enemy))
                continue;

            float chance = enemy.isBoss ? effect.chance * effect.bossChanceMultiplier : effect.chance;

            if (Random.value > Mathf.Clamp01(chance))
                continue;

            int value = CalculateSkillValue(effect, enemy, context);
            DealEnemyDamage(enemy, value, effect.damageType, false, context);
            TryApplyStatusOnHit(effect, enemy);
        }
    }

    private void TryApplyStatusOnHit(CardEffect effect, Enemy enemy)
    {
        if (effect == null || enemy == null)
            return;

        if (effect.statusAppliedOnHit == EffectType.None)
            return;

        if (!EnemyStatusController.IsEnemyStatusEffect(effect.statusAppliedOnHit))
            return;

        if (enemy.isBoss && !effect.statusWorksOnBosses)
            return;

        float chance = enemy.isBoss
            ? effect.statusChanceOnHit * effect.statusBossChanceMultiplier
            : effect.statusChanceOnHit;

        if (Random.value > Mathf.Clamp01(chance))
            return;

        int duration = effect.statusDurationOnHit;

        if (enemy.isBoss)
            duration = Mathf.CeilToInt(duration * effect.statusBossDurationMultiplier);

        EnemyStatusController.GetOrAdd(enemy).ApplyEffect(
            enemy,
            effect.statusAppliedOnHit,
            Mathf.Max(0, effect.statusValueOnHit),
            Mathf.Max(0, effect.statusSecondaryValueOnHit),
            Mathf.Max(0, duration),
            effect.damageType,
            false
        );
    }

    private void ApplyPercentMissingHPDamage(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        int missingHP = Mathf.Max(0, enemy.maxHP - enemy.GetCurrentHP());
        int percent = Mathf.Max(0, effect.value);
        int damage = Mathf.RoundToInt(missingHP * (percent / 100f));

        if (effect.scaleValueWithMiniGame && context.miniGameResult != null)
            damage = Mathf.RoundToInt(damage * context.miniGameResult.multiplier);

        if (effect.addBaseDamageToValue)
            damage += cardData.baseDamage;

        if (enemy.isBoss)
            damage = Mathf.RoundToInt(damage * effect.bossValueMultiplier);

        DealEnemyDamage(enemy, CalculateSkillDamage(damage, effect.damageType), effect.damageType, false, context);
    }

    private void ApplyPercentMaxHPDamage(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        int percent = Mathf.Max(0, effect.value);
        int damage = Mathf.RoundToInt(enemy.maxHP * (percent / 100f));

        if (effect.scaleValueWithMiniGame && context.miniGameResult != null)
            damage = Mathf.RoundToInt(damage * context.miniGameResult.multiplier);

        if (effect.addBaseDamageToValue)
            damage += cardData.baseDamage;

        if (enemy.isBoss)
            damage = Mathf.RoundToInt(damage * effect.bossValueMultiplier);

        DealEnemyDamage(enemy, CalculateSkillDamage(damage, effect.damageType), effect.damageType, false, context);
    }

    private void ApplyAdjacentSplashDamage(CardEffect effect, SkillCastContext context)
    {
        if (context.chosenTarget == null)
            return;

        int percent = effect.value > 0 ? effect.value : 50;
        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(context.lastDamageDealt * (percent / 100f)));
        List<Enemy> adjacentEnemies = GetAdjacentEnemies(context.chosenTarget);

        for (int i = 0; i < adjacentEnemies.Count; i++)
            DealEnemyDamage(adjacentEnemies[i], splashDamage, effect.damageType, false, context);
    }

    private void ApplyLifeStealDamage(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        int damage = CalculateSkillValue(effect, enemy, context);
        int dealt = DealEnemyDamage(enemy, damage, effect.damageType, false, context);

        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.Heal(dealt);
    }

    private void ApplyAllInDamage(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        int manaDamage = Mathf.RoundToInt(effect.value * context.manaSpent);

        if (effect.scaleValueWithMiniGame && context.miniGameResult != null)
            manaDamage = Mathf.RoundToInt(manaDamage * context.miniGameResult.multiplier);

        int finalDamage = cardData.baseDamage + temporarySkillValueBonus + manaDamage;

        if (enemy.isBoss)
            finalDamage = Mathf.RoundToInt(finalDamage * effect.bossValueMultiplier);

        DealEnemyDamage(enemy, CalculateSkillDamage(finalDamage, effect.damageType), effect.damageType, false, context);
    }

    private void ApplyFlee(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        if (enemy == null || enemy.isBoss)
            return;

        enemy.TryFlee(1f);
    }

    private void ApplyRandomStatus(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        if (enemy == null)
            return;

        EffectType[] normalStatuses =
        {
            EffectType.Poison,
            EffectType.Burn,
            EffectType.Bleed,
            EffectType.Weakened,
            EffectType.Softened,
            EffectType.Vulnerable,
            EffectType.Dazed,
            EffectType.Blind
        };

        EffectType[] bossSafeStatuses =
        {
            EffectType.Poison,
            EffectType.Burn,
            EffectType.Bleed,
            EffectType.Leech,
            EffectType.Volatile
        };

        EffectType[] pool = enemy.isBoss ? bossSafeStatuses : normalStatuses;
        EffectType picked = pool[Random.Range(0, pool.Length)];

        int value = CalculateStatusValue(effect, enemy, context);
        int duration = Mathf.Max(1, CalculateStatusDuration(effect, enemy, context));

        EnemyStatusController.GetOrAdd(enemy).ApplyEffect(enemy, picked, value, effect.secondaryValue, duration, effect.damageType, false);
    }

    private void ApplyStatus(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        if (enemy == null)
            return;

        int value = CalculateStatusValue(effect, enemy, context);
        int duration = CalculateStatusDuration(effect, enemy, context);

        if (duration <= 0 && RequiresDuration(effect.effectType))
            duration = 1;

        EnemyStatusController.GetOrAdd(enemy).ApplyEffect(
            enemy,
            effect.effectType,
            value,
            effect.secondaryValue,
            duration,
            effect.damageType,
            effect.removeWhenDamaged
        );
    }

    private void TryApplyQuickStatus(Enemy enemy, EffectType status, int value, int secondaryValue, int duration, bool removeWhenDamaged)
    {
        if (enemy == null)
            return;

        EnemyStatusController.GetOrAdd(enemy).ApplyEffect(enemy, status, value, secondaryValue, duration, CardDamageType.Magic, removeWhenDamaged);
    }

    private void ApplyPlayerOrCardEffect(CardEffect effect, SkillCastContext context)
    {
        int value = CalculateSupportValue(effect, context);
        int duration = Mathf.Max(0, effect.GetDurationRoll() + effect.GetMiniGameBonusTurns(context.miniGameResult));

        switch (effect.effectType)
        {
            case EffectType.Heal:
                if (PlayerHealth.Instance != null)
                    PlayerHealth.Instance.Heal(value);
                break;

            case EffectType.Shield:
                if (PlayerShield.Instance != null)
                    PlayerShield.Instance.AddShield(value);
                break;

            case EffectType.Cleanse:
                CleansePlayer(effect, context);
                break;

            case EffectType.CleanseAll:
                CleanseAllPlayerAndEnemies();
                break;

            case EffectType.Regeneration:
                if (BuffManager.Instance != null)
                    BuffManager.Instance.ApplyRegeneration(value, Mathf.Max(1, duration));
                break;

            case EffectType.FullHeal:
                if (PlayerHealth.Instance != null)
                    PlayerHealth.Instance.FullHeal();
                break;

            case EffectType.PhysicalBuff:
                if (BuffManager.Instance != null)
                    BuffManager.Instance.ApplyPhysicalBuff(value, Mathf.Max(1, duration));
                break;

            case EffectType.MagicBuff:
                if (BuffManager.Instance != null)
                    BuffManager.Instance.ApplyMagicBuff(value, Mathf.Max(1, duration));
                break;

            case EffectType.SupportBuff:
                if (BuffManager.Instance != null)
                    BuffManager.Instance.ApplySupportBuff(value, Mathf.Max(1, duration));
                break;

            case EffectType.CommonBuff:
            case EffectType.ExtremeBuff:
                if (BuffManager.Instance != null)
                    BuffManager.Instance.ApplyAllBuff(value, Mathf.Max(1, duration));
                break;

            case EffectType.ReturnMana:
                if (ManaManager.Instance != null)
                    ManaManager.Instance.AddMana(value);
                break;

            case EffectType.CloneLeft:
                CloneRelativeCard(true);
                break;

            case EffectType.CloneRight:
                CloneRelativeCard(false);
                break;
        }
    }

    private void CleansePlayer(CardEffect effect, SkillCastContext context)
    {
        int count = Mathf.Max(1, effect.value);

        if (context.miniGameResult != null && context.miniGameResult.IsGreatOrPerfect())
            count = Mathf.Max(count, 2);

        SendPlayerStatusMessage("CleanseSome", count);
        SendPlayerStatusMessage("RemoveSomePlayerDebuffs", count);
        SendPlayerStatusMessage("Cleanse", count);
    }

    private void CleanseAllPlayerAndEnemies()
    {
        SendPlayerStatusMessage("CleanseAll");
        SendPlayerStatusMessage("RemoveAllPlayerDebuffs");

        List<Enemy> enemies = GetAliveEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyStatusController controller = EnemyStatusController.Get(enemies[i]);

            if (controller != null)
                controller.RemoveAllNegativeEffects();
        }
    }

    private void CloneRelativeCard(bool left)
    {
        if (HandManager.Instance == null)
            return;

        Card targetCard = left
            ? HandManager.Instance.GetPreviousCard(this)
            : HandManager.Instance.GetNextCard(this);

        if (targetCard == null)
            return;

        HandManager.Instance.CloneCardAfterCurrent(targetCard);
    }

    private int CalculateSkillValue(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        int value = effect.GetBaseValueRoll() + temporarySkillValueBonus;

        if (context.miniGameResult != null && context.miniGameResult.grade == MiniGameGrade.Perfect && effect.perfectValueOverride > 0)
            value = effect.perfectValueOverride;

        if (effect.scaleValueWithMiniGame && context.miniGameResult != null)
            value = Mathf.RoundToInt(value * context.miniGameResult.multiplier);

        if (effect.addBaseDamageToValue && cardData != null)
            value += cardData.baseDamage;

        if (enemy != null && enemy.isBoss)
            value = Mathf.RoundToInt(value * effect.bossValueMultiplier);

        return CalculateSkillDamage(Mathf.Max(0, value), effect.damageType);
    }

    private int CalculateStatusValue(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        int value = effect.GetBaseValueRoll() + temporarySkillValueBonus;

        if (effect.scaleValueWithMiniGame && context.miniGameResult != null)
            value = Mathf.RoundToInt(value * context.miniGameResult.multiplier);

        if (enemy != null && enemy.isBoss)
            value = Mathf.RoundToInt(value * effect.bossValueMultiplier);

        return Mathf.Max(0, value);
    }

    private int CalculateStatusDuration(CardEffect effect, Enemy enemy, SkillCastContext context)
    {
        int duration = effect.GetDurationRoll() + effect.GetMiniGameBonusTurns(context.miniGameResult);

        if (enemy != null && enemy.isBoss)
            duration = Mathf.CeilToInt(duration * effect.bossDurationMultiplier);

        return Mathf.Max(0, duration);
    }

    private int CalculateSupportValue(CardEffect effect, SkillCastContext context)
    {
        int value = effect.GetBaseValueRoll() + temporarySkillValueBonus;

        if (context.miniGameResult != null && context.miniGameResult.grade == MiniGameGrade.Perfect && effect.perfectValueOverride > 0)
            value = effect.perfectValueOverride;

        if (effect.scaleValueWithMiniGame && context.miniGameResult != null)
            value = Mathf.RoundToInt(value * context.miniGameResult.multiplier);

        if (effect.addBaseDamageToValue && cardData != null)
            value += cardData.baseDamage;

        if (effect.secondaryValue > 0)
            value += effect.secondaryValue;

        if (BuffManager.Instance != null)
            value = Mathf.RoundToInt(value * BuffManager.Instance.GetSupportMultiplier());

        return Mathf.Max(0, value);
    }

    private int CalculateBasicDamage(int baseValue, CardDamageType damageType)
    {
        float multiplier = BuffManager.Instance == null ? 1f : BuffManager.Instance.GetDamageMultiplier(damageType);
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * multiplier));
    }

    private int CalculateSkillDamage(int baseValue, CardDamageType damageType)
    {
        float multiplier = BuffManager.Instance == null ? 1f : BuffManager.Instance.GetDamageMultiplier(damageType);
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * multiplier));
    }

    private int DealEnemyDamage(Enemy enemy, int damage, CardDamageType damageType, bool ignoreShield, SkillCastContext context)
    {
        if (enemy == null || damage <= 0)
            return 0;

        bool trueDamage = damageType == CardDamageType.True;
        int finalDamage = damage;

        EnemyStatusController controller = EnemyStatusController.Get(enemy);

        if (controller != null)
            finalDamage = controller.ModifyIncomingDamage(enemy, finalDamage, damageType, trueDamage);

        int beforeHP = enemy.GetCurrentHP();
        int dealt = enemy.TakeDamage(finalDamage, damageType, ignoreShield || trueDamage);
        int afterHP = enemy.GetCurrentHP();

        if (controller != null)
            controller.NotifyDamaged(enemy, dealt);

        if (context != null)
        {
            int hpDamage = Mathf.Max(0, beforeHP - afterHP);
            context.totalDamageDealt += hpDamage;
            context.lastDamageDealt = hpDamage;

            if (beforeHP > 0 && afterHP <= 0)
                context.killedEnemy = true;
        }

        return dealt;
    }

    private void ApplySplitBasicDamage(List<Enemy> enemies, int totalDamage, CardDamageType damageType)
    {
        if (enemies == null || enemies.Count == 0 || totalDamage <= 0)
            return;

        enemies = enemies
            .Where(enemy => enemy != null && enemy.GetCurrentHP() > 0)
            .Distinct()
            .ToList();

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

            DealEnemyDamage(enemies[i], finalDamage, damageType, false, null);
        }
    }

    private bool IsPlayerEffect(EffectType type)
    {
        switch (type)
        {
            case EffectType.Heal:
            case EffectType.Shield:
            case EffectType.Cleanse:
            case EffectType.CleanseAll:
            case EffectType.Regeneration:
            case EffectType.FullHeal:
            case EffectType.PhysicalBuff:
            case EffectType.MagicBuff:
            case EffectType.SupportBuff:
            case EffectType.CommonBuff:
            case EffectType.ExtremeBuff:
            case EffectType.ReturnMana:
                return true;
        }

        return false;
    }

    private bool IsCardManipulationEffect(EffectType type)
    {
        return type == EffectType.CloneLeft || type == EffectType.CloneRight;
    }

    private bool RequiresDuration(EffectType type)
    {
        return EnemyStatusController.IsEnemyStatusEffect(type) && type != EffectType.Marked && type != EffectType.Exposed;
    }

    private List<Enemy> GetAliveEnemies()
    {
        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        return enemies
            .Where(enemy => enemy != null && enemy.GetCurrentHP() > 0)
            .ToList();
    }

    private void SendPlayerStatusMessage(string methodName)
    {
        if (PlayerHealth.Instance == null)
            return;

        PlayerHealth.Instance.gameObject.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
    }

    private void SendPlayerStatusMessage(string methodName, object value)
    {
        if (PlayerHealth.Instance == null)
            return;

        PlayerHealth.Instance.gameObject.SendMessage(methodName, value, SendMessageOptions.DontRequireReceiver);
    }

    private void RefreshVisualColor()
    {
        if (!isActive)
        {
            SetRendererColor(Color.grey);
            return;
        }

        if (isCastingSkill)
        {
            SetRendererColor(Color.cyan);
            return;
        }

        if (isWaitingForSkillTarget)
        {
            SetRendererColor(Color.yellow);
            return;
        }

        SetRendererColor(originalColor);
    }

    private void SetRendererColor(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;
        else if (meshRenderer != null)
            meshRenderer.material.color = color;
    }

    private void FlashNoMana()
    {
        Color noManaColor = new Color(1f, 0.25f, 0.25f, 1f);
        SetRendererColor(noManaColor);

        DOVirtual.DelayedCall(0.15f, () =>
        {
            if (this != null)
                RefreshVisualColor();
        });
    }
}
