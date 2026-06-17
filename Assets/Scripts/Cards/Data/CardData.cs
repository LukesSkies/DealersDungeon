using System.Collections.Generic;
using UnityEngine;

// Stores all data for one card.
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName;
    public CardType cardType = CardType.Physical;

    [Header("Visuals")]
    public Sprite cardSprite;
    public Sprite miniGameIcon;

    [TextArea(3, 8)] public string skillText;

    [Header("Basic Tap / Drag Attack")]
    public int baseDamage = 1;
    public CardDamageType basicAttackDamageType = CardDamageType.Physical;

    [Header("Skill")]
    public CardTargetType targetType = CardTargetType.SelectedEnemy;
    public CardManaCostMode manaCostMode = CardManaCostMode.Fixed;
    public float manaCost = 0f;
    public float minimumManaToCast = 0f;
    public bool spendManaOnSkillCast = true;

    [Header("Mini Game")]
    public CardMiniGameType miniGameType = CardMiniGameType.None;
    public float miniGameTimeLimit = 3f;
    public KeyCode miniGameInputKey = KeyCode.Space;
    public bool allowLeftClickInput = true;

    [Header("Mini Game - Simon Says")]
    public int miniGameSequenceLength = 4;

    [Header("Mini Game - Button Mash")]
    public int miniGameMashTarget = 20;

    [Header("Mini Game - Timing / Hold")]
    public float miniGameSliderSpeed = 1.75f;
    public float miniGameHoldSpeed = 0.75f;

    [Header("Effects")]
    public List<CardEffect> effects = new List<CardEffect>();

    public bool CanTapAttack()
    {
        return true;
    }

    public bool CanDragAttack()
    {
        return true;
    }

    public bool HasSkill()
    {
        if (effects == null || effects.Count == 0)
            return false;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null && effects[i].effectType != EffectType.None)
                return true;
        }

        return false;
    }

    // Backwards-compatible name for older UI scripts.
    public bool HasSpell()
    {
        return HasSkill();
    }

    // Backwards-compatible property for older UI scripts.
    public bool spendManaOnSpellCast
    {
        get { return spendManaOnSkillCast; }
        set { spendManaOnSkillCast = value; }
    }

    public bool HasMiniGame()
    {
        return miniGameType != CardMiniGameType.None;
    }

    public bool RequiresEnemyTargetForSkill()
    {
        if (targetType == CardTargetType.SelectedEnemy || targetType == CardTargetType.AdjacentEnemies)
            return true;

        if (effects == null)
            return false;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null)
                continue;

            CardTargetType effectTarget = effects[i].GetTargetType(this);

            if (effectTarget == CardTargetType.SelectedEnemy || effectTarget == CardTargetType.AdjacentEnemies)
                return true;
        }

        return false;
    }

    // Backwards-compatible name for older scripts.
    public bool RequiresEnemyTargetForSpell()
    {
        return RequiresEnemyTargetForSkill();
    }

    public string GetAbilityText()
    {
        if (!string.IsNullOrWhiteSpace(skillText))
            return skillText;

        return GenerateFallbackAbilityText();
    }

    public string GetManaText()
    {
        if (!HasSkill())
            return "-";

        if (!spendManaOnSkillCast)
            return "Free";

        if (manaCostMode == CardManaCostMode.AllRemaining)
            return "All";

        return manaCost.ToString("0.##");
    }

    public string GetMiniGameText()
    {
        if (miniGameType == CardMiniGameType.None)
            return "None";

        return miniGameType.ToString();
    }

    private string GenerateFallbackAbilityText()
    {
        if (effects == null || effects.Count == 0)
            return "";

        List<string> lines = new List<string>();

        for (int i = 0; i < effects.Count; i++)
        {
            CardEffect effect = effects[i];

            if (effect == null || effect.effectType == EffectType.None)
                continue;

            string line = effect.effectType.ToString();

            if (effect.value > 0)
                line += " " + effect.value;

            if (effect.duration > 0)
                line += " for " + effect.duration + " turns";

            lines.Add(line);
        }

        return string.Join("\n", lines);
    }

    private void OnValidate()
    {
        if (effects == null)
            effects = new List<CardEffect>();

        if (baseDamage < 0)
            baseDamage = 0;

        if (manaCost < 0f)
            manaCost = 0f;

        if (minimumManaToCast < 0f)
            minimumManaToCast = 0f;

        if (miniGameTimeLimit < 0.5f)
            miniGameTimeLimit = 0.5f;

        if (miniGameSequenceLength < 1)
            miniGameSequenceLength = 1;

        if (miniGameMashTarget < 1)
            miniGameMashTarget = 1;

        if (miniGameSliderSpeed <= 0f)
            miniGameSliderSpeed = 1f;

        if (miniGameHoldSpeed <= 0f)
            miniGameHoldSpeed = 0.75f;

        if (manaCostMode == CardManaCostMode.AllRemaining && minimumManaToCast <= 0f)
            minimumManaToCast = 1f;
    }
}