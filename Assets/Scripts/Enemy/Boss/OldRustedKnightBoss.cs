using TMPro;
using UnityEngine;

// Boss mechanic for The Old Rusted Knight.
// Core mechanic: Rust Lock.
public class OldRustedKnightBoss : BossMechanic
{
    [Header("Rust Lock")]
    [Range(0f, 1f)]
    [SerializeField] private float rustChanceAfterAttack = 0.35f;

    [SerializeField] private int rustedTurnDuration = 2;

    [Range(0f, 1f)]
    [SerializeField] private float rustedDamageReduction = 0.75f;

    [Header("Rust Material")]
    [Tooltip("Renderers that should change material when the boss becomes rusted. If empty, the script can auto-find child renderers.")]
    [SerializeField] private Renderer[] renderersToChange;

    [Tooltip("If true, this script finds all child renderers automatically.")]
    [SerializeField] private bool autoFindRenderers = true;

    [Tooltip("Material shown while the boss is Rusted.")]
    [SerializeField] private Material rustedMaterial;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text bossStateText;

    private bool isRusted;
    private int rustedTurnsRemaining;

    private Material[][] originalMaterials;

    public bool IsRusted => isRusted;
    public int RustedTurnsRemaining => rustedTurnsRemaining;

    protected override void Awake()
    {
        base.Awake();

        CacheRenderers();
        CacheOriginalMaterials();
    }

    protected override void Start()
    {
        base.Start();

        ApplyNormalMaterial();
        UpdateVisuals();
    }

    public override bool TryHandleTurnBeforeAttack(Enemy actingEnemy)
    {
        if (!isRusted)
            return false;

        rustedTurnsRemaining--;

        Debug.Log($"{actingEnemy.name} is rust locked and cannot attack.");

        if (rustedTurnsRemaining <= 0)
            BreakFreeFromRust();

        UpdateVisuals();

        return true;
    }

    public override void OnAfterAttack(Enemy actingEnemy)
    {
        if (isRusted)
            return;

        if (Random.value <= rustChanceAfterAttack)
            BecomeRusted();
    }

    public override int ModifyIncomingDamage(
        Enemy targetEnemy,
        int incomingDamage,
        CardDamageType damageType,
        bool ignoreShield)
    {
        if (!isRusted)
            return incomingDamage;

        if (incomingDamage <= 0)
            return 0;

        bool bypassesRust =
            ignoreShield ||
            damageType == CardDamageType.True;

        if (bypassesRust)
            return incomingDamage;

        float damageMultiplier = 1f - rustedDamageReduction;
        int reducedDamage = Mathf.CeilToInt(incomingDamage * damageMultiplier);

        return Mathf.Max(1, reducedDamage);
    }

    private void BecomeRusted()
    {
        isRusted = true;
        rustedTurnsRemaining = Mathf.Max(1, rustedTurnDuration);

        ApplyRustedMaterial();

        Debug.Log($"{name} becomes Rusted for {rustedTurnsRemaining} turns.");

        UpdateVisuals();
    }

    private void BreakFreeFromRust()
    {
        isRusted = false;
        rustedTurnsRemaining = 0;

        ApplyNormalMaterial();

        Debug.Log($"{name} breaks free from the rust.");

        UpdateVisuals();
    }

    private void CacheRenderers()
    {
        if (!autoFindRenderers)
            return;

        if (renderersToChange != null && renderersToChange.Length > 0)
            return;

        renderersToChange = GetComponentsInChildren<Renderer>(true);
    }

    private void CacheOriginalMaterials()
    {
        if (renderersToChange == null)
            return;

        originalMaterials = new Material[renderersToChange.Length][];

        for (int i = 0; i < renderersToChange.Length; i++)
        {
            Renderer targetRenderer = renderersToChange[i];

            if (targetRenderer == null)
                continue;

            originalMaterials[i] = targetRenderer.materials;
        }
    }

    private void ApplyRustedMaterial()
    {
        if (rustedMaterial == null)
            return;

        if (renderersToChange == null)
            return;

        for (int i = 0; i < renderersToChange.Length; i++)
        {
            Renderer targetRenderer = renderersToChange[i];

            if (targetRenderer == null)
                continue;

            Material[] currentMaterials = targetRenderer.materials;

            for (int m = 0; m < currentMaterials.Length; m++)
                currentMaterials[m] = rustedMaterial;

            targetRenderer.materials = currentMaterials;
        }
    }

    private void ApplyNormalMaterial()
    {
        if (renderersToChange == null)
            return;

        if (originalMaterials == null)
            return;

        for (int i = 0; i < renderersToChange.Length; i++)
        {
            Renderer targetRenderer = renderersToChange[i];

            if (targetRenderer == null)
                continue;

            if (i >= originalMaterials.Length)
                continue;

            if (originalMaterials[i] == null)
                continue;

            targetRenderer.materials = originalMaterials[i];
        }
    }

    private void UpdateVisuals()
    {
        if (bossStateText != null)
        {
            if (isRusted)
                bossStateText.text = "Rusted: " + rustedTurnsRemaining;
            else
                bossStateText.text = "";
        }
    }
}