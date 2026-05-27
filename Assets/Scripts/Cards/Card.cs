using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.EventSystems;

// This script controls one card in the player's hand.
//
// It handles:
// - whether the card is currently active
// - hover animation
// - active/inactive visuals
// - clicking the card
// - single-target attacks
// - multi-target attacks
// - card effects
// - mana spending
// - damage buffs
public class Card : MonoBehaviour, IPointerClickHandler
{
    // True if this card is currently the usable/active card.
    //
    // Usually only one card in the hand should be active at a time.
    private bool isActive = false;

    // True if the mouse is currently hovering this card.
    private bool isHovered = false;

    // The normal hand position of this card.
    //
    // The card returns here after hover/use animations.
    private Vector3 basePosition;

    // Used if the card visual is a SpriteRenderer.
    private SpriteRenderer spriteRenderer;

    // Used if the card visual is a MeshRenderer.
    private MeshRenderer meshRenderer;

    // Stores the card's original colour so it can be restored when active.
    private Color originalColor;

    [Header("Scale Settings")]

    // The default size of the card when it is not active.
    [SerializeField] private float baseScale = 0.1f;

    // How much bigger the card becomes when active.
    [SerializeField] private float activeMultiplier = 2f;

    // How much bigger the card becomes when hovered.
    [SerializeField] private float hoverMultiplier = 2.5f;

    // Final calculated normal scale.
    private Vector3 normalScale;

    // Final calculated active scale.
    private Vector3 activeScale;

    // Final calculated hover scale.
    private Vector3 hoverScale;

    [Header("Card Data")]

    // The ScriptableObject that contains this card's stats and effects.
    [SerializeField] private CardData cardData;

    private void Awake()
    {
        // Try to find a SpriteRenderer in this card or its children.
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Try to find a MeshRenderer in this card or its children.
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        // Store the original colour so the card can return to it later.
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else if (meshRenderer != null)
            originalColor = meshRenderer.material.color;

        // Calculate the normal, active, and hover scales.
        UpdateScales();
    }

    // Calculates the card's three scale states.
    private void UpdateScales()
    {
        // Normal inactive size.
        normalScale = Vector3.one * baseScale;

        // Active card size.
        activeScale = normalScale * activeMultiplier;

        // Hovered active card size.
        hoverScale = normalScale * hoverMultiplier;

        // Start the card at normal size.
        transform.localScale = normalScale;
    }

    // Assigns data to this card.
    //
    // HandManager uses this when creating cards from the deck.
    public void SetCardData(CardData data)
    {
        cardData = data;
    }

    // Returns whether this card is currently usable.
    //
    // CardHoverManager uses this so only active cards can be hovered/clicked.
    public bool IsActive()
    {
        return isActive;
    }

    // Sets whether this card is hovered.
    //
    // Hovering only works if the card is active.
    public void SetHovered(bool hovered)
    {
        // Do nothing if the card is inactive.
        //
        // Also do nothing if the hover state is already correct.
        if (!isActive || isHovered == hovered)
            return;

        isHovered = hovered;

        // Stop any existing DOTween animations on this transform.
        //
        // This prevents animations fighting each other.
        transform.DOKill();

        if (hovered)
        {
            // Move the card upward slightly when hovered.
            transform.DOMoveY(basePosition.y + 0.5f, 0.2f);

            // Scale the card to hover size.
            transform.DOScale(hoverScale, 0.2f);
        }
        else
        {
            // Move the card back to its base hand position.
            transform.DOMoveY(basePosition.y, 0.2f);

            // Scale the card back to active size.
            transform.DOScale(activeScale, 0.2f);
        }
    }

    // Sets the card's normal hand position.
    //
    // HandManager calls this when laying out the hand.
    public void SetBasePosition(Vector3 pos)
    {
        basePosition = pos;

        // If the card is not hovered, immediately place it at its base position.
        if (!isHovered)
            transform.position = pos;
    }

