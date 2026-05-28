// This enum lists every possible card/effect type in the combat system.
//
// CardEffect uses this to decide what kind of effect a card applies.
// Enemy and Card scripts then check these values to run the correct behaviour.
public enum EffectType
{
    // No effect.
    //
    // Used as a safe/default value.
    None,

    // -------------------------
    // Damage effects
    // -------------------------

    // Deals direct damage.
    Damage,

    // Area-of-effect damage.
    //
    // In the current Card script, this works similarly to Damage,
    // but because effects are looped through all enemies,
    // it effectively damages multiple enemies.
    AOE,

    // Intended for splitting damage between enemies.
    //
    // This exists in the enum, but needs code support in Card.ApplyEffect()
    SplitDamage,

    // Deals half of the target enemy's current HP as damage.
    HalfHP,

    // -------------------------
    // Damage-over-time effects
    // -------------------------

    // Applies poison.
    //
    // Enemy.ProcessEffects() makes Poison deal damage each turn.
    Poison,

    // Applies burn.
    //
    // Enemy.ProcessEffects() makes Burn deal damage each turn.
    Burn,

    // Applies bleed.
    //
    // Enemy.ProcessEffects() makes Bleed deal damage each turn.
    Bleed,

    // -------------------------
    // Control effects
    // -------------------------

    // Prevents the enemy from attacking while active.
    Stun,

    // Prevents the enemy from attacking while active.
    Sleep,

    // Intended to confuse the enemy.
    //
    // This exists in the enum, but needs behaviour code
    // confusion makes eenemies attack themselevs or others on their side.
    Confusion,

    // Intended to scare the enemy.
    //
    // This exists in the enum, but needs behaviour code
    // fear causes enemies to possibly run away
    Fear,

    // -------------------------
    // Player buff/support effects
    // -------------------------

    // Applies a temporary player damage multiplier.
    DamageBuff,

    // Gives the player shield.
    Shield,

    // Heals the player.
    Heal,

    // Intended to heal the player based on damage dealt.
    //
    // This exists in the enum, but needs behaviour code
    Lifesteal,

    // -------------------------
    // Enemy debuff effects
    // -------------------------

    // Applies cripple to the enemy.
    //
    // This exists in the enum, but needs behaviour code
    // cripple makes enemies take more damage if they are already hurt
    Cripple,

    // Applies Vulnerable
    //
    // This exists in the enum, but needs behaviour code
    // intended to make the enemy take more damage.
    Vulnerable,

    // -------------------------
    // Special effects
    // -------------------------

    // Gives the enemy a chance to flee/die.
    //
    // Bosses ignore this in Enemy.TryFlee().
    Flee,

    // Deals bonus damage based on existing Poison, Burn, and Bleed.
    DetonateDOT
}