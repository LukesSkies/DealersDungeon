using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Effect Database")]
public class EffectDatabase : ScriptableObject
{
    public List<EffectEntry> effects;

    private Dictionary<EffectType, EffectEntry> lookup;

    public void Init()
    {
        lookup = new Dictionary<EffectType, EffectEntry>();

        foreach (var e in effects)
        {
            if (!lookup.ContainsKey(e.type))
                lookup.Add(e.type, e);
        }
    }

    public Sprite GetIcon(EffectType type)
    {
        if (lookup == null)
            Init();

        if (lookup.TryGetValue(type, out var entry))
            return entry.icon;

        return null;
    }
}

[System.Serializable]
public class EffectEntry
{
    public EffectType type;
    public Sprite icon;
}