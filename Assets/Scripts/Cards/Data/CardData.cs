using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;

    [Header("Base")]
    public int baseDamage;
    public float manaCost;

    [Header("Effects")]
    public List<CardEffect> effects = new List<CardEffect>();
}