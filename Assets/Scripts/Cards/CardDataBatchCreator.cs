using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Editor-only helper for creating the full card list as CardData assets.
//
// Put this file inside an Editor folder, for example:
// Assets/Scripts/Editor/CardDataBatchCreator.cs
//
// Then use:
// Tools/Cards/Rebuild Generated CardData
public static class CardDataBatchCreator
{
    private const string GeneratedFolder = "Assets/GeneratedCards";

    private static readonly string[] RecommendedStarterCards =
    {
        "Heavy Hitter",
        "Lowest Pickings",
        "Throwing Fire",
        "Heal",
        "Shield"
    };

    [MenuItem("Tools/Cards/Rebuild Generated CardData")]
    public static void RebuildGeneratedCardData()
    {
        EnsureFolder(GeneratedFolder);

        List<CardBuildSpec> specs = BuildCardSpecs();
        int createdOrUpdated = 0;

        for (int i = 0; i < specs.Count; i++)
        {
            CardBuildSpec spec = specs[i];
            string path = GetAssetPath(spec.cardName);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);

            if (cardData == null)
            {
                cardData = ScriptableObject.CreateInstance<CardData>();
                AssetDatabase.CreateAsset(cardData, path);
            }

            ApplySpec(cardData, spec);
            EditorUtility.SetDirty(cardData);
            createdOrUpdated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created or updated " + createdOrUpdated + " generated CardData assets in " + GeneratedFolder + ".");
        Debug.Log("Recommended starter cards: " + string.Join(", ", RecommendedStarterCards) + ".");
    }

