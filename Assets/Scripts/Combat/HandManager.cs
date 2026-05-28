using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

// This script manages the player's hand of cards during combat.
//
// It handles:
// - creating cards from the deck
// - positioning cards along a spline
// - deciding which card is currently active
// - marking cards as used
// - starting the enemy turn after all cards are used
// - clearing the hand when combat ends
public class HandManager : MonoBehaviour
{
    // Singleton reference so other scripts can call:
    // HandManager.Instance.StartCombatHand()
    // HandManager.Instance.GetActiveCard()
    // HandManager.Instance.UseCurrentCard()
    public static HandManager Instance;

    [Header("Setup")]

    // The card prefab that gets spawned for each card in the deck.
    //
    // This prefab should have the Card script attached.
    [SerializeField] private GameObject cardPrefab;

    // The spline used to lay out cards in a curved hand shape.
    //
    // Cards are positioned along this spline when the hand is created.
    [SerializeField] private SplineContainer splineContainer;

    // The point where cards first spawn before moving into position.
    [SerializeField] private Transform spawnPoint;

    [Header("Deck (ORDER MATTERS)")]

    // The deck used to create the hand.
    //
    // The order of this list matters because cards are used from left to right
    // based on currentCardIndex.
    [SerializeField] private List<CardData> deck = new List<CardData>();

    // The currently spawned card objects in the hand.
    private List<Card> handCards = new();

    // Index of the card that is currently active/usable.
    //
    // Example:
    // 0 = first card is active
    // 1 = second card is active
    // etc.
    private int currentCardIndex = 0;

    private void Awake()
    {
        // Set up singleton reference.
        Instance = this;
    }

    // Starts a new combat hand.
    //
    // DungeonManager calls this when combat begins.
    public void StartCombatHand()
    {
        // Clear any existing cards first.
        ClearHand();

        // Create cards from the deck list.
        CreateHandFromDeck();

        // Activate the first card.
        UpdateActiveCard();
    }

    // Creates one card object for every CardData in the deck.
    private void CreateHandFromDeck()
    {
        // Clear the hand card list before rebuilding it.
        handCards.Clear();

        for (int i = 0; i < deck.Count; i++)
        {
            // Spawn the card prefab at the spawn point.
            GameObject g = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation, transform);

            // Get the Card component from the spawned object.
            Card card = g.GetComponent<Card>();

            // Give this card its CardData.
            //
            // This decides the card's damage, cost, effects, name, etc.
            card.SetCardData(deck[i]);

            // Add the card to the hand list.
            handCards.Add(card);
        }

