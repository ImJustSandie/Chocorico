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
        Falling,   // Sin energía, cayendo
        Bouncing   // Rebotando tras golpear púas en el aire
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

    [Tooltip("Fuerza o impulso vertical inicial del salto (en la dirección del movimiento de la pared actual)")]
    [SerializeField] private float jumpInitialForceY = 6f;

    [Tooltip("Fuerza de arco/gravedad opuesta durante el salto que crea la parábola")]
    [SerializeField] private float jumpArcGravity = 12f;

    [Tooltip("Velocidad de deslizamiento al estar pegado a la pared")]
    [SerializeField] private float wallSlideSpeed = 3f;

    [Header("Dirección de las paredes")]
    [Tooltip("Si es true, la pared izquierda mueve hacia arriba. Si es false, mueve hacia abajo.")]
    [SerializeField] private bool leftWallGoesUp = true;

    [Header("Mecánicas Opcionales / Testing")]
    [Tooltip("Permite cancelar el salto en el aire devolviéndose a la pared de origen a cambio de energía")]
    [SerializeField] private bool enableCancelJump = true;

    [Header("Inicio")]
    [Tooltip("Si es true, el personaje empieza pegado a la pared izquierda")]
    [SerializeField] private bool startOnLeftWall = true;

    // === Referencias ===
    private Rigidbody2D rb;
    private EnergyManager energyManager;
    private PlayerState currentState;
    private WallSide currentWall;
    private float bounceTimer = 0f;
    private float bounceHorizontalDir = 0f;
    private float bounceForce = 0f;

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
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began)
                continue;

            bool touchedRight = touch.screenPosition.x > Screen.width / 2f;

            // 1. Si está en la pared (Idle o Clinging): Salto hacia la pared opuesta
            if (currentState == PlayerState.Clinging || currentState == PlayerState.Idle)
            {
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
            // 2. Si está en medio de un salto (Jumping): Cancelar y devolverse a la pared de la que salió (si está habilitado)
            else if (enableCancelJump && currentState == PlayerState.Jumping)
            {
                // Si saltó desde la izquierda y toca la IZQUIERDA -> vuelve a la izquierda
                if (currentWall == WallSide.Left && !touchedRight)
                {
                    CancelJump();
                    break;
                }
                // Si saltó desde la derecha y toca la DERECHA -> vuelve a la derecha
                else if (currentWall == WallSide.Right && touchedRight)
                {
                    CancelJump();
                    break;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (currentState == PlayerState.Bouncing)
        {
            bounceTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(bounceHorizontalDir * bounceForce, 0f);

            if (bounceTimer <= 0f)
            {
                // Continúa avanzando en dirección contraria a las púas (de vuelta a la pared de origen)
                currentState = PlayerState.Jumping;
                rb.linearVelocity = new Vector2(bounceHorizontalDir * jumpForceX, 0f);
            }
            return;
        }

        // Si se agota la energía pasivamente durante el juego, el jugador cae
        if (currentState != PlayerState.Idle && currentState != PlayerState.Falling && !energyManager.HasEnergy)
        {
            currentState = PlayerState.Falling;
            float fallGravity = GameManager.Instance != null ? GameManager.Instance.FallGravity : 5f;
            rb.linearVelocity = new Vector2(0f, -fallGravity);
            Debug.Log("¡Energía agotada! El jugador cae.");
        }

        // Aplicar curvatura de parábola mientras está en el aire saltando
        if (currentState == PlayerState.Jumping)
        {
            // Determinar la dirección inicial del salto según la pared de la que salió
            float verticalDir = (currentWall == WallSide.Left) 
                ? (leftWallGoesUp ? 1f : -1f) 
                : (leftWallGoesUp ? -1f : 1f);

            // Reducir progresivamente el impulso inicial para formar la parábola (fuerza en sentido contrario)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - (verticalDir * jumpArcGravity * Time.fixedDeltaTime));
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
        float verticalDir = (currentWall == WallSide.Left) 
            ? (leftWallGoesUp ? 1f : -1f) 
            : (leftWallGoesUp ? -1f : 1f);

        // Inicia el salto con velocidad horizontal y un impulso vertical inicial según la dirección de la pared
        rb.linearVelocity = new Vector2(horizontalDir * jumpForceX, verticalDir * jumpInitialForceY);

        Debug.Log(currentWall == WallSide.Left ? "Salto hacia la derecha" : "Salto hacia la izquierda");
    }

    /// <summary>
    /// Cancela el salto en el aire devolviendo al jugador hacia la pared de la que salió.
    /// Consume energía (el doble si está activo el debuff del Gansito).
    /// </summary>
    private void CancelJump()
    {
        // Consumir energía de cancelación
        if (!energyManager.TryConsumeCancelJumpEnergy())
        {
            // Sin energía: el jugador cae
            currentState = PlayerState.Falling;
            float fallGravity = GameManager.Instance != null ? GameManager.Instance.FallGravity : 5f;
            rb.linearVelocity = new Vector2(0f, -fallGravity);
            return;
        }

        // Dirección de regreso hacia la pared de origen
        float returnHorizontalDir = (currentWall == WallSide.Left) ? -1f : 1f;

        // Invertir la velocidad horizontal hacia la pared de origen, conservando la velocidad vertical actual
        rb.linearVelocity = new Vector2(returnHorizontalDir * jumpForceX, rb.linearVelocity.y);

        Debug.Log($"Salto cancelado: regresando a la pared {currentWall}");
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

    public bool IsInAirState()
    {
        return currentState == PlayerState.Jumping || currentState == PlayerState.Falling || currentState == PlayerState.Bouncing;
    }

    public void ApplyBounce(float horizontalDir, float force, float duration)
    {
        currentState = PlayerState.Bouncing;
        bounceHorizontalDir = horizontalDir;
        bounceForce = force;
        bounceTimer = duration;
        rb.linearVelocity = new Vector2(horizontalDir * force, 0f);
        Debug.Log("Rebote de púas aplicado");
    }
}