    [MenuItem("Tools/Cards/Assign Generated Cards To Selected DeckRuntimeManager")]
    public static void AssignGeneratedCardsToSelectedDeckRuntimeManager()
    {
        DeckRuntimeManager deckRuntimeManager = Selection.activeGameObject == null
            ? null
            : Selection.activeGameObject.GetComponent<DeckRuntimeManager>();

        if (deckRuntimeManager == null)
        {
            Debug.LogError("Select a GameObject with DeckRuntimeManager first.");
            return;
        }

        List<CardData> generatedCards = LoadGeneratedCards();

        SerializedObject serializedObject = new SerializedObject(deckRuntimeManager);
        SerializedProperty availableCards = serializedObject.FindProperty("availableCards");

        if (availableCards == null)
        {
            Debug.LogError("Could not find DeckRuntimeManager.availableCards.");
            return;
        }

        availableCards.ClearArray();

        for (int i = 0; i < generatedCards.Count; i++)
        {
            availableCards.InsertArrayElementAtIndex(i);
            availableCards.GetArrayElementAtIndex(i).objectReferenceValue = generatedCards[i];
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(deckRuntimeManager);

        Debug.Log("Assigned " + generatedCards.Count + " generated cards to DeckRuntimeManager.availableCards.");
    }

    [MenuItem("Tools/Cards/Assign Starter Cards To Selected HandManager")]
    public static void AssignStarterCardsToSelectedHandManager()
    {
        HandManager handManager = Selection.activeGameObject == null
            ? null
            : Selection.activeGameObject.GetComponent<HandManager>();

        if (handManager == null)
        {
            Debug.LogError("Select a GameObject with HandManager first.");
            return;
        }

        List<CardData> starterCards = LoadGeneratedCards()
            .Where(card => card != null && RecommendedStarterCards.Contains(card.cardName))
            .OrderBy(card => System.Array.IndexOf(RecommendedStarterCards, card.cardName))
            .ToList();

        SerializedObject serializedObject = new SerializedObject(handManager);
        SerializedProperty deck = serializedObject.FindProperty("deck");

        if (deck == null)
        {
            Debug.LogError("Could not find HandManager.deck.");
            return;
        }

        deck.ClearArray();

        for (int i = 0; i < starterCards.Count; i++)
        {
            deck.InsertArrayElementAtIndex(i);
            deck.GetArrayElementAtIndex(i).objectReferenceValue = starterCards[i];
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(handManager);

        Debug.Log("Assigned starter cards to HandManager.deck: " + string.Join(", ", starterCards.Select(card => card.cardName)) + ".");
    }

    private static void ApplySpec(CardData cardData, CardBuildSpec spec)
    {
        cardData.cardName = spec.cardName;
        cardData.cardType = spec.cardType;
        cardData.targetType = spec.targetType;
        cardData.baseDamage = spec.baseDamage;
        cardData.basicAttackDamageType = spec.basicAttackDamageType;
        cardData.manaCostMode = spec.manaCostMode;
        cardData.manaCost = spec.manaCost;
        cardData.minimumManaToCast = spec.minimumManaToCast;
        cardData.spendManaOnSkillCast = spec.spendManaOnSkillCast;
        cardData.miniGameType = spec.miniGameType;
        cardData.miniGameTimeLimit = spec.miniGameTimeLimit;
        cardData.miniGameInputKey = spec.miniGameInputKey;
        cardData.allowLeftClickInput = spec.allowLeftClickInput;
        cardData.miniGameSequenceLength = spec.miniGameSequenceLength;
        cardData.miniGameMashTarget = spec.miniGameMashTarget;
        cardData.miniGameSliderSpeed = spec.miniGameSliderSpeed;
        cardData.miniGameHoldSpeed = spec.miniGameHoldSpeed;
        cardData.skillText = spec.skillText;
        cardData.effects = new List<CardEffect>(spec.effects);
    }

    private static List<CardData> LoadGeneratedCards()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            RebuildGeneratedCardData();

        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { GeneratedFolder });

        return guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<CardData>)
            .Where(card => card != null)
            .OrderBy(card => card.cardName)
            .ToList();
    }

    private static string GetAssetPath(string cardName)
    {
        string safeName = Regex.Replace(cardName, @"[^a-zA-Z0-9_ -]", "");
        safeName = safeName.Replace(" ", "_");
        return Path.Combine(GeneratedFolder, safeName + ".asset").Replace("\\", "/");
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static List<CardBuildSpec> BuildCardSpecs()
    {
        List<CardBuildSpec> cards = new List<CardBuildSpec>();

        cards.Add(Spec("Heavy Hitter", CardType.Physical, CardTargetType.SelectedEnemy, 2, 4, CardMiniGameType.ButtonMash, "Deals a strong heavy hit to one selected enemy.", Damage(7, CardDamageType.Physical)));
        cards.Add(Spec("Heavy Crash", CardType.Physical, CardTargetType.RandomEnemy, 2, 2, CardMiniGameType.ButtonMash, "Deals a heavy hit to a random enemy. Cheaper because the player cannot choose the target.", Damage(4, CardDamageType.Physical)));
        cards.Add(Spec("Lowest Pickings", CardType.Physical, CardTargetType.LowestHPEnemy, 1, 2, CardMiniGameType.ButtonMash, "Automatically targets the enemy with the lowest current HP. Bonus under 30% HP needs custom code if you want the +1 rule.", Damage(4, CardDamageType.Physical)));
        cards.Add(Spec("Hearty Hitter", CardType.Physical, CardTargetType.HighestHPEnemy, 2, 3, CardMiniGameType.ButtonMash, "Automatically targets the enemy with the highest current HP. Bonus above 50% HP needs custom code if you want the +1 rule.", Damage(5, CardDamageType.Physical)));
        cards.Add(Spec("Adjacent Strike", CardType.Physical, CardTargetType.SelectedEnemy, 2, 4, CardMiniGameType.ButtonMash, "Hits one selected enemy, then deals 2 splash damage to enemies directly beside it.", Damage(5, CardDamageType.Physical), Damage(2, CardDamageType.Physical, CardTargetType.AdjacentEnemies)));
        cards.Add(Spec("Piercing Blow", CardType.Physical, CardTargetType.SelectedEnemy, 2, 3, CardMiniGameType.HoldRelease, "Deals damage that ignores enemy shield or armour.", Effect(EffectType.PiercingDamage, 4, CardDamageType.Physical)));
        cards.Add(Spec("Swinging With Rage", CardType.Physical, CardTargetType.RandomEnemy, 1, 4, CardMiniGameType.ButtonMash, "Hits random enemies 5 times. The same enemy can be hit more than once.", RandomHits(1, 5, CardDamageType.Physical)));
        cards.Add(Spec("Swinging Blind", CardType.Physical, CardTargetType.RandomEnemy, 1, 3, CardMiniGameType.ButtonMash, "Hits random enemies 3-6 times. The same enemy can be hit more than once.", RandomHits(1, 1, CardDamageType.Physical, 3, 6)));
        cards.Add(Spec("Slash", CardType.Physical, CardTargetType.AllNonBossEnemies, 1, 3, CardMiniGameType.TimingBar, "Hits all non-boss enemies.", Damage(3, CardDamageType.Physical, worksOnBosses: false)));
        cards.Add(Spec("Boss Splitter", CardType.Physical, CardTargetType.AllNonBossEnemies, 2, 5, CardMiniGameType.TimingBar, "Deals high damage to every normal enemy but does not affect bosses.", Damage(5, CardDamageType.Physical, worksOnBosses: false)));

        cards.Add(Spec("Absorb", CardType.Magic, CardTargetType.SelectedEnemy, 1, 3, CardMiniGameType.ButtonMash, "Deals 2 damage plus 25% of the enemy's missing HP. The listed damage cap of 8 needs custom code.", Damage(2, CardDamageType.Magic), Effect(EffectType.PercentMissingHPDamage, 25, CardDamageType.Magic)));
        cards.Add(Spec("Drain", CardType.Magic, CardTargetType.SelectedEnemy, 1, 4, CardMiniGameType.ButtonMash, "Deals 2 damage plus 15% of the enemy's maximum HP. Does not work on bosses.", Damage(2, CardDamageType.Magic, worksOnBosses: false), Effect(EffectType.PercentMaxHPDamage, 15, CardDamageType.Magic, worksOnBosses: false)));
        cards.Add(Spec("Throwing Fire", CardType.Magic, CardTargetType.AllEnemies, 1, 4, CardMiniGameType.TimingBar, "Hits all enemies and has a 35% chance to apply Burn. Boss Burn chance is reduced to about 15%.", Damage(3, CardDamageType.Magic), Status(EffectType.Burn, 2, 0, 1, 0.35f, true, 0.43f)));
        cards.Add(Spec("Poison Cloud", CardType.Magic, CardTargetType.AllEnemies, 1, 3, CardMiniGameType.TimingBar, "Deals 1 damage to all enemies and applies Poison for 2 turns. Boss Poison lasts 1 turn.", Damage(1, CardDamageType.Magic), Status(EffectType.Poison, 2, 0, 2, 1f, true, 1f, 0.5f)));
        cards.Add(Spec("Shadow Wall", CardType.Magic, CardTargetType.AllEnemies, 1, 2, CardMiniGameType.TimingBar, "Hits all enemies and may Blind non-boss enemies.", Damage(2, CardDamageType.Magic), Status(EffectType.Blind, 0, 0, 1, 0.3f, false)));
        cards.Add(Spec("Curse", CardType.Magic, CardTargetType.AllNonBossEnemies, 1, 3, CardMiniGameType.TimingCircle, "Deals damage and applies Curse for 2 turns.", Damage(1, CardDamageType.Magic, worksOnBosses: false), Status(EffectType.Curse, 15, 15, 2, worksOnBosses: false)));
        cards.Add(Spec("Petrify", CardType.Magic, CardTargetType.AllNonBossEnemies, 1, 4, CardMiniGameType.HoldRelease, "Deals damage and Petrifies non-boss enemies for 1 turn.", Damage(1, CardDamageType.Magic, worksOnBosses: false), Status(EffectType.Petrify, 50, 0, 1, worksOnBosses: false)));
        cards.Add(Spec("Marked", CardType.Magic, CardTargetType.RandomEnemy, 1, 3, CardMiniGameType.TimingBar, "Hits random enemies 5 times. Each hit has a 20% chance to apply Marked to non-boss enemies.", RandomHits(1, 5, CardDamageType.Magic, statusOnHit: EffectType.Marked, statusChance: 0.2f, statusWorksOnBosses: false)));
        cards.Add(Spec("Exposed", CardType.Magic, CardTargetType.SelectedEnemy, 1, 3, CardMiniGameType.ButtonMash, "Damages an enemy, removes its shield protection, and applies Exposed for 2 turns.", Damage(3, CardDamageType.Magic), Status(EffectType.Exposed, 0, 0, 2)));
        cards.Add(Spec("Weakened", CardType.Magic, CardTargetType.AllNonBossEnemies, 1, 2, CardMiniGameType.TimingBar, "Deals damage and applies Weakened for 2 turns.", Damage(1, CardDamageType.Magic, worksOnBosses: false), Status(EffectType.Weakened, 25, 0, 2, worksOnBosses: false)));
        cards.Add(Spec("Softened", CardType.Magic, CardTargetType.AllNonBossEnemies, 1, 3, CardMiniGameType.TimingCircle, "Deals damage and applies Softened for 2 turns.", Damage(1, CardDamageType.Magic, worksOnBosses: false), Status(EffectType.Softened, 20, 0, 2, worksOnBosses: false)));
        cards.Add(Spec("Doomed", CardType.Magic, CardTargetType.AllEnemies, 1, 5, CardMiniGameType.TimingCircle, "Hits each enemy and applies Doomed for 2 turns. Boss burst value is reduced.", Damage(2, CardDamageType.Magic), Status(EffectType.Doomed, 5, 0, 2, bossValueMultiplier: 0.6f)));
        cards.Add(Spec("Silenced", CardType.Magic, CardTargetType.SelectedEnemy, 1, 2, CardMiniGameType.TimingBar, "Deals damage and applies Silenced for 2 turns. Does not work on bosses.", Damage(2, CardDamageType.Magic, worksOnBosses: false), Status(EffectType.Silenced, 0, 0, 2, worksOnBosses: false)));
        cards.Add(Spec("Dazed", CardType.Magic, CardTargetType.AllNonBossEnemies, 1, 3, CardMiniGameType.TimingCircle, "Deals damage and applies Dazed for 2 turns.", Damage(1, CardDamageType.Magic, worksOnBosses: false), Status(EffectType.Dazed, 25, 0, 2, worksOnBosses: false)));
        cards.Add(Spec("Charmed", CardType.Magic, CardTargetType.SelectedEnemy, 1, 4, CardMiniGameType.SimonSays, "Deals damage and applies Charmed for 1 turn. Does not work on bosses.", Damage(1, CardDamageType.Magic, worksOnBosses: false), Status(EffectType.Charmed, 60, 0, 1, worksOnBosses: false)));
        cards.Add(Spec("Poison", CardType.Magic, CardTargetType.SelectedEnemy, 1, 2, CardMiniGameType.TimingBar, "Applies Poison for 3 turns. Boss Poison lasts 2 turns.", Status(EffectType.Poison, 2, 0, 3, bossDurationMultiplier: 0.67f)));
        cards.Add(Spec("Burn", CardType.Magic, CardTargetType.SelectedEnemy, 1, 2, CardMiniGameType.TimingCircle, "Deals damage and applies Burn. Burn continuation chance is stored in secondaryValue.", Damage(2, CardDamageType.Magic), Status(EffectType.Burn, 2, 40, 1)));
        cards.Add(Spec("Bleed", CardType.Physical, CardTargetType.RandomEnemy, 1, 3, CardMiniGameType.ButtonMash, "Hits random enemies 3 times. Each hit can apply Bleed.", RandomHits(1, 3, CardDamageType.Physical, statusOnHit: EffectType.Bleed, statusChance: 0.25f, statusValue: 1, statusSecondaryValue: 10, statusDuration: 3, statusBossChanceMultiplier: 0.48f)));
        cards.Add(Spec("Stun", CardType.Magic, CardTargetType.RandomNonBossEnemy, 1, 4, CardMiniGameType.HoldRelease, "Hits 2 random non-boss enemies. Each hit has a 30% chance to Stun for 1 turn.", RandomHits(2, 2, CardDamageType.Magic, targetOverride: CardTargetType.RandomNonBossEnemy, worksOnBosses: false, statusOnHit: EffectType.Stun, statusChance: 0.3f, statusDuration: 1, statusWorksOnBosses: false)));
        cards.Add(Spec("Sleep", CardType.Magic, CardTargetType.SelectedEnemy, 1, 3, CardMiniGameType.TimingCircle, "Applies Sleep for 1-2 turns. Damage wakes the enemy early. Does not work on bosses.", Status(EffectType.Sleep, 0, 0, 1, worksOnBosses: false, removeWhenDamaged: true, randomMinDuration: 1, randomMaxDuration: 2)));
        cards.Add(Spec("Cripple", CardType.Magic, CardTargetType.RandomEnemy, 1, 3, CardMiniGameType.ButtonMash, "Hits random enemies 6 times. Each hit can apply Cripple. Does not work on bosses.", RandomHits(1, 6, CardDamageType.Magic, worksOnBosses: false, statusOnHit: EffectType.Cripple, statusChance: 0.12f, statusSecondaryValue: 35, statusWorksOnBosses: false)));
        cards.Add(Spec("Vulnerable", CardType.Magic, CardTargetType.AllNonBossEnemies, 1, 3, CardMiniGameType.TimingBar, "Deals damage and applies Vulnerable for 2 turns.", Damage(1, CardDamageType.Magic, worksOnBosses: false), Status(EffectType.Vulnerable, 25, 0, 2, worksOnBosses: false)));
        cards.Add(Spec("Flee", CardType.Magic, CardTargetType.AllNonBossEnemies, 1, 4, CardMiniGameType.SimonSays, "Attempts to make non-boss enemies flee. The below-30% HP rule needs custom code if you want strict gating.", Effect(EffectType.Flee, 1, CardDamageType.Magic, CardTargetType.AllNonBossEnemies, worksOnBosses: false)));
        cards.Add(Spec("Wildfire", CardType.Magic, CardTargetType.SelectedEnemy, 1, 4, CardMiniGameType.TimingCircle, "Deals damage and applies Wildfire. Spread chance is stored in secondaryValue.", Damage(3, CardDamageType.Magic), Status(EffectType.Wildfire, 3, 35, 1)));
        cards.Add(Spec("Leech DOT", CardType.Magic, CardTargetType.SelectedEnemy, 1, 3, CardMiniGameType.TimingBar, "Applies Leech for 3 turns. Boss Leech lasts 2 turns.", Status(EffectType.Leech, 2, 50, 3, bossDurationMultiplier: 0.67f)));
        cards.Add(Spec("Volatile DOT", CardType.Magic, CardTargetType.SelectedEnemy, 1, 4, CardMiniGameType.TimingCircle, "Applies Volatile for 2 turns. Explosion damage is stored in secondaryValue.", Status(EffectType.Volatile, 3, 4, 2)));
        cards.Add(Spec("Multi DOT", CardType.Magic, CardTargetType.AllEnemies, 1, 5, CardMiniGameType.SimonSays, "Deals damage to all enemies. Each enemy receives one random DOT or debuff for 1 turn.", Damage(1, CardDamageType.Magic), Effect(EffectType.RandomStatus, 1, CardDamageType.Magic, duration: 1)));

        cards.Add(Spec("Heal", CardType.Support, CardTargetType.Player, 1, 2, CardMiniGameType.SimonSays, "Heals the player.", Support(EffectType.Heal, 5)));
        cards.Add(Spec("Shield", CardType.Support, CardTargetType.Player, 1, 2, CardMiniGameType.HoldRelease, "Adds shield to the player.", Support(EffectType.Shield, 5)));
        cards.Add(Spec("Cleanse", CardType.Support, CardTargetType.Player, 1, 2, CardMiniGameType.SimonSays, "Removes 1 random negative effect. Great or Perfect removes 2.", Support(EffectType.Cleanse, 1)));
        cards.Add(Spec("Regeneration", CardType.Support, CardTargetType.Player, 1, 3, CardMiniGameType.SimonSays, "Heals the player over time for 3 turns.", Support(EffectType.Regeneration, 2, duration: 3)));
        cards.Add(Spec("Life Steal", CardType.Magic, CardTargetType.SelectedEnemy, 1, 4, CardMiniGameType.TimingBar, "Deals damage, then heals the player for the damage dealt.", Effect(EffectType.LifeStealDamage, 4, CardDamageType.Magic)));
        cards.Add(Spec("Damage Buff", CardType.Support, CardTargetType.Player, 1, 2, CardMiniGameType.SimonSays, "Physical cards deal 20% more damage for 2 turns. Great or Perfect increases duration.", Support(EffectType.PhysicalBuff, 20, duration: 2, bonusTurnsOnGreatOrPerfect: 1)));
        cards.Add(Spec("Magic Buff", CardType.Support, CardTargetType.Player, 1, 2, CardMiniGameType.SimonSays, "Magic cards deal 20% more damage for 2 turns. Great or Perfect increases duration.", Support(EffectType.MagicBuff, 20, duration: 2, bonusTurnsOnGreatOrPerfect: 1)));
        cards.Add(Spec("Support Buff", CardType.Support, CardTargetType.Player, 1, 2, CardMiniGameType.SimonSays, "Support cards are 20% stronger for 2 turns. Great or Perfect increases duration.", Support(EffectType.SupportBuff, 20, duration: 2, bonusTurnsOnGreatOrPerfect: 1)));
        cards.Add(Spec("Common Buff", CardType.Support, CardTargetType.Player, 1, 1, CardMiniGameType.SimonSays, "All cards are 10% stronger for 2 turns. Perfect increases this to 15%.", Support(EffectType.CommonBuff, 10, duration: 2, perfectValueOverride: 15)));
        cards.Add(Spec("Cleanse All", CardType.Support, CardTargetType.Player, 1, 3, CardMiniGameType.None, "Removes all negative effects from the player and clears control effects from enemies.", Support(EffectType.CleanseAll, 0)));
        cards.Add(Spec("Full Heal", CardType.Support, CardTargetType.Player, 1, 5, CardMiniGameType.None, "Fully heals the player. One-use-per-combat needs a separate exhaust rule if required.", Support(EffectType.FullHeal, 0)));
        cards.Add(Spec("Extreme Buff", CardType.Support, CardTargetType.Player, 1, 5, CardMiniGameType.None, "Damage, healing, shield, and support effects are increased by 35% for 2 turns.", Support(EffectType.ExtremeBuff, 35, duration: 2)));
        cards.Add(Spec("Clone", CardType.Support, CardTargetType.Player, 1, 3, CardMiniGameType.None, "Copies the card to its left at 70% power. Cannot copy Clone, Mirror, Ghost, Full Heal, Extreme Buff, or All In.", Support(EffectType.CloneLeft, 0)));
        cards.Add(Spec("Mirror", CardType.Support, CardTargetType.Player, 1, 3, CardMiniGameType.None, "Copies the card to its right at 70% power. Cannot copy Clone, Mirror, Ghost, Full Heal, Extreme Buff, or All In.", Support(EffectType.CloneRight, 0)));
        cards.Add(Spec("Ghost", CardType.Support, CardTargetType.Player, 1, 4, CardMiniGameType.None, "Ghost needs a new EffectType to add temporary cards. This generated asset is a placeholder so the card exists in the deck builder.", Support(EffectType.ReturnMana, 0)));
        cards.Add(Spec("Return Mana Small", CardType.Support, CardTargetType.Player, 1, 0, CardMiniGameType.None, "Gain 1 Mana. Exhaust-after-use needs a separate exhaust rule if required.", Support(EffectType.ReturnMana, 1)));
        cards.Add(Spec("Return Mana Medium", CardType.Support, CardTargetType.Player, 1, 1, CardMiniGameType.None, "Spend 1 Mana to gain 3 Mana.", Support(EffectType.ReturnMana, 3)));
        cards.Add(Spec("Return Mana Large", CardType.Support, CardTargetType.Player, 1, 2, CardMiniGameType.None, "Spend 2 Mana to gain 5 Mana.", Support(EffectType.ReturnMana, 5)));
        cards.Add(Spec("Chance", CardType.Support, CardTargetType.Player, 2, 4, CardMiniGameType.None, "Coin flip and chance-buff behaviour needs a new minigame/effect. This currently applies the 35% all-effect buff.", Support(EffectType.ExtremeBuff, 35, duration: 2)));
        cards.Add(Spec("All In", CardType.Magic, CardTargetType.SelectedEnemy, 2, 0, CardMiniGameType.TimingCircle, "Spend all remaining Mana to deal Base Damage + 3 damage per Mana spent.", Effect(EffectType.AllInDamage, 3, CardDamageType.Magic), CardManaCostMode.AllRemaining, minimumManaToCast: 1f));

        return cards;
    }

    private static CardBuildSpec Spec(
        string cardName,
        CardType cardType,
        CardTargetType targetType,
        int baseDamage,
        float manaCost,
        CardMiniGameType miniGameType,
        string skillText,
        CardEffect effect,
        CardManaCostMode manaCostMode = CardManaCostMode.Fixed,
        float minimumManaToCast = 0f)
    {
        return Spec(cardName, cardType, targetType, baseDamage, manaCost, miniGameType, skillText, new[] { effect }, manaCostMode, minimumManaToCast);
    }

    private static CardBuildSpec Spec(
        string cardName,
        CardType cardType,
        CardTargetType targetType,
        int baseDamage,
        float manaCost,
        CardMiniGameType miniGameType,
        string skillText,
        CardEffect firstEffect,
        CardEffect secondEffect,
        CardManaCostMode manaCostMode = CardManaCostMode.Fixed,
        float minimumManaToCast = 0f)
    {
        return Spec(cardName, cardType, targetType, baseDamage, manaCost, miniGameType, skillText, new[] { firstEffect, secondEffect }, manaCostMode, minimumManaToCast);
    }

    private static CardBuildSpec Spec(
        string cardName,
        CardType cardType,
        CardTargetType targetType,
        int baseDamage,
        float manaCost,
        CardMiniGameType miniGameType,
        string skillText,
        CardEffect[] effects,
        CardManaCostMode manaCostMode = CardManaCostMode.Fixed,
        float minimumManaToCast = 0f)
    {
        CardBuildSpec spec = new CardBuildSpec
        {
            cardName = cardName,
            cardType = cardType,
            targetType = targetType,
            baseDamage = baseDamage,
            basicAttackDamageType = cardType == CardType.Physical ? CardDamageType.Physical : CardDamageType.Magic,
            manaCost = manaCost,
            manaCostMode = manaCostMode,
            minimumManaToCast = minimumManaToCast,
            spendManaOnSkillCast = true,
            miniGameType = miniGameType,
            skillText = skillText,
            effects = effects == null ? new List<CardEffect>() : effects.Where(effect => effect != null).ToList()
        };

        if (cardType == CardType.Support)
            spec.basicAttackDamageType = CardDamageType.Physical;

        return spec;
    }

    private static CardEffect Damage(
        int value,
        CardDamageType damageType,
        CardTargetType targetOverride = CardTargetType.UseCardTarget,
        bool worksOnBosses = true)
    {
        return Effect(EffectType.Damage, value, damageType, targetOverride, worksOnBosses: worksOnBosses);
    }

    private static CardEffect Support(
        EffectType effectType,
        int value,
        int duration = 0,
        int bonusTurnsOnGreatOrPerfect = 0,
        int perfectValueOverride = 0)
    {
        CardEffect effect = Effect(effectType, value, CardDamageType.Magic, CardTargetType.Player, duration: duration);
        effect.bonusTurnsOnGreatOrPerfect = bonusTurnsOnGreatOrPerfect;
        effect.perfectValueOverride = perfectValueOverride;
        return effect;
    }

    private static CardEffect Status(
        EffectType statusType,
        int value,
        int secondaryValue,
        int duration,
        float chance = 1f,
        bool worksOnBosses = true,
        float bossChanceMultiplier = 1f,
        float bossDurationMultiplier = 1f,
        float bossValueMultiplier = 1f,
        bool removeWhenDamaged = false,
        int randomMinDuration = 0,
        int randomMaxDuration = 0)
    {
        CardEffect effect = Effect(statusType, value, CardDamageType.Magic, duration: duration, worksOnBosses: worksOnBosses);
        effect.secondaryValue = secondaryValue;
        effect.chance = chance;
        effect.bossChanceMultiplier = bossChanceMultiplier;
        effect.bossDurationMultiplier = bossDurationMultiplier;
        effect.bossValueMultiplier = bossValueMultiplier;
        effect.removeWhenDamaged = removeWhenDamaged;
        effect.randomMinDuration = randomMinDuration;
        effect.randomMaxDuration = randomMaxDuration;
        return effect;
    }

    private static CardEffect RandomHits(
        int value,
        int hitCount,
        CardDamageType damageType,
        int randomMinHits = 0,
        int randomMaxHits = 0,
        CardTargetType targetOverride = CardTargetType.UseCardTarget,
        bool worksOnBosses = true,
        EffectType statusOnHit = EffectType.None,
        float statusChance = 0f,
        int statusValue = 0,
        int statusSecondaryValue = 0,
        int statusDuration = 0,
        bool statusWorksOnBosses = true,
        float statusBossChanceMultiplier = 1f)
    {
        CardEffect effect = Effect(EffectType.RandomHits, value, damageType, targetOverride, worksOnBosses: worksOnBosses);
        effect.hitCount = hitCount;
        effect.randomMinHits = randomMinHits;
        effect.randomMaxHits = randomMaxHits;
        effect.statusAppliedOnHit = statusOnHit;
        effect.statusChanceOnHit = statusChance;
        effect.statusValueOnHit = statusValue;
        effect.statusSecondaryValueOnHit = statusSecondaryValue;
        effect.statusDurationOnHit = statusDuration;
        effect.statusWorksOnBosses = statusWorksOnBosses;
        effect.statusBossChanceMultiplier = statusBossChanceMultiplier;
        return effect;
    }

    private static CardEffect Effect(
        EffectType effectType,
        int value,
        CardDamageType damageType,
        CardTargetType targetOverride = CardTargetType.UseCardTarget,
        int duration = 0,
        bool worksOnBosses = true)
    {
        return new CardEffect
        {
            effectType = effectType,
            targetOverride = targetOverride,
            damageType = damageType,
            value = value,
            duration = duration,
            worksOnBosses = worksOnBosses,
            chance = 1f,
            bossChanceMultiplier = 1f,
            bossValueMultiplier = 1f,
            bossDurationMultiplier = 1f,
            scaleValueWithMiniGame = true,
            hitCount = 1
        };
    }

    private class CardBuildSpec
    {
        public string cardName;
        public CardType cardType;
        public CardTargetType targetType;
        public int baseDamage;
        public CardDamageType basicAttackDamageType;
        public CardManaCostMode manaCostMode;
        public float manaCost;
        public float minimumManaToCast;
        public bool spendManaOnSkillCast;
        public CardMiniGameType miniGameType;
        public float miniGameTimeLimit = 3f;
        public KeyCode miniGameInputKey = KeyCode.Space;
        public bool allowLeftClickInput = true;
        public int miniGameSequenceLength = 4;
        public int miniGameMashTarget = 20;
        public float miniGameSliderSpeed = 1.75f;
        public float miniGameHoldSpeed = 0.75f;
        public string skillText;
        public List<CardEffect> effects = new List<CardEffect>();
    }
}
