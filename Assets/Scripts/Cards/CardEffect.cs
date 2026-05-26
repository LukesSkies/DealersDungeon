using System;
using UnityEngine;

[Serializable]
public class CardEffect
{
    public EffectType effectType;

    public int value;
    public int duration;

    public bool targetAllEnemies;

    [Range(0f, 1f)]
    public float chance = 1f;

    public EffectCondition condition = EffectCondition.None;
}