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

    [Header("Configuración por Tier")]
    [Tooltip("Multiplicador de slowdown de las cintas del Tier 1 (0.5 = 50% más lento)")]
    [SerializeField] private float tier1SlowdownMultiplier = 0.5f;
    [Tooltip("Duración del efecto de slowdown del Tier 1 (segundos)")]
    [SerializeField] private float tier1SlowdownDuration = 3f;

    [Tooltip("Duración de la pausa del drenaje pasivo del Tier 2 (segundos). Solo pausa la pérdida pasiva: las púas siguen drenando energía.")]
    [SerializeField] private float tier2Duration = 4f;

    [Header("Puntuación por Tier")]
    [Tooltip("Puntos otorgados al recoger el Aguardiente de Tier 1")]
    [SerializeField] private int tier1Score = 10;

    [Tooltip("Puntos otorgados al recoger el Aguardiente de Tier 2")]
    [SerializeField] private int tier2Score = 20;

    [Tooltip("Puntos otorgados al recoger el Aguardiente de Tier 3")]
    [SerializeField] private int tier3Score = 30;

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
        int score;

        switch (tier)
        {
            case Tier.Tier1:
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.ApplyConveyorSlowdown(tier1SlowdownDuration, tier1SlowdownMultiplier);
                }
                score = tier1Score;
                break;

            case Tier.Tier2:
                // Tier 2: pausa SOLO el drenaje pasivo; DrainEnergy (púas, ítems negativos) sigue funcionando
                energyManager.PauseDrain(tier2Duration);
                score = tier2Score;
                break;

            case Tier.Tier3:
                // Tier 3: rellena la barra de energía al máximo
                energyManager.AddEnergy(energyManager.MaxEnergy);
                score = tier3Score;
                break;

            default:
                score = 0;
                break;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddPoints(score);
        }

        Destroy(gameObject);
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
