using UnityEngine;

// This script manages temporary player buffs.
//
// Right now it only handles a damage multiplier.
// Example:
// A card can apply x2 damage for 3 turns.
public class BuffManager : MonoBehaviour
{
    // Singleton reference so other scripts can call:
    // BuffManager.Instance.ApplyDamageBuff(...)
    public static BuffManager Instance;

    // Current damage multiplier.
    //
    // 1 = normal damage.
    // 2 = double damage.
    // 0.5 = half damage.
    private float damageMultiplier = 1f;

    // How many turns the current buff has left.
    private int turnsRemaining = 0;

    private void Awake()
    {
        // Set up singleton reference.
        Instance = this;
    }

    // Applies a damage buff.
    //
    // multiplier = how much damage should be multiplied by.
    // turns = how many turns the buff should last.
    public void ApplyDamageBuff(float multiplier, int turns)
    {
        damageMultiplier = multiplier;
        turnsRemaining = turns;
    }

    // Returns the current damage multiplier.
    //
    // Card.CalculateDamage() uses this when calculating final damage.
    public float GetDamageMultiplier()
    {
        return damageMultiplier;
    }

    // Should be called at the end of a turn.
    //
    // Reduces the buff duration.
    // When duration reaches 0, damage returns to normal.
    public void OnTurnEnd()
    {
        if (turnsRemaining > 0)
        {
            turnsRemaining--;

            if (turnsRemaining <= 0)
                damageMultiplier = 1f;
        }
    }
}