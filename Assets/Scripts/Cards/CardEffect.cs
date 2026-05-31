using System;
using UnityEngine;

// This is the data for ONE effect inside a CardData asset.
// A single card can have many CardEffects.
[Serializable]
public class CardEffect
{
    [Header("Effect")]
    public EffectType effectType = EffectType.None;

    [Tooltip("Who this effect should target when the card spell is cast.")]
    public TargetType targetType = TargetType.SingleEnemy;

    [Tooltip("Physical, Spell, or True damage. Used by status modifiers such as Softened and Shock.")]
    public CardDamageType damageType = CardDamageType.Spell;

    [Header("Numbers")]
    [Tooltip("Main number. Damage, healing, shield, status tick damage, buff percent, etc.")]
    public int value = 0;

    [Tooltip("Extra number. Examples: splash damage, recoil damage, threshold percent, mana refund, upgrade amount.")]
    public int secondaryValue = 0;

    [Tooltip("How many turns this lasts. For instant effects, leave at 0.")]
    public int duration = 0;

    [Tooltip("For MultiHit / RandomHits / ChainDamage.")]
    public int hitCount = 1;

    [Tooltip("For RandomEnemies.")]
    public int targetCount = 1;

    [Header("Random Duration")]
    [Tooltip("If greater than 0, this is the minimum random duration.")]
    public int randomMinDuration = 0;

    [Tooltip("If greater than randomMinDuration, the effect duration is rolled between min and max.")]
    public int randomMaxDuration = 0;

    [Header("Chance / Conditions")]
    [Range(0f, 1f)]
    public float chance = 1f;

    public EffectCondition condition = EffectCondition.None;

    [Tooltip("Used by TargetHasStatus, TargetDoesNotHaveStatus, EnemyWithStatus, EnemyWithoutStatus, Cleanse, etc.")]
    public EffectType requiredStatus = EffectType.None;

    [Tooltip("Used by ManaAtLeast condition.")]
    public float requiredMana = 0f;

    [Header("Special Behaviour")]
    [Range(0f, 1f)]
    [Tooltip("For ChainDamage. 0.5 means each jump does half as much damage.")]
    public float chainDecay = 0.5f;

    [Tooltip("If true, this effect can affect bosses even when that effect normally should not.")]
    public bool ignoreBossImmunity = false;

    [Tooltip("If true, removes the required status after this effect uses it. Useful for consume-style cards.")]
    public bool removeRequiredStatusAfterUse = false;

    [TextArea(2, 4)]
    public string designerNote;

    public int GetRolledDuration(float cardManaCost)
    {
        int finalMin = randomMinDuration;
        int finalMax = randomMaxDuration;

        // Your rule: Burn lasts a random number of turns, with minimum based on mana cost.
        if (effectType == EffectType.Burn && finalMin <= 0)
            finalMin = Mathf.Max(1, Mathf.CeilToInt(cardManaCost));

        if (finalMax > finalMin)
            return UnityEngine.Random.Range(finalMin, finalMax + 1);

        if (finalMin > 0)
            return finalMin;

        return duration;
    }
}
