using UnityEngine;
using System.Collections.Generic;

// Stores icons for effect types.
[CreateAssetMenu(menuName = "Combat/Effect Database")]
public class EffectDatabase : ScriptableObject
{
    // Effects assigned in the Inspector.
    public List<EffectEntry> effects;

    // Fast lookup for effect icons.
    private Dictionary<EffectType, EffectEntry> lookup;

    // Builds the lookup.
    public void Init()
    {
        lookup = new Dictionary<EffectType, EffectEntry>();

        foreach (var e in effects)
        {
            if (e == null)
                continue;

            if (!lookup.ContainsKey(e.type))
                lookup.Add(e.type, e);
        }
    }

    // Gets the icon for an effect type.
    public Sprite GetIcon(EffectType type)
    {
        if (lookup == null)
            Init();

        if (lookup.TryGetValue(type, out var entry))
            return entry.icon;

        return null;
    }
}

// Connects an effect type to an icon.
[System.Serializable]
public class EffectEntry
{
    public EffectType type;
    public Sprite icon;
}