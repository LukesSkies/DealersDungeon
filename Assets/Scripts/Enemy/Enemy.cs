using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Splines;

// This script controls one enemy.
//
// It handles:
// - HP
// - taking damage
// - dying
// - attacking the player
// - status effects
// - effect icons
// - boss flag
public class Enemy : MonoBehaviour
{
    [Header("Stats")]

    // The enemy's maximum HP.
    public int maxHP = 10;

    // The enemy's current HP.
    private int currentHP;

    [Header("UI")]

    // Text that displays this enemy's HP.
    [SerializeField] private TMP_Text hpText;

    [Header("Combat")]

    // Damage this enemy deals when it attacks the player.
    public int attackDamage = 2;

    // True if this enemy is a boss.
    //
    // Bosses cannot flee.
    public bool isBoss = false;

    [Header("Effects UI")]

    // Parent object where effect icons are created.
    [SerializeField] private Transform effectContainer;

    // Prefab used to display one active effect icon.
    //
    // This prefab should have EffectIconUI on it.
    [SerializeField] private GameObject effectIconPrefab;

    // Database used to find the correct sprite for each effect type.
    [SerializeField] private EffectDatabase effectDatabase;

    [Header("Optional Layout")]

    // If true, effect icons are arranged along a spline.
    //
    // If false, they are arranged in a simple line.
    [SerializeField] private bool useSplineLayout = false;

    // Optional spline used for arranging effect icons.
    [SerializeField] private SplineContainer splineContainer;

    // List of effects currently active on this enemy.
    //
    // Example:
    // Poison for 3 turns.
    // Stun for 1 turn.
    // Burn for 2 turns.
    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    private void Start()
    {
        // Start the enemy at full HP.
        currentHP = maxHP;

        // Register this enemy with the EnemyManager.
        //
        // EnemyManager needs to know how many enemies are alive.
        EnemyManager.Instance?.RegisterEnemy(this);

        // Build the effect icon lookup if an effect database is assigned.
        effectDatabase?.Init();

        // Show starting HP.
        UpdateUI();
    }

    // Returns the enemy's current HP.
    public int GetCurrentHP()
    {
        return currentHP;
    }

    // Checks whether this enemy currently has a specific effect.
    //
    // Example:
    // HasEffect(EffectType.Poison)
    public bool HasEffect(EffectType type)
    {
        return activeEffects.Exists(e => e.type == type);
    }

    // Damages the enemy.
    public void TakeDamage(int amount)
    {
        // Subtract HP.
        currentHP -= amount;

        // Keep HP between 0 and maxHP.
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // Refresh HP text.
        UpdateUI();

        // Die if HP reaches 0.
        if (currentHP <= 0)
            Die();
    }

    // Applies a card effect to this enemy.
    //
    // If the enemy already has the same effect:
    // - duration is increased
    // - value becomes whichever value is higher
    //
    // If the enemy does not have the effect:
    // - a new ActiveEffect is added
    public void ApplyEffect(CardEffect effect)
    {
        // Try to find an existing effect of the same type.
        ActiveEffect existing = activeEffects.Find(e => e.type == effect.effectType);

        if (existing != null)
        {
            // Add more duration to the existing effect.
            existing.duration += effect.duration;

            // Keep the strongest value.
            existing.value = Mathf.Max(existing.value, effect.value);
        }
        else
        {
            // Add the effect as a new active effect.
            activeEffects.Add(new ActiveEffect(effect));
        }

        // Rebuild the effect icon UI.
        RebuildEffectUI();
    }

    // Rebuilds all effect icons above/on the enemy.
    private void RebuildEffectUI()
    {
        // Stop if any required UI references are missing.
        if (effectContainer == null || effectIconPrefab == null || effectDatabase == null)
            return;

        // Sort effects so icons appear in a consistent order.
        activeEffects.Sort((a, b) => a.type.CompareTo(b.type));

        // Delete old effect icons.
        foreach (Transform child in effectContainer)
            Destroy(child.gameObject);

        // Create a new icon for every active effect.
        foreach (var effect in activeEffects)
        {
            // Spawn an effect icon UI object.
            GameObject obj = Instantiate(effectIconPrefab, effectContainer);

            // Get the icon UI script.
            EffectIconUI ui = obj.GetComponent<EffectIconUI>();

            // Set icon sprite and duration text.
            ui.Setup(effectDatabase.GetIcon(effect.type), effect.duration);

            // Start at zero scale for a small pop-in animation.
            obj.transform.localScale = Vector3.zero;

            // Animate the icon appearing.
            obj.transform.DOScale(1f, 0.15f);
        }

        // Position the effect icons.
        ArrangeEffects();
    }

    // Arranges the active effect icons.
    private void ArrangeEffects()
    {
        // Use spline layout if enabled and assigned.
        if (useSplineLayout && splineContainer != null)
        {
            Spline spline = splineContainer.Spline;

            // Width of the section of the spline used for effects.
            float width = 0.8f;

            // Space icons evenly across the width.
            float spacing = activeEffects.Count <= 1 ? 0 : width / (activeEffects.Count - 1);

            for (int i = 0; i < effectContainer.childCount; i++)
            {
                // Calculate spline position.
                float t = 0.5f - width / 2f + spacing * i;

                Vector3 localPos = spline.EvaluatePosition(t);
                Vector3 worldPos = splineContainer.transform.TransformPoint(localPos);

                // Move icon to the spline position.
                effectContainer.GetChild(i).position = worldPos;
            }
        }
        else
        {
            // Simple fallback layout:
            // place icons in a horizontal line.
            float spacing = 0.5f;

            for (int i = 0; i < effectContainer.childCount; i++)
            {
                Transform child = effectContainer.GetChild(i);
                child.localPosition = new Vector3(i * spacing, 0f, 0f);
            }
        }
    }

    // Processes active effects.
    //
    // HandManager calls this during the enemy turn.
    //
    // Damage-over-time effects deal damage here,
    // then all effects lose 1 duration.
    public void ProcessEffects()
    {
        foreach (var effect in activeEffects)
        {
            switch (effect.type)
            {
                // These effects deal damage every time effects are processed.
                case EffectType.Poison:
                case EffectType.Burn:
                case EffectType.Bleed:
                    TakeDamage(effect.value);
                    break;
            }

            // Reduce duration after processing.
            effect.duration--;
        }

        // Remove expired effects.
        activeEffects.RemoveAll(e => e.duration <= 0);

        // Refresh icons after effects changed.
        RebuildEffectUI();
    }

    // Makes this enemy attack the player.
    public void AttackPlayer()
    {
        // Stunned or sleeping enemies skip their attack.
        if (HasEffect(EffectType.Stun) || HasEffect(EffectType.Sleep))
            return;

        // Damage the player.
        PlayerHealth.Instance.TakeDamage(attackDamage);
    }

    // Gives this enemy a chance to flee.
    //
    // In this system, fleeing currently means the enemy dies/disappears.
    public void TryFlee(float chance)
    {
        // Bosses cannot flee.
        if (isBoss)
            return;

        // Random.value returns a number between 0 and 1.
        //
        // If it is less than or equal to chance, the enemy flees.
        if (Random.value <= chance)
            Die();
    }

    // Updates the enemy HP text.
    private void UpdateUI()
    {
        if (hpText != null)
            hpText.text = $"{currentHP}/{maxHP}";
    }

    // Kills this enemy.
    private void Die()
    {
        // Tell EnemyManager this enemy is gone.
        EnemyManager.Instance?.UnregisterEnemy(this);

        // Destroy the enemy GameObject.
        Destroy(gameObject);
    }
}