using System.Collections;
using System.Collections.Generic;
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

    private List<Card> handCards = new();
    private int currentCardIndex = 0;

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
            GameObject g = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation, transform);

            Card card = g.GetComponent<Card>();
            card.SetCardData(deck[i]);

            handCards.Add(card);
        }

        UpdateCardPositions();
    }

    public Card GetActiveCard()
    {
        if (currentCardIndex >= handCards.Count) return null;
        return handCards[currentCardIndex];
    }

    public void UseCurrentCard()
    {
        if (currentCardIndex >= handCards.Count) return;

        Card usedCard = handCards[currentCardIndex];

        StartCoroutine(PlayCardUse(usedCard));

        currentCardIndex++;
        UpdateActiveCard();

        if (currentCardIndex >= handCards.Count)
        {
            StartCoroutine(EnemyTurn());
        }
    }

    private IEnumerator PlayCardUse(Card card)
    {
        card.transform.DOKill();

        card.transform.DOScale(card.transform.localScale * 1.2f, 0.1f);
        card.transform.DOMoveY(card.transform.position.y + 0.3f, 0.1f);

        yield return new WaitForSeconds(0.15f);

        card.SetUsed();
    }

    private void UpdateActiveCard()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            if (i < currentCardIndex)
                handCards[i].SetUsed();
            else
                handCards[i].SetActive(i == currentCardIndex);
        }
    }

    private void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;

        Spline spline = splineContainer.Spline;

        float center = 0.5f;
        float handWidth = 0.6f;
        float spacing = (handCards.Count == 1) ? 0f : handWidth / (handCards.Count - 1);

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

    public bool HasActiveCard()
    {
        return currentCardIndex < handCards.Count;
    }

    private IEnumerator EnemyTurn()
    {
        yield return new WaitForSeconds(0.5f);

        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        if (enemies.Count == 0)
            yield break;

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.AttackPlayer();
                yield return new WaitForSeconds(0.3f);
            }
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.ProcessEffects();
        }

        yield return new WaitForSeconds(0.5f);

        StartCoroutine(StartNewTurn());
    }

    private IEnumerator StartNewTurn()
    {
        yield return new WaitForSeconds(0.3f);

        currentCardIndex = 0;

        foreach (var card in handCards)
        {
            if (card == null) continue;
            card.SetActive(false);
        }

        UpdateActiveCard();
    }

    public void ClearHand()
    {
        StopAllCoroutines();

        foreach (var card in handCards)
        {
            if (card == null) continue;

            card.transform.DOKill();

            Sequence seq = DOTween.Sequence();
            seq.Append(card.transform.DOScale(Vector3.zero, 0.2f));
            seq.Join(card.transform.DOMoveY(card.transform.position.y - 0.5f, 0.2f));
            seq.OnComplete(() => Destroy(card.gameObject));
        }

        handCards.Clear();
        currentCardIndex = 0;
    }
}