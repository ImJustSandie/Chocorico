using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla el movimiento del jugador: input táctil, salto entre paredes,
/// wall cling y detección de caída.
/// Depende de EnergyManager (mismo GameObject) y GameManager (singleton).
/// </summary>
public class PlayerManager : MonoBehaviour
{
    // === Estado del jugador ===
    private enum PlayerState
    {
        Idle,      // Quieto en la pared, esperando el primer toque
        Clinging,  // Pegado a una pared, deslizándose
        Jumping,   // En el aire, moviéndose hacia la pared opuesta
        Falling    // Sin energía, cayendo
    }

    private enum WallSide
    {
        Left,
        Right
    }

    // === Configuración desde el Inspector ===
    [Header("Movimiento")]
    [Tooltip("Fuerza horizontal del salto hacia la pared opuesta")]
    [SerializeField] private float jumpForceX = 8f;

    [Tooltip("Curvatura vertical durante el salto (fuerza de la semi-parábola hacia arriba o abajo)")]
    [SerializeField] private float jumpArcForce = 4f;

    [Tooltip("Velocidad de deslizamiento al estar pegado a la pared")]
    [SerializeField] private float wallSlideSpeed = 3f;

    [Header("Dirección de las paredes")]
    [Tooltip("Si es true, la pared izquierda mueve hacia arriba. Si es false, mueve hacia abajo.")]
    [SerializeField] private bool leftWallGoesUp = true;

    [Header("Inicio")]
    [Tooltip("Si es true, el personaje empieza pegado a la pared izquierda")]
    [SerializeField] private bool startOnLeftWall = true;

    // === Referencias ===
    private Rigidbody2D rb;
    private EnergyManager energyManager;
    private PlayerState currentState;
    private WallSide currentWall;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        energyManager = GetComponent<EnergyManager>();

        if (rb == null)
        {
            Debug.LogError("PlayerManager: No se encontró Rigidbody2D en el GameObject.");
            enabled = false;
            return;
        }

        if (energyManager == null)
        {
            Debug.LogError("PlayerManager: No se encontró EnergyManager en el GameObject.");
            enabled = false;
            return;
        }

        // Estado inicial: quieto en la pared configurada, esperando el primer toque
        currentWall = startOnLeftWall ? WallSide.Left : WallSide.Right;
        currentState = PlayerState.Idle;
        rb.linearVelocity = Vector2.zero;
    }

    public void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    public void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        // Solo procesar toques si está quieto o pegado a la pared
        if (currentState != PlayerState.Clinging && currentState != PlayerState.Idle)
            return;

        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began)
                continue;

            bool touchedRight = touch.screenPosition.x > Screen.width / 2f;

            // Si está en la pared izquierda, solo salta si toca el lado DERECHO
            if (currentWall == WallSide.Left && touchedRight)
            {
                Jump();
                break;
            }
            // Si está en la pared derecha, solo salta si toca el lado IZQUIERDO
            else if (currentWall == WallSide.Right && !touchedRight)
            {
                Jump();
                break;
            }
        }
    }

    void FixedUpdate()
    {
        // Si se agota la energía pasivamente durante el juego, el jugador cae
        if (currentState != PlayerState.Idle && currentState != PlayerState.Falling && !energyManager.HasEnergy)
        {
            currentState = PlayerState.Falling;
            float fallGravity = GameManager.Instance != null ? GameManager.Instance.FallGravity : 5f;
            rb.linearVelocity = new Vector2(0f, -fallGravity);
            Debug.Log("¡Energía agotada! El jugador cae.");
        }

        // Aplicar curvatura vertical (semi-parábola) mientras está en el aire saltando
        if (currentState == PlayerState.Jumping)
        {
            // Determinar si en la pared actual se iba hacia arriba o hacia abajo
            float verticalDir = (currentWall == WallSide.Left) 
                ? (leftWallGoesUp ? 1f : -1f) 
                : (leftWallGoesUp ? -1f : 1f);

            // Aumentar progresivamente la velocidad vertical en la dirección correspondiente
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + (verticalDir * jumpArcForce * Time.fixedDeltaTime));
        }

        // Delegar la verificación de límites al GameManager
        if (GameManager.Instance != null && GameManager.Instance.IsOffScreen(transform.position))
        {
            GameManager.Instance.PlayerDied();
        }
    }

    /// <summary>
    /// Realiza el salto hacia la pared opuesta, consumiendo energía.
    /// </summary>
    private void Jump()
    {
        // Notificar que el juego comenzó para iniciar el drenado pasivo
        energyManager.StartGame();

        // Verificar energía a través del EnergyManager
        if (!energyManager.TryConsumeJumpEnergy())
        {
            // Sin energía: el jugador cae
            currentState = PlayerState.Falling;
            float fallGravity = GameManager.Instance != null ? GameManager.Instance.FallGravity : 5f;
            rb.linearVelocity = new Vector2(0f, -fallGravity);
            return;
        }

        currentState = PlayerState.Jumping;

        float horizontalDir = (currentWall == WallSide.Left) ? 1f : -1f;
        // Inicia el salto con velocidad vertical en 0 y se curva hacia arriba/abajo en FixedUpdate
        rb.linearVelocity = new Vector2(horizontalDir * jumpForceX, 0f);

        Debug.Log(currentWall == WallSide.Left ? "Salto hacia la derecha" : "Salto hacia la izquierda");
    }

    /// <summary>
    /// Aplica la velocidad de deslizamiento según la pared actual y la configuración.
    /// </summary>
    private void ApplyWallSlide()
    {
        float slideDirection;

        if (currentWall == WallSide.Left)
        {
            slideDirection = leftWallGoesUp ? 1f : -1f;
        }
        else
        {
            slideDirection = leftWallGoesUp ? -1f : 1f;
        }

        rb.linearVelocity = new Vector2(0f, slideDirection * wallSlideSpeed);
    }

    /// <summary>
    /// Detecta colisiones con las paredes usando Tags.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("WallLeft"))
        {
            currentWall = WallSide.Left;
            currentState = PlayerState.Clinging;
            ApplyWallSlide();
            Debug.Log("Pegado a la pared izquierda");
        }
        else if (collision.gameObject.CompareTag("WallRight"))
        {
            currentWall = WallSide.Right;
            currentState = PlayerState.Clinging;
            ApplyWallSlide();
            Debug.Log("Pegado a la pared derecha");
        }
    }
}
