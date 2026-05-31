using UnityEngine;

public enum CardType
{
    Attack,
    Support,
    Healer,
    Control,
    Utility
}

public enum CardPlayMode
{
    // Card can basic tap/drag attack, but clicking the card does not cast a spell.
    BasicAttackOnly,

    // Clicking the card immediately casts its effects.
    SpellOnCardClick,

    // Clicking the card arms the spell, then the player must click an enemy.
    SpellRequiresEnemyTarget
}

public enum CardDamageType
{
    Physical,
    Spell,
    True
}

public enum TargetType
{
    None,

    // Enemy targeting.
    SingleEnemy,
    AllEnemies,
    RandomEnemy,
    RandomEnemies,
    LowestHPEnemy,
    HighestHPEnemy,
    EnemyWithStatus,
    EnemyWithoutStatus,
    DraggedEnemies,

    // Player targeting.
    Self,

    // Card/hand targeting.
    CurrentCard,
    PreviousCard,
    NextCard,
    RandomCardInHand
}

public enum EffectType
{
    None,

    // -------------------------
    // Core damage effects
    // -------------------------
    Damage,
    AOE,
    SplitDamage,
    HalfHP,
    SplashDamage,
    ChainDamage,
    RandomHits,
    MultiHit,
    PiercingDamage,
    TrueDamage,
    RecoilDamage,
    OverkillDamage,
    ExecuteDamage,
    OpeningStrike,

    // -------------------------
    // Damage-over-time and status effects
    // -------------------------
    Poison,
    Burn,
    Bleed,
    Blind,
    Curse,
    Petrify,
    Shock,
    Marked,
    Exposed,
    Weakened,
    Softened,
    Hexed,
    Doomed,
    Silenced,
    Dazed,
    Charmed,
    Panicked,
    Taunted,
    Stun,
    Sleep,
    Confusion,
    Fear,
    Cripple,
    Vulnerable,

    // -------------------------
    // DoT manipulation
    // -------------------------
    DetonateDOT,
    AmplifyDOT,
    ConsumeDOT,
    PoisonCloud,
    Wildfire,
    Rot,
    LeechDOT,
    VolatileDOT,

    // -------------------------
    // Player healing / recovery
    // -------------------------
    Heal,
    Shield,
    Cleanse,
    CleanseAll,
    CleanseSome,
    Regeneration,
    FullHeal,
    HealFromDamage,
    Lifesteal,

    // -------------------------
    // Player buffs
    // -------------------------
    DamageBuff,
    AttackBuff,
    SpellDamageBuff,
    CostReduction,
    DefenseBuff,
    EvasionBuff,
    ManaGainBuff,
    DrawBuff,
    CriticalBuff,
    CriticalDamageBuff,
    Guard,
    MagicShield,
    PhysicalShield,
    Reflect,
    CounterAttack,
    DodgeBuff,
    Invisibility,

    // -------------------------
    // Enemy debuffs
    // -------------------------
    AttackDebuff,
    DefenseDebuff,
    EvasionDebuff,
    SpellPowerDebuff,
    HealingDebuff,
    BarrierBreak,
    Flee,

    // -------------------------
    // Shield / defensive specials
    // -------------------------
    ShieldOverload,

    // -------------------------
    // Card manipulation
    // -------------------------
    Clone,
    UpgradeCardTemporary,
    ReduceRandomCardCost,
    RefundManaOnKill
}

public enum EffectCondition
{
    None,

    TargetBelowHalfHP,
    TargetAboveHalfHP,
    TargetHasPoison,
    TargetHasBurn,
    TargetHasBleed,
    TargetIsStunned,
    TargetIsBoss,
    TargetIsNotBoss,

    TargetHasStatus,
    TargetDoesNotHaveStatus,
    TargetHasAnyDOT,
    TargetHasNoDOT,
    TargetIsAlive,

    PlayerHasShield,
    PlayerHasNoShield,
    ManaAtLeast
}
