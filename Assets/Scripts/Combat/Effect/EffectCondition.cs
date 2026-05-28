// Conditions that can be attached to a CardEffect.
//
// A condition decides whether an effect is allowed to apply.
//
// Example:
// A card might only deal bonus damage if the enemy is already poisoned.
public enum EffectCondition
{
    // No condition.
    //
    // The effect can always apply.
    None,

    // Effect only applies if the target is at or below half HP.
    TargetBelowHalfHP,

    // Effect only applies if the target is above half HP.
    TargetAboveHalfHP,

    // Effect only applies if the target already has Poison.
    TargetHasPoison,

    // Effect only applies if the target already has Burn.
    TargetHasBurn,

    // Effect only applies if the target already has Bleed.
    TargetHasBleed,

    // Effect only applies if the target is stunned.
    //
    // This exists in the enum,
    // needs a case added to make it work.
    TargetIsStunned,

    // Effect only applies if the target is a boss.
    TargetIsBoss
}