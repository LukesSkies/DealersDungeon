using System;
using UnityEngine;

// Stores one effect inside a CardData asset.
[Serializable]
public class CardEffect
{
    [Header("Effect")]
    public EffectType effectType = EffectType.None;

    [Tooltip("UseCardTarget means this effect uses CardData.targetType.")]
    public CardTargetType targetOverride = CardTargetType.UseCardTarget;

    public CardDamageType damageType = CardDamageType.Magic;

    [Header("Numbers")]
    [Tooltip("Main number. Damage, healing, shield, DOT tick damage, buff percent, mana return, flee attempts, etc.")]
    public int value = 0;

    [Tooltip("Extra number. Common uses: chance percent, burst damage, percent value, continuation chance, adjacent percent.")]
    public int secondaryValue = 0;

    [Tooltip("Turns this effect lasts. Instant effects should be 0.")]
    public int duration = 0;

    [Header("Skill Damage Options")]
    [Tooltip("Adds CardData.baseDamage to this skill damage after mini-game scaling.")]
    public bool addBaseDamageToValue = false;

    [Tooltip("Multiplies this effect's value by the mini-game multiplier.")]
    public bool scaleValueWithMiniGame = true;

    [Tooltip("Adds extra turns based on the mini-game grade. OK +0, Good +1, Perfect +2.")]
    public bool addMiniGameBonusTurns = false;

    [Header("Random Hits / Values")]
    public int hitCount = 1;
    public int randomMinHits = 0;
    public int randomMaxHits = 0;
    public int randomMinValue = 0;
    public int randomMaxValue = 0;
    public int randomMinDuration = 0;
    public int randomMaxDuration = 0;

    [Header("Mini Game Grade Bonuses")]
    public int bonusTurnsOnGreatOrPerfect = 0;
    public int bonusTurnsOnPerfect = 0;
    public int perfectValueOverride = 0;

    [Header("Chance")]
    [Range(0f, 1f)] public float chance = 1f;

    [Tooltip("Applied when target is a boss. 0.5 means half chance on bosses.")]
    public float bossChanceMultiplier = 1f;

    [Header("Status On Hit")]
    [Tooltip("Optional status applied after each hit from this effect.")]
    public EffectType statusAppliedOnHit = EffectType.None;

    [Range(0f, 1f)] public float statusChanceOnHit = 0f;
    public int statusValueOnHit = 0;
    public int statusSecondaryValueOnHit = 0;
    public int statusDurationOnHit = 0;
    public bool statusWorksOnBosses = true;
    public float statusBossChanceMultiplier = 1f;
    public float statusBossDurationMultiplier = 1f;

    [Header("Boss Rules")]
    [Tooltip("Controls whether this exact effect works on bosses. Damage and status can be split into separate effects.")]
    public bool worksOnBosses = true;

    [Tooltip("Applied to value when target is a boss. Example: Doomed burst uses 0.5.")]
    public float bossValueMultiplier = 1f;

    [Tooltip("Applied to duration when target is a boss. Example: Poison can use 0.5.")]
    public float bossDurationMultiplier = 1f;

    [Header("Special")]
    [Tooltip("For status effects that wake when damaged, such as Sleep.")]
    public bool removeWhenDamaged = false;

    [TextArea(2, 4)] public string designerNote;

    public CardTargetType GetTargetType(CardData owner)
    {
        if (targetOverride == CardTargetType.UseCardTarget && owner != null)
            return owner.targetType;

        return targetOverride;
    }

    public int GetHitCount()
    {
        if (randomMaxHits > 0 && randomMaxHits >= randomMinHits)
            return UnityEngine.Random.Range(Mathf.Max(1, randomMinHits), randomMaxHits + 1);

        return Mathf.Max(1, hitCount);
    }

    public int GetBaseValueRoll()
    {
        if (randomMaxValue > 0 && randomMaxValue >= randomMinValue)
            return UnityEngine.Random.Range(randomMinValue, randomMaxValue + 1);

        return value;
    }

    public int GetDurationRoll()
    {
        if (randomMaxDuration > 0 && randomMaxDuration >= randomMinDuration)
            return UnityEngine.Random.Range(Mathf.Max(1, randomMinDuration), randomMaxDuration + 1);

        return duration;
    }

    public int GetMiniGameBonusTurns(MiniGameResult miniGameResult)
    {
        if (miniGameResult == null)
            return 0;

        int bonus = 0;

        if (addMiniGameBonusTurns)
        {
            if (miniGameResult.grade == MiniGameGrade.Perfect)
                bonus += 2;
            else if (miniGameResult.grade == MiniGameGrade.Good)
                bonus += 1;
        }

        if (miniGameResult.grade == MiniGameGrade.Good || miniGameResult.grade == MiniGameGrade.Perfect)
            bonus += bonusTurnsOnGreatOrPerfect;

        if (miniGameResult.grade == MiniGameGrade.Perfect)
            bonus += bonusTurnsOnPerfect;

        return bonus;
    }
}
