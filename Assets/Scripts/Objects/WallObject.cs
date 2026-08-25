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
    private WallSpawnCoordinator.Side side;
    private bool isRegistered = false;

    public void Initialize(WallDirection dir)
    {
        Initialize(dir, WallSpawnCoordinator.Side.Left);
    }

    public void Initialize(WallDirection dir, WallSpawnCoordinator.Side spawnSide)
    {
        direction = dir;
        hasBeenOnScreen = false;

        // Registrar en el coordinador (púas y aguardiente se excluyen entre sí por pared;
        // las púas además se excluyen globalmente para que nunca queden enfrentadas)
        side = spawnSide;
        if (!isRegistered)
        {
            isRegistered = true;
            if (this is PuaObject)
                WallSpawnCoordinator.RegisterPua(side);
            else
                WallSpawnCoordinator.RegisterAguardiente(side);
        }
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnDestroy()
    {
        if (!isRegistered) return;
        isRegistered = false;

        if (this is PuaObject)
            WallSpawnCoordinator.UnregisterPua(side);
        else
            WallSpawnCoordinator.UnregisterAguardiente(side);
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
