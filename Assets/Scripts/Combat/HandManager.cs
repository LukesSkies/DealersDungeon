using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

// Manages the player's combat hand.
public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Setup")]

    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform spawnPoint;

    [Header("Fallback Deck - Used If No Deck Builder Deck Exists")]

    [SerializeField] private List<CardData> deck = new List<CardData>();

    // Cards currently in the player's hand.
    private readonly List<Card> handCards = new List<Card>();

    // The current card the player can use.
    private int currentCardIndex = 0;

    private Coroutine enemyTurnCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    // Starts the player's hand for combat.
    public void StartCombatHand()
    {
        ClearHand();

        currentCardIndex = 0;

        CreateHandFromDeck();

        UpdateActiveCard();
    }

    // Creates cards from the selected deck.
    private void CreateHandFromDeck()
    {
        handCards.Clear();

        if (cardPrefab == null)
        {
            Debug.LogWarning("HandManager has no card prefab assigned.");
            return;
        }

        List<CardData> sourceDeck = GetDeckForCombat();

        if (sourceDeck == null || sourceDeck.Count == 0)
        {
            Debug.LogWarning("HandManager could not create a hand because the combat deck is empty.");
            return;
        }

        for (int i = 0; i < sourceDeck.Count; i++)
        {
            CardData data = sourceDeck[i];

            if (data == null)
                continue;

            Card card = CreateCard(data, handCards.Count);

            if (card != null)
                handCards.Add(card);
        }

        UpdateCardPositions();
    }

    // Gets the deck to use in combat.
    private List<CardData> GetDeckForCombat()
    {
        if (DeckRuntimeManager.Instance != null && DeckRuntimeManager.Instance.HasSelectedCards())
            return DeckRuntimeManager.Instance.GetCombatDeck();

        List<CardData> fallbackDeck = new List<CardData>();

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i] != null)
                fallbackDeck.Add(deck[i]);
        }

        return fallbackDeck;
    }

    // Creates one card object.
    private Card CreateCard(CardData data, int siblingIndex)
    {
        if (data == null)
            return null;

        if (cardPrefab == null)
            return null;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject instance = Instantiate(cardPrefab, pos, rot, transform);

        instance.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, transform.childCount - 1));

        Card card = instance.GetComponent<Card>();

        if (card == null)
            card = instance.AddComponent<Card>();

        card.SetCardData(data);

        CardVisualAutoBinder visualBinder = instance.GetComponentInChildren<CardVisualAutoBinder>(true);

        if (visualBinder != null)
            visualBinder.Refresh();

        return card;
    }

    // Gets the current active card.
    public Card GetActiveCard()
    {
        if (currentCardIndex < 0 || currentCardIndex >= handCards.Count)
            return null;

        return handCards[currentCardIndex];
    }

    // Checks if there is an active card.
    public bool HasActiveCard()
    {
        return currentCardIndex >= 0 && currentCardIndex < handCards.Count;
    }

    // Gets all cards in hand.
    public IReadOnlyList<Card> GetHandCards()
    {
        return handCards;
    }

    // Gets the current card index.
    public int GetCurrentCardIndex()
    {
        return currentCardIndex;
    }

    // Uses the current card and moves to the next one.
    public void UseCurrentCard()
    {
        if (currentCardIndex >= handCards.Count)
            return;

        Card usedCard = handCards[currentCardIndex];

        StartCoroutine(PlayCardUse(usedCard));

        currentCardIndex++;

        UpdateActiveCard();

        if (currentCardIndex >= handCards.Count)
        {
            if (enemyTurnCoroutine != null)
                StopCoroutine(enemyTurnCoroutine);

            enemyTurnCoroutine = StartCoroutine(EnemyTurn());
        }
    }

    // Plays the used card animation.
    private IEnumerator PlayCardUse(Card card)
    {
        if (card == null)
            yield break;

        card.transform.DOKill();

        card.transform.DOScale(card.transform.localScale * 1.2f, 0.1f);
        card.transform.DOMoveY(card.transform.position.y + 0.3f, 0.1f);

        yield return new WaitForSeconds(0.15f);

        if (card != null)
            card.SetUsed();
    }

    // Updates which card is active.
    private void UpdateActiveCard()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            if (handCards[i] == null)
                continue;

            if (i < currentCardIndex)
                handCards[i].SetUsed();
            else
                handCards[i].SetActive(i == currentCardIndex);
        }
    }

    // Places cards along the spline.
    private void UpdateCardPositions()
    {
        handCards.RemoveAll(card => card == null);

        if (handCards.Count == 0)
            return;

        if (splineContainer == null || splineContainer.Spline == null)
        {
            UpdateCardPositionsFallback();
            return;
        }

        Spline spline = splineContainer.Spline;

        float center = 0.5f;
        float handWidth = 0.6f;
        float spacing = handCards.Count == 1 ? 0f : handWidth / (handCards.Count - 1);

        for (int i = 0; i < handCards.Count; i++)
        {
            if (handCards[i] == null)
                continue;

            float t = center - handWidth / 2f + spacing * i;

            Vector3 localPos = spline.EvaluatePosition(t);
            Vector3 forward = spline.EvaluateTangent(t);
            Vector3 up = spline.EvaluateUpVector(t);

            Vector3 worldPos = splineContainer.transform.TransformPoint(localPos);

            Quaternion worldRot = Quaternion.LookRotation(
                splineContainer.transform.TransformDirection(up),
                Vector3.Cross(
                    splineContainer.transform.TransformDirection(up),
                    splineContainer.transform.TransformDirection(forward)
                ).normalized
            );

            handCards[i].SetBasePosition(worldPos);

            handCards[i].transform.DOMove(worldPos, 0.25f).SetEase(Ease.OutQuad);
            handCards[i].transform.DORotateQuaternion(worldRot, 0.25f).SetEase(Ease.OutQuad);
        }
    }

    // Places cards in a straight line if there is no spline.
    private void UpdateCardPositionsFallback()
    {
        float spacing = 0.25f;
        float startX = -(handCards.Count - 1) * spacing * 0.5f;

        for (int i = 0; i < handCards.Count; i++)
        {
            if (handCards[i] == null)
                continue;

            Vector3 target = transform.position + new Vector3(startX + i * spacing, 0f, 0f);

            handCards[i].SetBasePosition(target);

            handCards[i].transform.DOMove(target, 0.25f).SetEase(Ease.OutQuad);
        }
    }

    // Runs the enemy turn.
    private IEnumerator EnemyTurn()
    {
        yield return new WaitForSeconds(0.5f);

        List<Enemy> enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies.RemoveAll(enemy => enemy == null || enemy.GetCurrentHP() <= 0);

        if (enemies.Count == 0)
        {
            enemyTurnCoroutine = null;
            yield break;
        }

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.GetCurrentHP() <= 0)
                continue;

            enemy.AttackPlayer();

            yield return new WaitForSeconds(0.3f);
        }

        enemies = EnemyManager.Instance == null
            ? new List<Enemy>()
            : new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        enemies.RemoveAll(enemy => enemy == null || enemy.GetCurrentHP() <= 0);

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.GetCurrentHP() <= 0)
                continue;

            enemy.ProcessEffects();
        }

        if (BuffManager.Instance != null)
            BuffManager.Instance.OnTurnEnd();

        yield return new WaitForSeconds(0.5f);

        enemyTurnCoroutine = null;

        StartCoroutine(StartNewTurn());
    }

    // Starts the next player turn.
    private IEnumerator StartNewTurn()
    {
        yield return new WaitForSeconds(0.3f);

        handCards.RemoveAll(card => card == null);

        currentCardIndex = 0;

        foreach (Card card in handCards)
        {
            if (card != null)
                card.SetActive(false);
        }

        UpdateCardPositions();

        UpdateActiveCard();
    }

    // Gets the card before another card.
    public Card GetPreviousCard(Card card)
    {
        int index = handCards.IndexOf(card);

        if (index > 0)
            return handCards[index - 1];

        return null;
    }

    // Gets the card after another card.
    public Card GetNextCard(Card card)
    {
        int index = handCards.IndexOf(card);

        if (index >= 0 && index < handCards.Count - 1)
            return handCards[index + 1];

        return null;
    }

    // Gets a random card in hand.
    public Card GetRandomCardInHand()
    {
        List<Card> validCards = handCards
            .Where(card => card != null)
            .ToList();

        if (validCards.Count == 0)
            return null;

        return validCards[UnityEngine.Random.Range(0, validCards.Count)];
    }

    // Clones a card after the current card.
    public void CloneCardAfterCurrent(Card source)
    {
        if (source == null || source.GetCardData() == null || cardPrefab == null)
            return;

        int sourceIndex = handCards.IndexOf(source);

        if (sourceIndex < 0)
            sourceIndex = currentCardIndex;

        int insertIndex = Mathf.Clamp(sourceIndex + 1, 0, handCards.Count);

        Card clone = CreateCard(source.GetCardData(), insertIndex);

        if (clone == null)
            return;

        clone.CopyTemporaryStateFrom(source);

        handCards.Insert(insertIndex, clone);

        UpdateCardPositions();
        UpdateActiveCard();
    }

    // Reduces the mana cost of a random remaining card.
    public void ReduceRandomCardCost(float reduction)
    {
        if (reduction <= 0f)
            return;

        List<Card> validCards = handCards
            .Where(card => card != null && handCards.IndexOf(card) >= currentCardIndex)
            .ToList();

        if (validCards.Count == 0)
            return;

        Card chosen = validCards[UnityEngine.Random.Range(0, validCards.Count)];

        chosen.ModifyManaCostTemporary(reduction);
    }

    // Clears all cards from the hand.
    public void ClearHand()
    {
        if (enemyTurnCoroutine != null)
        {
            StopCoroutine(enemyTurnCoroutine);
            enemyTurnCoroutine = null;
        }

        StopAllCoroutines();

        foreach (Card card in handCards)
        {
            if (card == null)
                continue;

            card.transform.DOKill();

            Card capturedCard = card;

            Sequence sequence = DOTween.Sequence();

            sequence.Append(capturedCard.transform.DOScale(Vector3.zero, 0.2f));
            sequence.Join(capturedCard.transform.DOMoveY(capturedCard.transform.position.y - 0.5f, 0.2f));

            sequence.OnComplete(() =>
            {
                if (capturedCard != null)
                    Destroy(capturedCard.gameObject);
            });
        }

        handCards.Clear();

        currentCardIndex = 0;
    }
}