using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Setup")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform spawnPoint;

    [Header("Deck (ORDER MATTERS)")]
    [SerializeField] private List<CardData> deck = new List<CardData>();

    private readonly List<Card> handCards = new List<Card>();
    private int currentCardIndex = 0;
    private Coroutine enemyTurnCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    public void StartCombatHand()
    {
        ClearHand();
        CreateHandFromDeck();
        UpdateActiveCard();
    }

    private void CreateHandFromDeck()
    {
        handCards.Clear();

        for (int i = 0; i < deck.Count; i++)
        {
            Card card = CreateCard(deck[i], handCards.Count);
            handCards.Add(card);
        }

        UpdateCardPositions();
    }

    private Card CreateCard(CardData data, int siblingIndex)
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject instance = Instantiate(cardPrefab, pos, rot, transform);
        instance.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, transform.childCount - 1));

        Card card = instance.GetComponent<Card>();

        if (card == null)
            card = instance.AddComponent<Card>();

        card.SetCardData(data);
        return card;
    }

    public Card GetActiveCard()
    {
        if (currentCardIndex < 0 || currentCardIndex >= handCards.Count)
            return null;

        return handCards[currentCardIndex];
    }

    public bool HasActiveCard()
    {
        return currentCardIndex >= 0 && currentCardIndex < handCards.Count;
    }

    public IReadOnlyList<Card> GetHandCards()
    {
        return handCards;
    }

    public int GetCurrentCardIndex()
    {
        return currentCardIndex;
    }

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

    private void UpdateCardPositionsFallback()
    {
        float spacing = 0.25f;
        float startX = -(handCards.Count - 1) * spacing * 0.5f;

        for (int i = 0; i < handCards.Count; i++)
        {
            Vector3 target = transform.position + new Vector3(startX + i * spacing, 0f, 0f);
            handCards[i].SetBasePosition(target);
            handCards[i].transform.DOMove(target, 0.25f).SetEase(Ease.OutQuad);
        }
    }

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

            EnemyStatusController status = EnemyStatusController.Get(enemy);

            bool actionHandledOrSkipped = status != null && status.TryHandlePreAttack(enemy);

            if (!actionHandledOrSkipped && enemy.GetCurrentHP() > 0)
                enemy.AttackPlayer();

            yield return new WaitForSeconds(0.3f);
        }

        // Enemy status ticks happen after enemies try to act,
        // matching your current poison/burn/bleed timing.
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            EnemyStatusController status = EnemyStatusController.Get(enemy);

            if (status != null)
                status.ProcessTurnEnd(enemy);
            else
                enemy.ProcessEffects();
        }

        if (BuffManager.Instance != null)
            BuffManager.Instance.OnTurnEnd();

        yield return new WaitForSeconds(0.5f);

        enemyTurnCoroutine = null;
        StartCoroutine(StartNewTurn());
    }

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

    public Card GetPreviousCard(Card card)
    {
        int index = handCards.IndexOf(card);

        if (index > 0)
            return handCards[index - 1];

        return null;
    }

    public Card GetNextCard(Card card)
    {
        int index = handCards.IndexOf(card);

        if (index >= 0 && index < handCards.Count - 1)
            return handCards[index + 1];

        return null;
    }

    public Card GetRandomCardInHand()
    {
        List<Card> validCards = handCards.Where(card => card != null).ToList();

        if (validCards.Count == 0)
            return null;

        return validCards[UnityEngine.Random.Range(0, validCards.Count)];
    }

    public void CloneCardAfterCurrent(Card source)
    {
        if (source == null || source.GetCardData() == null || cardPrefab == null)
            return;

        int sourceIndex = handCards.IndexOf(source);

        if (sourceIndex < 0)
            sourceIndex = currentCardIndex;

        int insertIndex = Mathf.Clamp(sourceIndex + 1, 0, handCards.Count);

        Card clone = CreateCard(source.GetCardData(), insertIndex);
        clone.CopyTemporaryStateFrom(source);

        handCards.Insert(insertIndex, clone);

        UpdateCardPositions();
        UpdateActiveCard();
    }

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
