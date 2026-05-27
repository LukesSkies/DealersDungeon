using System;
using UnityEngine;

// This represents one effect on a card.
[Serializable]
public class CardEffect
{
    // The type of effect this card applies.
    //
    // Example:
    // Damage, Heal, Poison, Shield, Stun, etc.
    public EffectType effectType;

    // Main value used by the effect.
    //
    // For Damage, this can be damage amount.
    // For Heal, this can be healing amount.
    // For Shield, this can be shield amount.
    // For Poison/Burn/Bleed, this can be damage per tick.
    public int value;

    // How long the effect lasts.
    //
    // Used by effects like:
    // - Poison
    // - Burn
    // - Bleed
    // - Stun
    // - Sleep
    // - DamageBuff
    //
    // For instant effects like Damage or Heal,
    // this can stay at 0.
    public int duration;

    // If true, this effect targets all enemies.
    //
    // If false, it can be treated as a single-target effect.
    //
    // Note:
    // Currently Card script mostly applies non-player effects
    // by looping through all enemies already.
    // Make stricter targeting rules.
    public bool targetAllEnemies;

    // The chance that this effect happens.
    //
    // 1 = 100% chance.
    // 0.5 = 50% chance.
    // 0 = never happens.
    [Range(0f, 1f)]
    public float chance = 1f;

    // Optional condition that must be true before this effect applies.
    //
    // Example:
    // - only if target is below half HP
    // - only if target is poisoned
    // - only if target is a boss
    public EffectCondition condition = EffectCondition.None;
}