using System;

// Runtime version of a CardEffect placed on an enemy.
[Serializable]
public class ActiveEffect
{
    public EffectType type;
    public int value;
    public int secondaryValue;
    public int duration;
    public int stacks;
    public CardDamageType damageType;
    public bool removeWhenDamaged;

    public ActiveEffect(EffectType type, int value, int secondaryValue, int duration, CardDamageType damageType, bool removeWhenDamaged)
    {
        this.type = type;
        this.value = value;
        this.secondaryValue = secondaryValue;
        this.duration = duration;
        this.damageType = damageType;
        this.removeWhenDamaged = removeWhenDamaged;
        stacks = 1;
    }
}
