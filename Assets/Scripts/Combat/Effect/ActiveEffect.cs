using System;

[Serializable]
public class ActiveEffect
{
    public EffectType type;
    public int value;
    public int duration;

    public ActiveEffect(CardEffect effect)
    {
        type = effect.effectType;
        value = effect.value;
        duration = effect.duration;
    }
}