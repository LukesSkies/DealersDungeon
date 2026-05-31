using System;

// Runtime copy of a CardEffect that is currently active on an enemy.
[Serializable]
public class ActiveEffect
{
    public EffectType type;
    public int value;
    public int secondaryValue;
    public int duration;
    public int stacks;
    public CardDamageType damageType;

    public ActiveEffect(CardEffect effect, float sourceCardManaCost)
    {
        type = effect.effectType;
        value = effect.value;
        secondaryValue = effect.secondaryValue;
        duration = effect.GetRolledDuration(sourceCardManaCost);
        stacks = 1;
        damageType = effect.damageType;
    }
}
