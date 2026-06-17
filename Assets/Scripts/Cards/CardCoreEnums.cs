using UnityEngine;

// Main category for a card.
public enum CardType
{
    Physical,
    Magic,
    Support
}

// Damage type used by basic attacks and card skills.
public enum CardDamageType
{
    Physical,
    Magic,
    True
}

// How the card chooses targets when its skill is cast.
public enum CardTargetType
{
    UseCardTarget,
    SelectedEnemy,
    RandomEnemy,
    RandomNonBossEnemy,
    LowestHPEnemy,
    HighestHPEnemy,
    AdjacentEnemies,
    AllNonBossEnemies,
    AllEnemies,
    Player,
    PreviousCard,
    NextCard
}

// How mana is paid for the skill.
public enum CardManaCostMode
{
    Fixed,
    AllRemaining
}
// What one card effect does.
public enum EffectType
{
    None,

    // Direct damage.
    Damage,
    RandomHits,
    PiercingDamage,
    PercentMissingHPDamage,
    PercentMaxHPDamage,
    AdjacentSplashDamage,
    LifeStealDamage,
    AllInDamage,

    // Enemy status effects.
    Blind,
    Curse,
    Petrify,
    Marked,
    Exposed,
    Weakened,
    Softened,
    Doomed,
    Silenced,
    Dazed,
    Charmed,
    Poison,
    Burn,
    Bleed,
    Stun,
    Sleep,
    Cripple,
    Vulnerable,
    Flee,
    RandomStatus,
    Leech,
    Volatile,
    Wildfire,

    // Player support.
    Heal,
    Shield,
    Cleanse,
    CleanseAll,
    Regeneration,
    FullHeal,
    PhysicalBuff,
    MagicBuff,
    SupportBuff,
    CommonBuff,
    ExtremeBuff,
    ReturnMana,

    // Card manipulation.
    CloneLeft,
    CloneRight
}
