using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Splines;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 10;
    private int currentHP;

    [Header("UI")]
    [SerializeField] private TMP_Text hpText;

    [Header("Combat")]
    public int attackDamage = 2;
    public bool isBoss = false;

    [Header("Effects UI")]
    [SerializeField] private Transform effectContainer;
    [SerializeField] private GameObject effectIconPrefab;
    [SerializeField] private EffectDatabase effectDatabase;

    [Header("Optional Layout")]
    [SerializeField] private bool useSplineLayout = false;
    [SerializeField] private SplineContainer splineContainer;

    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    void Start()
    {
        currentHP = maxHP;

        EnemyManager.Instance?.RegisterEnemy(this);
        effectDatabase?.Init();

        UpdateUI();
    }

    public int GetCurrentHP() => currentHP;

    public bool HasEffect(EffectType type)
    {
        return activeEffects.Exists(e => e.type == type);
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateUI();

        if (currentHP <= 0)
            Die();
    }

    public void ApplyEffect(CardEffect effect)
    {
        ActiveEffect existing = activeEffects.Find(e => e.type == effect.effectType);

        if (existing != null)
        {
            existing.duration += effect.duration;
            existing.value = Mathf.Max(existing.value, effect.value);
        }
        else
        {
            activeEffects.Add(new ActiveEffect(effect));
        }

        RebuildEffectUI();
    }

    private void RebuildEffectUI()
    {
        if (effectContainer == null || effectIconPrefab == null || effectDatabase == null)
            return;

        // Sort effects for consistency
        activeEffects.Sort((a, b) => a.type.CompareTo(b.type));

        // Clear old UI
        foreach (Transform child in effectContainer)
            Destroy(child.gameObject);

        // Rebuild icons
        foreach (var effect in activeEffects)
        {
            GameObject obj = Instantiate(effectIconPrefab, effectContainer);
            EffectIconUI ui = obj.GetComponent<EffectIconUI>();

            ui.Setup(effectDatabase.GetIcon(effect.type), effect.duration);

            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(1f, 0.15f);
        }

        ArrangeEffects();
    }

    private void ArrangeEffects()
    {
        if (useSplineLayout && splineContainer != null)
        {
            Spline spline = splineContainer.Spline;

            float width = 0.8f;
            float spacing = activeEffects.Count <= 1 ? 0 : width / (activeEffects.Count - 1);

            for (int i = 0; i < effectContainer.childCount; i++)
            {
                float t = 0.5f - width / 2f + spacing * i;

                Vector3 localPos = spline.EvaluatePosition(t);
                Vector3 worldPos = splineContainer.transform.TransformPoint(localPos);

                effectContainer.GetChild(i).position = worldPos;
            }
        }
        else
        {
            float spacing = 0.5f;

            for (int i = 0; i < effectContainer.childCount; i++)
            {
                Transform child = effectContainer.GetChild(i);
                child.localPosition = new Vector3(i * spacing, 0f, 0f);
            }
        }
    }

    public void ProcessEffects()
    {
        foreach (var effect in activeEffects)
        {
            switch (effect.type)
            {
                case EffectType.Poison:
                case EffectType.Burn:
                case EffectType.Bleed:
                    TakeDamage(effect.value);
                    break;
            }

            effect.duration--;
        }

        activeEffects.RemoveAll(e => e.duration <= 0);

        RebuildEffectUI();
    }

    public void AttackPlayer()
    {
        if (HasEffect(EffectType.Stun) || HasEffect(EffectType.Sleep))
            return;

        PlayerHealth.Instance.TakeDamage(attackDamage);
    }

    public void TryFlee(float chance)
    {
        if (isBoss) return;

        if (Random.value <= chance)
            Die();
    }

    private void UpdateUI()
    {
        if (hpText != null)
            hpText.text = $"{currentHP}/{maxHP}";
    }

    private void Die()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        Destroy(gameObject);
    }
}