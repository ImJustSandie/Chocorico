using UnityEngine;

public enum WallDirection
{
    Up,
    Down
}

[RequireComponent(typeof(Collider2D))]
public abstract class WallObject : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] protected float moveSpeed = 3f;

    protected WallDirection direction;
    protected bool hasBeenOnScreen = false;
    protected SpriteRenderer spriteRenderer;

    public void Initialize(WallDirection dir)
    {
        direction = dir;
        hasBeenOnScreen = false;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        float dirY = (direction == WallDirection.Up) ? 1f : -1f;
        transform.position += new Vector3(0f, dirY * moveSpeed * Time.deltaTime, 0f);

        if (!hasBeenOnScreen)
        {
            if (GameManager.Instance != null)
            {
                float y = transform.position.y;
                if (y >= GameManager.Instance.ScreenBottom && y <= GameManager.Instance.ScreenTop)
                {
                    hasBeenOnScreen = true;
                }
            }
        }

        if (hasBeenOnScreen && GameManager.Instance != null)
        {
            bool offScreen = false;
            if (direction == WallDirection.Up && transform.position.y > GameManager.Instance.ScreenTop + 2f)
                offScreen = true;
            else if (direction == WallDirection.Down && transform.position.y < GameManager.Instance.ScreenBottom - 2f)
                offScreen = true;

            if (offScreen)
            {
                Destroy(gameObject);
            }
        }
    }

    protected abstract void OnPlayerHit(PlayerManager player, EnergyManager energyManager);

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        PlayerManager player = other.GetComponent<PlayerManager>();
        EnergyManager energyManager = other.GetComponent<EnergyManager>();

        if (player != null && energyManager != null)
        {
            OnPlayerHit(player, energyManager);
        }
    }
}
