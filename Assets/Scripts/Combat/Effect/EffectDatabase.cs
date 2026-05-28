using UnityEngine;
using System.Collections.Generic;

// This ScriptableObject stores the icon for each EffectType.
//
// You can create it from:
// Create -> Combat -> Effect Database
//
// It lets the Enemy UI look up the correct icon for Poison, Burn, Stun, etc.
[CreateAssetMenu(menuName = "Combat/Effect Database")]
public class EffectDatabase : ScriptableObject
{
    // List of effect entries assigned in the Inspector.
    //
    // Each entry connects:
    // EffectType -> Sprite icon
    public List<EffectEntry> effects;

    // Runtime lookup dictionary.
    //
    // This makes GetIcon faster than searching through the list every time.
    private Dictionary<EffectType, EffectEntry> lookup;

    // Builds the lookup dictionary from the effects list.
    public void Init()
    {
        lookup = new Dictionary<EffectType, EffectEntry>();

        foreach (var e in effects)
        {
            // Prevent duplicate keys from causing errors.
            //
            // If two entries use the same EffectType,
            // only the first one is added.
            if (!lookup.ContainsKey(e.type))
                lookup.Add(e.type, e);
        }
    }

    // Returns the icon sprite for a given effect type.
    public Sprite GetIcon(EffectType type)
    {
        // If the lookup has not been built yet, build it now.
        if (lookup == null)
            Init();

        // Try to find the effect entry.
        if (lookup.TryGetValue(type, out var entry))
            return entry.icon;

        // Return null if no icon was found.
        return null;
    }
}

// One entry inside the EffectDatabase.
//
// This connects an EffectType to a Sprite icon.
[System.Serializable]
public class EffectEntry
{
    // The effect type.
    public EffectType type;

    // The icon sprite shown in the UI for this effect.
    public Sprite icon;
}