    // Sets whether this card is the current active card.
    public void SetActive(bool active)
    {
        isActive = active;

        // Active cards use their normal colour.
        // Inactive cards turn grey.
        if (spriteRenderer != null)
            spriteRenderer.color = active ? originalColor : Color.grey;
        else if (meshRenderer != null)
            meshRenderer.material.color = active ? originalColor : Color.grey;

        // Stop existing scale animations.
        transform.DOKill();

        // Scale up if active, scale down if inactive.
        transform.DOScale(active ? activeScale : normalScale, 0.2f);
    }

    // Marks this card as used.
    //
    // Used cards become inactive, shrink down, return to position,
    // and become darker/transparent.
    public void SetUsed()
    {
        isActive = false;
        isHovered = false;

        // Stop any current animations.
        transform.DOKill();

        // Shrink the card back to normal size.
        transform.DOScale(normalScale, 0.2f);

        // Move the card back to its base position.
        transform.DOMove(basePosition, 0.25f);

        // Darken the card to show it has been used.
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        else if (meshRenderer != null)
            meshRenderer.material.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    }

    // Called when this card is clicked.
    //
    // This is mainly used for cards that apply effects directly,
    // such as healing, shield, damage buffs, or all-enemy effects.
    public void Click()
    {
        // Do nothing if the card is inactive or has no data.
        if (!isActive || cardData == null)
            return;

        // Try to spend mana.
        //
        // If the player does not have enough mana, the card does nothing.
        if (!ManaManager.Instance.TrySpendMana(cardData.manaCost))
            return;

        // Apply all effects listed on the card data.
        ExecuteEffects();

        // Tell the hand this card has been used.
        HandManager.Instance.UseCurrentCard();
    }

    // Unity UI/EventSystem click callback.
    //
    // This allows the card to be clicked through pointer events too.
    public void OnPointerClick(PointerEventData eventData)
    {
        Click();
    }

    // Performs a single-target attack.
    //
    // EnemyTargeting calls this when the player clicks an enemy with a card.
    public void SingleAttack(Enemy target)
    {
        // Do nothing if the card is inactive or there is no target.
        if (!isActive || target == null)
            return;

        // Calculate final damage after buffs.
        int damage = CalculateDamage(cardData.baseDamage);

        // Damage the selected enemy.
        target.TakeDamage(damage);

        // Add mana reward for a single attack.
        ManaManager.Instance.AddMana(1f);

        // Mark the card as used.
        HandManager.Instance.UseCurrentCard();
    }

    // Performs a multi-target attack.
    //
    // EnemyTargeting calls this when the player drags with a card.
    public void MultiAttack()
    {
        // Do nothing if the card is inactive.
        if (!isActive)
            return;

        // Get all currently alive enemies.
        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        // Do nothing if there are no enemies.
        if (enemies.Count == 0)
            return;

        // Calculate total damage after buffs.
        int totalDamage = CalculateDamage(cardData.baseDamage);

        // Split damage evenly across all enemies.
        int damagePerEnemy = totalDamage / enemies.Count;

        // Remainder is used so no damage is lost.
        //
        // Example:
        // 10 damage across 3 enemies:
        // 3, 3, 3 with remainder 1
        // Final becomes 4, 3, 3.
        int remainder = totalDamage % enemies.Count;

        for (int i = 0; i < enemies.Count; i++)
        {
            int finalDamage = damagePerEnemy;

            // Distribute leftover damage one point at a time.
            if (remainder > 0)
            {
                finalDamage++;
                remainder--;
            }

            enemies[i].TakeDamage(finalDamage);
        }

        // Add smaller mana reward for a multi-attack.
        ManaManager.Instance.AddMana(0.5f);

        // Mark the card as used.
        HandManager.Instance.UseCurrentCard();
    }

