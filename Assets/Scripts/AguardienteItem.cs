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

    [Header("Colores por Tier")]
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
        ApplyTierColor();
    }

    void Start()
    {
        ApplyTierColor();
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

    private void ApplyTierColor()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            switch (tier)
            {
                case Tier.Tier1:
                    spriteRenderer.color = tier1Color;
                    break;
                case Tier.Tier2:
                    spriteRenderer.color = tier2Color;
                    break;
                case Tier.Tier3:
                    spriteRenderer.color = tier3Color;
                    break;
            }
        }
    }
}
