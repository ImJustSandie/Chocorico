using UnityEngine;

public class AguardienteItem : WallObject
{
    public enum Tier
    {
        Tier1,
        Tier2,
        Tier3
    }

    [Header("Aguardiente")]
    [SerializeField] private Tier tier = Tier.Tier1;

    [Header("Sprites por Tier")]
    [Tooltip("Sprite opcional para Tier 1")]
    [SerializeField] private Sprite tier1Sprite;
    [Tooltip("Sprite opcional para Tier 2")]
    [SerializeField] private Sprite tier2Sprite;
    [Tooltip("Sprite opcional para Tier 3")]
    [SerializeField] private Sprite tier3Sprite;

    [Header("Colores por Tier (Fallback si no hay sprite o tinte)")]
    [SerializeField] private Color tier1Color = Color.yellow;
    [SerializeField] private Color tier2Color = Color.blue;
    [SerializeField] private Color tier3Color = Color.green;

    [Header("Duraciones")]
    [SerializeField] private float tier1Duration = 2f;
    [SerializeField] private float tier2Duration = 4f;
    [SerializeField] private float tier3Duration = 6f;

    public void SetTier(Tier newTier)
    {
        tier = newTier;
        ApplyTierVisuals();
    }

    void Start()
    {
        ApplyTierVisuals();
    }

    protected override void OnPlayerHit(PlayerManager player, EnergyManager energyManager)
    {
        float duration = GetDuration();
        energyManager.PauseDrain(duration);
        Destroy(gameObject);
    }

    private float GetDuration()
    {
        switch (tier)
        {
            case Tier.Tier1: return tier1Duration;
            case Tier.Tier2: return tier2Duration;
            case Tier.Tier3: return tier3Duration;
            default: return tier1Duration;
        }
    }

    private void ApplyTierVisuals()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            Sprite targetSprite = null;
            Color fallbackColor = Color.white;

            switch (tier)
            {
                case Tier.Tier1:
                    targetSprite = tier1Sprite;
                    fallbackColor = tier1Color;
                    break;
                case Tier.Tier2:
                    targetSprite = tier2Sprite;
                    fallbackColor = tier2Color;
                    break;
                case Tier.Tier3:
                    targetSprite = tier3Sprite;
                    fallbackColor = tier3Color;
                    break;
            }

            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
                spriteRenderer.color = Color.white; // Respetar colores originales del sprite
            }
            else
            {
                spriteRenderer.color = fallbackColor;
            }
        }
    }
}
