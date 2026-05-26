using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;

    private float damageMultiplier = 1f;
    private int turnsRemaining = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void ApplyDamageBuff(float multiplier, int turns)
    {
        damageMultiplier = multiplier;
        turnsRemaining = turns;
    }

    public float GetDamageMultiplier()
    {
        return damageMultiplier;
    }

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