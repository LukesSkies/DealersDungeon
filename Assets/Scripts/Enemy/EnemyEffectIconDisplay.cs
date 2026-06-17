using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

// Shows enemy status effect icons.
public class EnemyEffectIconDisplay : MonoBehaviour
{
    [Header("Required References")]

    [SerializeField] private Transform effectContainer;
    [SerializeField] private GameObject effectIconPrefab;
    [SerializeField] private EffectDatabase effectDatabase;

    [Header("Optional Layout")]

    [SerializeField] private bool useSplineLayout = false;
    [SerializeField] private SplineContainer splineContainer;

    [Header("Fallback Layout")]

    [SerializeField] private float iconSpacing = 0.5f;

    private Enemy enemy;
    private EnemyStatusController statusController;

    // Used to check if effects changed.
    private int lastEffectHash = int.MinValue;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        statusController = GetComponent<EnemyStatusController>();

        if (effectDatabase != null)
            effectDatabase.Init();
    }

    private void LateUpdate()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (statusController == null && enemy != null)
            statusController = EnemyStatusController.Get(enemy);

        int currentHash = CalculateEffectHash();

        if (currentHash != lastEffectHash)
        {
            lastEffectHash = currentHash;
            RebuildEffectUI();
        }
    }

    // Forces icons to refresh.
    public void ForceRefresh()
    {
        lastEffectHash = int.MinValue;
        RebuildEffectUI();
    }

    // Calculates a value based on current effects.
    private int CalculateEffectHash()
    {
        if (statusController == null)
            return 0;

        IReadOnlyList<ActiveEffect> effects = statusController.ActiveEffects;

        unchecked
        {
            int hash = 17;

            for (int i = 0; i < effects.Count; i++)
            {
                ActiveEffect effect = effects[i];

                if (effect == null || effect.duration == 0)
                    continue;

                hash = hash * 31 + effect.type.GetHashCode();
                hash = hash * 31 + effect.value;
                hash = hash * 31 + effect.secondaryValue;
                hash = hash * 31 + effect.duration;
                hash = hash * 31 + effect.stacks;
            }

            return hash;
        }
    }

    // Rebuilds the status icon UI.
    private void RebuildEffectUI()
    {
        if (effectContainer == null || effectIconPrefab == null || effectDatabase == null)
            return;

        foreach (Transform child in effectContainer)
        {
            if (child != null)
                Destroy(child.gameObject);
        }

        if (statusController == null)
            return;

        List<ActiveEffect> visibleEffects = new List<ActiveEffect>();

        IReadOnlyList<ActiveEffect> effects = statusController.ActiveEffects;

        for (int i = 0; i < effects.Count; i++)
        {
            ActiveEffect effect = effects[i];

            if (effect != null && effect.duration != 0)
                visibleEffects.Add(effect);
        }

        visibleEffects.Sort((a, b) => a.type.CompareTo(b.type));

        for (int i = 0; i < visibleEffects.Count; i++)
        {
            ActiveEffect effect = visibleEffects[i];

            GameObject iconObject = Instantiate(effectIconPrefab, effectContainer);

            EffectIconUI iconUI = iconObject.GetComponent<EffectIconUI>();

            if (iconUI != null)
                iconUI.Setup(effectDatabase.GetIcon(effect.type), effect.duration);

            iconObject.transform.localScale = Vector3.zero;
            iconObject.transform.DOScale(1f, 0.15f);
        }

        ArrangeEffectIcons();
    }

    // Positions the icons.
    private void ArrangeEffectIcons()
    {
        if (effectContainer == null)
            return;

        if (useSplineLayout && splineContainer != null && splineContainer.Spline != null)
        {
            Spline spline = splineContainer.Spline;

            float width = 0.8f;
            int count = effectContainer.childCount;
            float spacing = count <= 1 ? 0f : width / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float t = 0.5f - width / 2f + spacing * i;

                Vector3 localPosition = spline.EvaluatePosition(t);
                Vector3 worldPosition = splineContainer.transform.TransformPoint(localPosition);

                effectContainer.GetChild(i).position = worldPosition;
            }
        }
        else
        {
            for (int i = 0; i < effectContainer.childCount; i++)
            {
                Transform child = effectContainer.GetChild(i);
                child.localPosition = new Vector3(i * iconSpacing, 0f, 0f);
            }
        }
    }
}