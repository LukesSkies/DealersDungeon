using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;

    private readonly List<PlayerBuff> activeBuffs = new List<PlayerBuff>();

    private void Awake()
    {
        Instance = this;
    }

    public void ApplyDamageBuff(float multiplier, int turns)
    {
        // Backwards compatible with your old cards.
        // multiplier 2 = double damage.
        ApplyBuff(EffectType.DamageBuff, multiplier, turns);
    }

    public void ApplyBuff(EffectType type, float value, int turns)
    {
        if (turns <= 0)
            turns = 1;

        PlayerBuff existing = activeBuffs.Find(buff => buff.type == type);

        if (existing != null)
        {
            existing.value = value;
            existing.turnsRemaining = Mathf.Max(existing.turnsRemaining, turns);
        }
        else
        {
            activeBuffs.Add(new PlayerBuff(type, value, turns));
        }
    }

    public float GetDamageMultiplier()
    {
        return GetDamageMultiplier(CardDamageType.Physical);
    }

    public float GetDamageMultiplier(CardDamageType damageType)
    {
        float multiplier = 1f;

        foreach (PlayerBuff buff in activeBuffs)
        {
            switch (buff.type)
            {
                case EffectType.DamageBuff:
                    multiplier *= buff.value;
                    break;

                case EffectType.AttackBuff:
                    if (damageType == CardDamageType.Physical)
                        multiplier *= PercentToMultiplier(buff.value);
                    break;

                case EffectType.SpellDamageBuff:
                    if (damageType == CardDamageType.Spell)
                        multiplier *= PercentToMultiplier(buff.value);
                    break;

                case EffectType.CriticalBuff:
                    float critChance = Mathf.Clamp01(buff.value / 100f);
                    if (UnityEngine.Random.value <= critChance)
                        multiplier *= 2f;
                    break;

                case EffectType.CriticalDamageBuff:
                    // This only matters if CriticalBuff also triggered elsewhere.
                    // Kept simple for now: this acts like bonus damage.
                    multiplier *= PercentToMultiplier(buff.value);
                    break;
            }
        }

        return multiplier;
    }

    public float ModifyManaCost(float baseCost)
    {
        float finalCost = baseCost;

        foreach (PlayerBuff buff in activeBuffs)
        {
            if (buff.type == EffectType.CostReduction)
            {
                // value is treated as flat reduction.
                // Example: value 1 makes a 3-cost spell cost 2.
                finalCost -= buff.value;
            }
        }

        return Mathf.Max(0f, finalCost);
    }

    public float ModifyManaGain(float baseGain)
    {
        float finalGain = baseGain;

        foreach (PlayerBuff buff in activeBuffs)
        {
            if (buff.type == EffectType.ManaGainBuff)
                finalGain += buff.value;
        }

        return Mathf.Max(0f, finalGain);
    }

    public float GetDefenseMultiplier()
    {
        float multiplier = 1f;

        foreach (PlayerBuff buff in activeBuffs)
        {
            if (buff.type == EffectType.DefenseBuff || buff.type == EffectType.Guard)
            {
                // value is percent damage reduction.
                // Example: 25 means take 25% less damage.
                multiplier *= Mathf.Clamp01(1f - buff.value / 100f);
            }
        }

        return multiplier;
    }

    public float GetEvasionChance()
    {
        float chance = 0f;

        foreach (PlayerBuff buff in activeBuffs)
        {
            if (buff.type == EffectType.EvasionBuff || buff.type == EffectType.DodgeBuff || buff.type == EffectType.Invisibility)
                chance += buff.value / 100f;
        }

        return Mathf.Clamp01(chance);
    }

    public bool HasBuff(EffectType type)
    {
        return activeBuffs.Exists(buff => buff.type == type);
    }

    public void ClearBuff(EffectType type)
    {
        activeBuffs.RemoveAll(buff => buff.type == type);
    }

    public void ClearAllBuffs()
    {
        activeBuffs.Clear();
    }

    // Call this once at the end of the full round, after enemies act.
    public void OnTurnEnd()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].turnsRemaining--;

            if (activeBuffs[i].turnsRemaining <= 0)
                activeBuffs.RemoveAt(i);
        }
    }

    private float PercentToMultiplier(float value)
    {
        // If the designer enters 2, treat it as x2 for compatibility.
        // If they enter 25, treat it as +25%.
        if (value > 0f && value <= 5f)
            return value;

        return 1f + value / 100f;
    }
}

[Serializable]
public class PlayerBuff
{
    public EffectType type;
    public float value;
    public int turnsRemaining;

    public PlayerBuff(EffectType type, float value, int turnsRemaining)
    {
        this.type = type;
        this.value = value;
        this.turnsRemaining = turnsRemaining;
    }
}