    // Runs every CardEffect stored on this card's CardData.
    private void ExecuteEffects()
    {
        // Get all currently alive enemies.
        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        foreach (var effect in cardData.effects)
        {
            switch (effect.effectType)
            {
                // Healing affects the player directly.
                case EffectType.Heal:
                    PlayerHealth.Instance.Heal(effect.value);
                    break;

                // Shield affects the player directly.
                case EffectType.Shield:
                    PlayerShield.Instance.AddShield(effect.value);
                    break;

                // Damage buff affects the player through BuffManager.
                case EffectType.DamageBuff:
                    BuffManager.Instance.ApplyDamageBuff(effect.value, effect.duration);
                    break;

                // All other effects are applied to enemies.
                default:
                    foreach (var enemy in enemies)
                    {
                        // Skip this enemy if the effect condition is not met.
                        if (!CheckCondition(effect, enemy))
                            continue;

                        ApplyEffect(effect, enemy);
                    }
                    break;
            }
        }
    }

    // Checks whether an effect is allowed to apply to a specific enemy.
    private bool CheckCondition(CardEffect effect, Enemy enemy)
    {
        switch (effect.condition)
        {
            // No condition means the effect always applies.
            case EffectCondition.None:
                return true;

            // Only applies if the target is at or below half HP.
            case EffectCondition.TargetBelowHalfHP:
                return enemy.GetCurrentHP() <= enemy.maxHP / 2;

            // Only applies if the target is above half HP.
            case EffectCondition.TargetAboveHalfHP:
                return enemy.GetCurrentHP() > enemy.maxHP / 2;

            // Only applies if the target already has Poison.
            case EffectCondition.TargetHasPoison:
                return enemy.HasEffect(EffectType.Poison);

            // Only applies if the target already has Burn.
            case EffectCondition.TargetHasBurn:
                return enemy.HasEffect(EffectType.Burn);

            // Only applies if the target already has Bleed.
            case EffectCondition.TargetHasBleed:
                return enemy.HasEffect(EffectType.Bleed);

            // Only applies if the target is a boss.
            case EffectCondition.TargetIsBoss:
                return enemy.isBoss;
        }

        // Default to true so new/unhandled conditions do not break cards.
        return true;
    }

    // Applies one effect to one enemy.
    private void ApplyEffect(CardEffect effect, Enemy enemy)
    {
        switch (effect.effectType)
        {
            // Direct damage.
            case EffectType.Damage:
                enemy.TakeDamage(CalculateDamage(effect.value));
                break;

            // Area damage.
            //
            // Since ExecuteEffects already loops through enemies,
            // this currently damages each enemy it is applied to.
            case EffectType.AOE:
                enemy.TakeDamage(CalculateDamage(effect.value));
                break;

            // Deals half of the enemy's current HP.
            case EffectType.HalfHP:
                enemy.TakeDamage(enemy.GetCurrentHP() / 2);
                break;

            // Status effects that get stored on the enemy.
            case EffectType.Poison:
            case EffectType.Burn:
            case EffectType.Bleed:
            case EffectType.Stun:
            case EffectType.Sleep:
            case EffectType.Confusion:
            case EffectType.Cripple:
                enemy.ApplyEffect(effect);
                break;

            // Gives the enemy a chance to flee.
            case EffectType.Flee:
                enemy.TryFlee(effect.chance);
                break;

            // Detonates existing damage-over-time effects.
            case EffectType.DetonateDOT:
                Detonate(enemy);
                break;
        }
    }

    // Calculates final damage after player damage buffs.
    private int CalculateDamage(int baseValue)
    {
        float multiplier = BuffManager.Instance.GetDamageMultiplier();
        return Mathf.RoundToInt(baseValue * multiplier);
    }

    // Deals bonus damage based on existing DoT effects on the enemy.
    //
    // Poison adds 2.
    // Burn adds 2.
    // Bleed adds 3.
    //
    // The total bonus is then doubled.
    private void Detonate(Enemy enemy)
    {
        int bonus = 0;

        if (enemy.HasEffect(EffectType.Poison))
            bonus += 2;

        if (enemy.HasEffect(EffectType.Burn))
            bonus += 2;

        if (enemy.HasEffect(EffectType.Bleed))
            bonus += 3;

        enemy.TakeDamage(bonus * 2);
    }
}