using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName;
    public CardType cardType;

    [Header("Basic Tap / Drag Attack")]
    public bool canTapAttack = true;
    public bool canDragAttack = true;
    public int baseDamage = 0;
    public CardDamageType basicAttackDamageType = CardDamageType.Physical;

    [Header("Spell")]
    public CardPlayMode playMode = CardPlayMode.SpellOnCardClick;
    public float manaCost = 0f;

    [Tooltip("If true, mana is spent when the spell is successfully cast. Recommended true.")]
    public bool spendManaOnSpellCast = true;

    [Header("Effects")]
    public List<CardEffect> effects = new List<CardEffect>();

    public bool HasSpell()
    {
        return playMode != CardPlayMode.BasicAttackOnly && effects != null && effects.Count > 0;
    }

    public bool RequiresEnemyTargetForSpell()
    {
        if (playMode == CardPlayMode.SpellRequiresEnemyTarget)
            return true;

        if (effects == null)
            return false;

        for (int i = 0; i < effects.Count; i++)
        {
            TargetType t = effects[i].targetType;

            if (t == TargetType.SingleEnemy ||
                t == TargetType.EnemyWithStatus ||
                t == TargetType.EnemyWithoutStatus)
            {
                return true;
            }
        }

        return false;
    }
}
