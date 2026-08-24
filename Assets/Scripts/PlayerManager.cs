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
        Charging,  // Manteniendo presionado el lado contrario: avanza contra la cinta antes de saltar
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

    [Tooltip("Velocidad al presionar el lado de la pared actual: avanza a favor de la cinta más rápido")]
    [SerializeField] private float fastSlideSpeed = 6f;

    [Tooltip("Velocidad vertical con la que avanza contra la cinta mientras se mantiene presionado el lado contrario")]
    [SerializeField] private float chargeMoveSpeed = 2f;

    [Tooltip("Tiempo máximo (segundos) de presión para considerar el toque como tap rápido: salto plano sin avance vertical")]
    [SerializeField] private float tapThreshold = 0.15f;

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
    // Dirección vertical actual de la gravedad del arco (se invierte al rebotar en púas)
    private float arcVerticalDir = 0f;
    // Id del toque que está cargando el salto (manteniendo presionado)
    private int chargingTouchId = -1;
    // Momento en que inició la carga del salto
    private float chargeStartTime = 0f;
    // Indica si el salto actual es un tap rápido (plano, sin componente vertical)
    private bool isTapJump = false;
    // Id del toque que mantiene el deslizamiento rápido a favor de la cinta (-1 si ninguno)
    private int fastSlideTouchId = -1;

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

        // Desactivar la gravedad física: la curva del salto la controla jumpArcGravity
        // (si no, la gravedad de Physics2D se suma al subir y se resta al bajar, asimetrizando el arco)
        rb.gravityScale = 0f;

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
        // Si hay un toque de deslizamiento rápido activo, detectar cuando se suelta
        if (fastSlideTouchId != -1)
        {
            bool fastSlideHeld = false;
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.touchId == fastSlideTouchId
                    && touch.phase != UnityEngine.InputSystem.TouchPhase.Ended
                    && touch.phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    fastSlideHeld = true;
                    break;
                }
            }

            if (!fastSlideHeld)
            {
                fastSlideTouchId = -1;
                // Al soltar no salta: vuelve al deslizamiento normal si sigue en la pared
                if (currentState == PlayerState.Clinging || currentState == PlayerState.Idle)
                {
                    ApplyWallSlide();
                }
            }
        }

        // Mientras carga el salto: saltar cuando el dedo se suelte
        if (currentState == PlayerState.Charging)
        {
            bool stillHolding = false;
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.touchId == chargingTouchId
                    && touch.phase != UnityEngine.InputSystem.TouchPhase.Ended
                    && touch.phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    stillHolding = true;
                    break;
                }
            }

            if (!stillHolding)
            {
                chargingTouchId = -1;
                // Tap rápido (presionar y soltar de una): salto plano, sin avance vertical
                bool wasTap = (Time.time - chargeStartTime) <= tapThreshold;
                Jump(wasTap);
            }
            return;
        }

        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began)
                continue;

            bool touchedRight = touch.screenPosition.x > Screen.width / 2f;

            // 1. Si está en la pared (Idle o Clinging)
            if (currentState == PlayerState.Clinging || currentState == PlayerState.Idle)
            {
                bool touchedSameSide = (currentWall == WallSide.Left && !touchedRight)
                    || (currentWall == WallSide.Right && touchedRight);

                if (touchedSameSide)
                {
                    // Presionar el lado de la pared actual: deslizamiento rápido a favor de la cinta (no salta)
                    fastSlideTouchId = touch.touchId;
                    break;
                }

                // Si toca el lado contrario: inicia la carga del salto hacia la pared opuesta
                StartCharge(touch.touchId);
                break;
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
                // El regreso espeja el arco del salto original
                arcVerticalDir = -arcVerticalDir;
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

        // Mientras carga el salto: avanza en sentido contrario a la cinta, sin despegarse de la pared
        if (currentState == PlayerState.Charging)
        {
            rb.linearVelocity = new Vector2(0f, -GetSlideDirection() * chargeMoveSpeed);
        }

        // Deslizamiento rápido: presionar el lado de la pared actual avanza a favor de la cinta más rápido
        if (fastSlideTouchId != -1
            && (currentState == PlayerState.Clinging || currentState == PlayerState.Idle))
        {
            rb.linearVelocity = new Vector2(0f, GetSlideDirection() * fastSlideSpeed);
        }

        // Aplicar curvatura de parábola mientras está en el aire saltando
        if (currentState == PlayerState.Jumping)
        {
            // Reducir progresivamente el impulso inicial para formar la parábola (fuerza en sentido contrario)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - (arcVerticalDir * jumpArcGravity * Time.fixedDeltaTime));
        }

        // Con gravityScale en 0, la caída se acelera manualmente con FallGravity
        if (currentState == PlayerState.Falling)
        {
            float fallGravity = GameManager.Instance != null ? GameManager.Instance.FallGravity : 5f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - fallGravity * Time.fixedDeltaTime);
        }

        // Delegar la verificación de límites al GameManager
        if (GameManager.Instance != null && GameManager.Instance.IsOffScreen(transform.position))
        {
            GameManager.Instance.PlayerDied();
        }
    }

    /// <summary>
    /// Inicia la carga del salto: mientras el dedo se mantenga presionado,
    /// el jugador avanza en sentido contrario a la cinta. Al soltar, salta.
    /// </summary>
    private void StartCharge(int touchId)
    {
        energyManager.StartGame();
        chargingTouchId = touchId;
        chargeStartTime = Time.time;
        currentState = PlayerState.Charging;
        Debug.Log("Cargando salto: avanzando contra la cinta");
    }

    /// <summary>
    /// Realiza el salto hacia la pared opuesta, consumiendo energía.
    /// </summary>
    private void Jump(bool tapJump = false)
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
        isTapJump = tapJump;

        float horizontalDir = (currentWall == WallSide.Left) ? 1f : -1f;

        // Salto invertido: impulso vertical contrario a la dirección de la cinta,
        // el arco curva de vuelta hacia el sentido de la cinta.
        // En un tap rápido no hay avance vertical: salto plano horizontal
        float verticalDir = tapJump ? 0f : -GetSlideDirection();
        arcVerticalDir = tapJump ? 0f : verticalDir;

        // Inicia el salto con velocidad horizontal y un impulso vertical inicial según la dirección de la pared
        rb.linearVelocity = new Vector2(horizontalDir * jumpForceX, verticalDir * jumpInitialForceY);

        Debug.Log(tapJump
            ? "Tap rápido: salto plano hacia la " + (currentWall == WallSide.Left ? "derecha" : "izquierda")
            : "Salto hacia la " + (currentWall == WallSide.Left ? "derecha" : "izquierda"));
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

        // Solo invertir la velocidad horizontal hacia la pared de origen:
        // la velocidad vertical y la gravedad del arco se conservan, así la
        // trayectoria continúa su caída natural de vuelta (sin cambios bruscos)
        rb.linearVelocity = new Vector2(returnHorizontalDir * jumpForceX, rb.linearVelocity.y);

        Debug.Log($"Salto cancelado: regresando a la pared {currentWall}");
    }

    /// <summary>
    /// Devuelve la dirección vertical de la cinta de la pared actual (+1 sube, -1 baja).
    /// </summary>
    private float GetSlideDirection()
    {
        if (currentWall == WallSide.Left)
        {
            return leftWallGoesUp ? 1f : -1f;
        }
        return leftWallGoesUp ? -1f : 1f;
    }

    /// <summary>
    /// Aplica la velocidad de deslizamiento según la pared actual y la configuración.
    /// </summary>
    private void ApplyWallSlide()
    {
        rb.linearVelocity = new Vector2(0f, GetSlideDirection() * wallSlideSpeed);
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
