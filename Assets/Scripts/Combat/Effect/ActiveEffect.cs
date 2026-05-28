using System;

// This represents an effect currently active on an enemy.
[Serializable]
public class ActiveEffect
{
    // The type of effect currently active.
    public EffectType type;

    // The effect value.
    //
    // For Poison/Burn/Bleed, this is usually damage per tick.
    // For other effects, this can mean different things depending on the effect.
    public int value;

    // How many turns/duration this effect has left.
    public int duration;

    // Creates an ActiveEffect from a CardEffect.
    //
    // This copies the important runtime data from the card effect
    // into the enemy's active effect list.
    public ActiveEffect(CardEffect effect)
    {
        type = effect.effectType;
        value = effect.value;
        duration = effect.duration;
    }
}