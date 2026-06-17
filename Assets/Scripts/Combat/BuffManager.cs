using System;
using System.Collections.Generic;
using UnityEngine;

// Manages player buffs during combat.
public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;

    private readonly List<PlayerBuff> activeBuffs = new List<PlayerBuff>();
    private readonly List<PlayerRegeneration> activeRegenerations = new List<PlayerRegeneration>();

    private void Awake()
    {
        Instance = this;
    }

    public void ApplyDamageBuff(float multiplier, int turns)
    {
        ApplyPhysicalBuff(MultiplierToPercent(multiplier), turns);
    }

    public void ApplyPhysicalBuff(float percent, int turns)
    {
        ApplyBuff(EffectType.PhysicalBuff, percent, turns);
    }

    public void ApplyMagicBuff(float percent, int turns)
    {
        ApplyBuff(EffectType.MagicBuff, percent, turns);
    }

    public void ApplySupportBuff(float percent, int turns)
    {
        ApplyBuff(EffectType.SupportBuff, percent, turns);
    }

    public void ApplyAllBuff(float percent, int turns)
    {
        ApplyBuff(EffectType.CommonBuff, percent, turns);
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

    public void ApplyRegeneration(int healPerTurn, int turns)
    {
        if (healPerTurn <= 0 || turns <= 0)
            return;

        activeRegenerations.Add(new PlayerRegeneration(healPerTurn, turns));
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
            if (buff.type == EffectType.CommonBuff || buff.type == EffectType.ExtremeBuff)
                multiplier *= PercentToMultiplier(buff.value);

            if (damageType == CardDamageType.Physical && buff.type == EffectType.PhysicalBuff)
                multiplier *= PercentToMultiplier(buff.value);

            if (damageType == CardDamageType.Magic && buff.type == EffectType.MagicBuff)
                multiplier *= PercentToMultiplier(buff.value);
        }

        return Mathf.Max(0f, multiplier);
    }

    public float GetSupportMultiplier()
    {
        float multiplier = 1f;

        foreach (PlayerBuff buff in activeBuffs)
        {
            if (buff.type == EffectType.CommonBuff || buff.type == EffectType.ExtremeBuff)
                multiplier *= PercentToMultiplier(buff.value);

            if (buff.type == EffectType.SupportBuff)
                multiplier *= PercentToMultiplier(buff.value);
        }

        return Mathf.Max(0f, multiplier);
    }

    public float ModifyManaCost(float baseCost)
    {
        return Mathf.Max(0f, baseCost);
    }

    public float ModifyManaGain(float baseGain)
    {
        return Mathf.Max(0f, baseGain);
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
        activeRegenerations.Clear();
    }

    // Call this at the start of the player turn.
    public void OnPlayerTurnStart()
    {
        for (int i = activeRegenerations.Count - 1; i >= 0; i--)
        {
            PlayerRegeneration regeneration = activeRegenerations[i];

            if (PlayerHealth.Instance != null)
                PlayerHealth.Instance.Heal(regeneration.healPerTurn);

            regeneration.turnsRemaining--;

            if (regeneration.turnsRemaining <= 0)
                activeRegenerations.RemoveAt(i);
        }
    }

    // Call this at the end of the player turn.
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
        if (value > 0f && value <= 5f)
            return value;

        return 1f + value / 100f;
    }

    private float MultiplierToPercent(float multiplier)
    {
        if (multiplier > 0f && multiplier <= 5f)
            return (multiplier - 1f) * 100f;

        return multiplier;
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

[Serializable]
public class PlayerRegeneration
{
    public int healPerTurn;
    public int turnsRemaining;

    public PlayerRegeneration(int healPerTurn, int turnsRemaining)
    {
        this.healPerTurn = healPerTurn;
        this.turnsRemaining = turnsRemaining;
    }
}
