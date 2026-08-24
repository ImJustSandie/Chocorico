using UnityEngine;

public class PuaObject : WallObject
{
    [Header("Sprites")]
    [Tooltip("Sprite opcional para las púas")]
    [SerializeField] private Sprite puaSprite;

    [Header("Púas")]
    [SerializeField] private Color puaColor = new Color(0.4f, 0.05f, 0.05f);
    [SerializeField] private float damageAmount = 5f;
    [SerializeField] private float bounceForce = 5f;
    [SerializeField] private float bounceDuration = 0.5f;

    private bool hasHit = false;

    public void SetPuaColor(Color color)
    {
        puaColor = color;
        if (spriteRenderer != null && puaSprite == null)
        {
            spriteRenderer.color = puaColor;
        }
    }

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            if (puaSprite != null)
            {
                spriteRenderer.sprite = puaSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = puaColor;
            }
        }
    }

    protected override void OnPlayerHit(PlayerManager player, EnergyManager energyManager)
    {
        if (hasHit) return;

        energyManager.DrainEnergy(damageAmount);
        hasHit = true;

        bool isJumpingOrFalling = player.IsInAirState();

        if (isJumpingOrFalling)
        {
            // Rebota avanzando en dirección contraria a las púas (hacia la pared de origen)
            float screenCenterX = 0f;
            if (GameManager.Instance != null)
                screenCenterX = (GameManager.Instance.ScreenLeft + GameManager.Instance.ScreenRight) * 0.5f;

            float horizontalDir = (transform.position.x < screenCenterX) ? 1f : -1f;
            player.ApplyBounce(horizontalDir, bounceForce, bounceDuration);
        }
        else
        {
            if (GameManager.Instance != null)
                GameManager.Instance.PlayerDied();
        }

        Invoke(nameof(ResetHit), 0.3f);
    }

    private void ResetHit()
    {
        hasHit = false;
    }
}
