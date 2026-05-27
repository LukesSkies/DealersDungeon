using UnityEngine;
using System.Collections.Generic;

// This creates a card data asset in Unity.
//
// Right-click in the Project window:
// Create -> Cards -> Card Data
//
// Each CardData asset represents one card type.
// Example:
// - Strike
// - Poison Shot
// - Heal
// - Shield Up
// - Fire Blast
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    // The display name of the card.
    //
    // Example:
    // "Quick Shot"
    // "Poison Flask"
    // "Blood Bargain"
    public string cardName;

    // The broad category of this card.
    //
    // Example:
    // Attack, Support, or Healer.
    public CardType cardType;

    [Header("Base")]

    // The card's base damage.
    //
    // This is used by attacks before buffs are applied.
    public int baseDamage;

    // How much mana this card costs to play.
    //
    // Your Card script checks ManaManager before allowing
    // the card to execute.
    public float manaCost;

    [Header("Effects")]

    // List of extra effects this card applies.
    //
    // A card can have no effects, one effect, or many effects.
    //
    // Example card:
    // - baseDamage = 5
    // - effects:
    //   - Poison, value 2, duration 3
    //
    // Another example:
    // - baseDamage = 0
    // - effects:
    //   - Heal, value 5
    //   - Shield, value 1
    public List<CardEffect> effects = new List<CardEffect>();
}