        // Move all cards into their hand positions.
        UpdateCardPositions();
    }

    // Returns the currently active card.
    //
    // EnemyTargeting and CardHoverManager use this.
    public Card GetActiveCard()
    {
        if (currentCardIndex >= handCards.Count)
            return null;

        return handCards[currentCardIndex];
    }

    // Called when the current card has been used.
    //
    // This moves the hand forward to the next card.
    public void UseCurrentCard()
    {
        // Stop if there is no active card left.
        if (currentCardIndex >= handCards.Count)
            return;

        // Store the card that was just used.
        Card usedCard = handCards[currentCardIndex];

        // Play the card use animation.
        StartCoroutine(PlayCardUse(usedCard));

        // Move to the next card.
        currentCardIndex++;

        // Update which card is now active.
        UpdateActiveCard();

        // If every card has been used, start the enemy turn.
        if (currentCardIndex >= handCards.Count)
        {
            StartCoroutine(EnemyTurn());
        }
    }

    // Plays a small animation when a card is used.
    private IEnumerator PlayCardUse(Card card)
    {
        // Stop any existing animations on this card.
        card.transform.DOKill();

        // Quickly scale the card up.
        card.transform.DOScale(card.transform.localScale * 1.2f, 0.1f);

        // Move the card slightly upward.
        card.transform.DOMoveY(card.transform.position.y + 0.3f, 0.1f);

        // Wait briefly so the animation can be seen.
        yield return new WaitForSeconds(0.15f);

        // Mark the card as used.
        card.SetUsed();
    }

    // Updates every card in the hand so only the current card is active.
    private void UpdateActiveCard()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            // Cards before the current index have already been used.
            if (i < currentCardIndex)
                handCards[i].SetUsed();
            // The current card is active.
            // Cards after the current card are inactive.
            else
                handCards[i].SetActive(i == currentCardIndex);
        }
    }

    // Positions all cards along the assigned spline.
    //
    // This creates the curved card hand layout.
    private void UpdateCardPositions()
    {
        // Stop if there are no cards.
        if (handCards.Count == 0)
            return;

        // Get the spline from the spline container.
        Spline spline = splineContainer.Spline;

        // The center point of the hand on the spline.
        //
        // Spline t values usually go from 0 to 1.
        float center = 0.5f;

        // How much of the spline width the hand uses.
        float handWidth = 0.6f;

        // Space between cards along the spline.
        //
        // If there is only one card, spacing is 0.
        float spacing = (handCards.Count == 1) ? 0f : handWidth / (handCards.Count - 1);

        for (int i = 0; i < handCards.Count; i++)
        {
            // Calculate this card's position along the spline.
            float t = center - handWidth / 2f + spacing * i;

            // Get local spline position.
            Vector3 localPos = spline.EvaluatePosition(t);

            // Get spline tangent direction.
            Vector3 forward = spline.EvaluateTangent(t);

            // Get spline up vector.
            Vector3 up = spline.EvaluateUpVector(t);

            // Convert local spline position into world position.
            Vector3 worldPos = splineContainer.transform.TransformPoint(localPos);

            // Calculate the card's rotation based on the spline direction.
            //
            // This makes cards follow the curve/angle of the spline.
            Quaternion worldRot = Quaternion.LookRotation(
                splineContainer.transform.TransformDirection(up),
                Vector3.Cross(
                    splineContainer.transform.TransformDirection(up),
                    splineContainer.transform.TransformDirection(forward)
                ).normalized
            );

            // Store the card's base hand position.
            //
            // The Card script uses this when returning from hover/use animations.
            handCards[i].SetBasePosition(worldPos);

            // Animate the card into position.
            handCards[i].transform.DOMove(worldPos, 0.25f).SetEase(Ease.OutQuad);

            // Animate the card into rotation.
            handCards[i].transform.DORotateQuaternion(worldRot, 0.25f).SetEase(Ease.OutQuad);
        }
    }

    // Returns true if there is currently an active card.
    //
    // EnemyTargeting uses this before allowing attacks.
    public bool HasActiveCard()
    {
        return currentCardIndex < handCards.Count;
    }

    // Handles the enemy turn after the player uses all cards.
    private IEnumerator EnemyTurn()
    {
        // Small delay before enemies act.
        yield return new WaitForSeconds(0.5f);

        // Copy the current enemy list.
        //
        // Copying helps avoid problems if enemies die or are removed
        // while this turn is processing.
        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        // If there are no enemies, stop the enemy turn.
        if (enemies.Count == 0)
            yield break;

        // Each enemy attacks the player.
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.AttackPlayer();

                // Small delay between enemy attacks.
                yield return new WaitForSeconds(0.3f);
            }
        }

        // After attacking, process enemy status effects.
        //
        // Poison, Burn, and Bleed damage happen here.
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.ProcessEffects();
        }

        // Small delay before returning to the player's turn.
        yield return new WaitForSeconds(0.5f);

        // Start a new player turn.
        StartCoroutine(StartNewTurn());
    }

    // Resets the hand so the player can use cards again.
    private IEnumerator StartNewTurn()
    {
        // Brief delay before reactivating the hand.
        yield return new WaitForSeconds(0.3f);

        // Reset to the first card.
        currentCardIndex = 0;

        // Temporarily set every card inactive.
        foreach (var card in handCards)
        {
            if (card == null)
                continue;

            card.SetActive(false);
        }

        // Activate the first card again.
        UpdateActiveCard();
    }

    // Clears every card from the hand.
    //
    // This is called when:
    // - combat starts, before creating a new hand
    // - combat ends
    public void ClearHand()
    {
        // Stop enemy turn / new turn coroutines running on this manager.
        StopAllCoroutines();

        foreach (var card in handCards)
        {
            if (card == null)
                continue;

            // Stop current DOTween animations on this card.
            card.transform.DOKill();

            // Create a small disappearing animation.
            Sequence seq = DOTween.Sequence();

            // Shrink card to zero.
            seq.Append(card.transform.DOScale(Vector3.zero, 0.2f));

            // Move card slightly downward while shrinking.
            seq.Join(card.transform.DOMoveY(card.transform.position.y - 0.5f, 0.2f));

            // Destroy the card when the animation finishes.
            seq.OnComplete(() => Destroy(card.gameObject));
        }

        // Clear the hand list.
        handCards.Clear();

        // Reset the active card index.
        currentCardIndex = 0;
    }
}