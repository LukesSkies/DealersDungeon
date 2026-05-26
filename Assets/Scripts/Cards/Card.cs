using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerClickHandler
{
    private bool isActive = false;
    private bool isHovered = false;

    private Vector3 basePosition;

    private SpriteRenderer spriteRenderer;
    private MeshRenderer meshRenderer;
    private Color originalColor;

    [SerializeField] private float baseScale = 0.1f;
    [SerializeField] private float activeMultiplier = 2f;
    [SerializeField] private float hoverMultiplier = 2.5f;

    private Vector3 normalScale;
    private Vector3 activeScale;
    private Vector3 hoverScale;

    [SerializeField] private CardData cardData;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else if (meshRenderer != null)
            originalColor = meshRenderer.material.color;

        UpdateScales();
    }

    private void UpdateScales()
    {
        normalScale = Vector3.one * baseScale;
        activeScale = normalScale * activeMultiplier;
        hoverScale = normalScale * hoverMultiplier;

        transform.localScale = normalScale;
    }

    public void SetCardData(CardData data) => cardData = data;
    public bool IsActive() => isActive;

    public void SetHovered(bool hovered)
    {
        if (!isActive || isHovered == hovered) return;

        isHovered = hovered;

        transform.DOKill();

        if (hovered)
        {
            transform.DOMoveY(basePosition.y + 0.5f, 0.2f);
            transform.DOScale(hoverScale, 0.2f);
        }
        else
        {
            transform.DOMoveY(basePosition.y, 0.2f);
            transform.DOScale(activeScale, 0.2f);
        }
    }

    public void SetBasePosition(Vector3 pos)
    {
        basePosition = pos;

        if (!isHovered)
            transform.position = pos;
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (spriteRenderer != null)
            spriteRenderer.color = active ? originalColor : Color.grey;
        else if (meshRenderer != null)
            meshRenderer.material.color = active ? originalColor : Color.grey;

        transform.DOKill();
        transform.DOScale(active ? activeScale : normalScale, 0.2f);
    }

    public void SetUsed()
    {
        isActive = false;
        isHovered = false;

        transform.DOKill();

        transform.DOScale(normalScale, 0.2f);
        transform.DOMove(basePosition, 0.25f);

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        else if (meshRenderer != null)
            meshRenderer.material.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    }

    public void Click()
    {
        if (!isActive || cardData == null) return;

        if (!ManaManager.Instance.TrySpendMana(cardData.manaCost))
            return;

        ExecuteEffects();

        HandManager.Instance.UseCurrentCard();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Click();
    }

    public void SingleAttack(Enemy target)
    {
        if (!isActive || target == null) return;

        int damage = CalculateDamage(cardData.baseDamage);
        target.TakeDamage(damage);

        ManaManager.Instance.AddMana(1f);
        HandManager.Instance.UseCurrentCard();
    }

    public void MultiAttack()
    {
        if (!isActive) return;

        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.GetAllEnemies());
        if (enemies.Count == 0) return;

        int totalDamage = CalculateDamage(cardData.baseDamage);

        int damagePerEnemy = totalDamage / enemies.Count;
        int remainder = totalDamage % enemies.Count;

        for (int i = 0; i < enemies.Count; i++)
        {
            int finalDamage = damagePerEnemy;

            if (remainder > 0)
            {
                finalDamage++;
                remainder--;
            }

            enemies[i].TakeDamage(finalDamage);
        }

        ManaManager.Instance.AddMana(0.5f);
        HandManager.Instance.UseCurrentCard();
    }

    private void ExecuteEffects()
    {
        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.GetAllEnemies());

        foreach (var effect in cardData.effects)
        {
            switch (effect.effectType)
            {
                case EffectType.Heal:
                    PlayerHealth.Instance.Heal(effect.value);
                    break;

                case EffectType.Shield:
                    PlayerShield.Instance.AddShield(effect.value);
                    break;

                case EffectType.DamageBuff:
                    BuffManager.Instance.ApplyDamageBuff(effect.value, effect.duration);
                    break;

                default:
                    foreach (var enemy in enemies)
                    {
                        if (!CheckCondition(effect, enemy)) continue;
                        ApplyEffect(effect, enemy);
                    }
                    break;
            }
        }
    }

    private bool CheckCondition(CardEffect effect, Enemy enemy)
    {
        switch (effect.condition)
        {
            case EffectCondition.None: return true;
            case EffectCondition.TargetBelowHalfHP: return enemy.GetCurrentHP() <= enemy.maxHP / 2;
            case EffectCondition.TargetAboveHalfHP: return enemy.GetCurrentHP() > enemy.maxHP / 2;
            case EffectCondition.TargetHasPoison: return enemy.HasEffect(EffectType.Poison);
            case EffectCondition.TargetHasBurn: return enemy.HasEffect(EffectType.Burn);
            case EffectCondition.TargetHasBleed: return enemy.HasEffect(EffectType.Bleed);
            case EffectCondition.TargetIsBoss: return enemy.isBoss;
        }
        return true;
    }

    private void ApplyEffect(CardEffect effect, Enemy enemy)
    {
        switch (effect.effectType)
        {
            case EffectType.Damage:
                enemy.TakeDamage(CalculateDamage(effect.value));
                break;

            case EffectType.AOE:
                enemy.TakeDamage(CalculateDamage(effect.value));
                break;

            case EffectType.HalfHP:
                enemy.TakeDamage(enemy.GetCurrentHP() / 2);
                break;

            case EffectType.Poison:
            case EffectType.Burn:
            case EffectType.Bleed:
            case EffectType.Stun:
            case EffectType.Sleep:
            case EffectType.Confusion:
            case EffectType.Cripple:
                enemy.ApplyEffect(effect);
                break;

            case EffectType.Flee:
                enemy.TryFlee(effect.chance);
                break;

            case EffectType.DetonateDOT:
                Detonate(enemy);
                break;
        }
    }

    private int CalculateDamage(int baseValue)
    {
        float multiplier = BuffManager.Instance.GetDamageMultiplier();
        return Mathf.RoundToInt(baseValue * multiplier);
    }

    private void Detonate(Enemy enemy)
    {
        int bonus = 0;

        if (enemy.HasEffect(EffectType.Poison)) bonus += 2;
        if (enemy.HasEffect(EffectType.Burn)) bonus += 2;
        if (enemy.HasEffect(EffectType.Bleed)) bonus += 3;

        enemy.TakeDamage(bonus * 2);
    }
}