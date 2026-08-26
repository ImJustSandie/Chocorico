using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla el movimiento del jugador: input táctil, salto entre paredes,
/// wall cling y detección de caída.
/// Inputs: presionar el lado de la pantalla donde está el jugador avanza EN CONTRA
/// de la cinta; presionar el lado contrario salta inmediatamente hacia esa pared.
/// Depende de EnergyManager (mismo GameObject) y GameManager (singleton).
/// </summary>
/// 
/// 
public class PlayerManager : MonoBehaviour
{
    // === Estado del jugador ===
    private enum PlayerState
    {
        Idle,        // Quieto en la pared, esperando el primer toque
        Clinging,    // Pegado a una pared, deslizándose
        AgainstBelt, // Manteniendo presionado el lado actual: avanza contra la cinta
        Jumping,     // En el aire, moviéndose hacia la pared opuesta
        Falling,     // Sin energía, cayendo
        Bouncing     // Rebotando tras golpear púas en el aire
    }

    private enum WallSide
    {
        Left,
        Right
    }

    // === Configuración desde el Inspector ===
    [Header("Movimiento")]
    [Tooltip("Velocidad horizontal del salto (a mayor valor, más cerrado el arco)")]
    [SerializeField] private float jumpForceX = 8f;

    [Tooltip("Gravedad del arco del salto. Junto con jumpForceX define qué tan abierto es: la velocidad vertical inicial se calcula para aterrizar a la misma altura de origen")]
    [SerializeField] private float jumpArcGravity = 12f;

    [Tooltip("Velocidad de deslizamiento al estar pegado a la pared")]
    [SerializeField] private float wallSlideSpeed = 3f;

    [Tooltip("Velocidad vertical con la que avanza contra la cinta mientras se mantiene presionado el lado actual")]
    [SerializeField] private float beltAgainstSpeed = 2f;

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
    // Id del toque que mantiene el avance contra la cinta (-1 si ninguno)
    private int beltTouchId = -1;
    // Escala inicial del modelo (para hacer flip sin perder la escala original)
    private Vector3 initialScale;

    [Header("Animación")]
    [Tooltip("Referencia al Animator del personaje. Expone los parámetros Anim_Clinging y Anim_Jumping.")]
    public Animator animator;

    [Header("God Mode (Testing)")]
    [Tooltip("Activa el God Mode: no pierde energía y revive al centro al caer")]
    [SerializeField] private bool isGodMode = false;

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

        // Estado inicial: en el centro de la pantalla, esperando el primer toque
        currentWall = startOnLeftWall ? WallSide.Left : WallSide.Right;
        currentState = PlayerState.Idle;
        rb.linearVelocity = Vector2.zero;
        initialScale = transform.localScale;
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
        if (GameManager.Instance != null && GameManager.Instance.IsOffScreen(transform.position))
        {
            if (isGodMode)
            {
                RespawnAtWall();
                return;
            }
            else
            {
                GameManager.Instance.PlayerDied();
                return;
            }
        }

        // Mientras no se haya presionado el botón de jugar, ignorar todos los inputs de gameplay
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        // Si hay un toque de avance contra la cinta activo, detectar cuando se suelta
        if (beltTouchId != -1)
        {
            bool beltHeld = false;
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.touchId == beltTouchId
                    && touch.phase != UnityEngine.InputSystem.TouchPhase.Ended
                    && touch.phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    beltHeld = true;
                    break;
                }
            }

            if (!beltHeld)
            {
                StopBeltAgainst();
            }
        }

        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began)
                continue;

            bool touchedRight = touch.screenPosition.x > Screen.width / 2f;

            // 1. Si aún no empezó (en el centro): cualquier lado inicia la partida
            //    saltando hacia la pared de ese lado
            if (currentState == PlayerState.Idle)
            {
                Jump(touchedRight ? WallSide.Right : WallSide.Left);
                break;
            }
            // 2. Pegado a una pared (Clinging o avanzando contra la cinta)
            if (currentState == PlayerState.Clinging || currentState == PlayerState.AgainstBelt)
            {
                bool touchedSameSide = (currentWall == WallSide.Left && !touchedRight)
                    || (currentWall == WallSide.Right && touchedRight);

                if (touchedSameSide)
                {
                    // Presionar el lado de la pared actual: avanza en contra de la cinta mientras se mantenga presionado
                    StartBeltAgainst(touch.touchId);
                }
                else
                {
                    // Presionar el lado contrario: salta inmediatamente hacia la pared opuesta
                    StopBeltAgainst();
                    Jump(currentWall == WallSide.Left ? WallSide.Right : WallSide.Left);
                }
                break;
            }
            // 3. Si está en medio de un salto (Jumping): Cancelar y devolverse a la pared de la que salió (si está habilitado)
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
                // La velocidad vertical se conserva: el arco continúa su trayectoria natural
                currentState = PlayerState.Jumping;
                rb.linearVelocity = new Vector2(bounceHorizontalDir * jumpForceX, rb.linearVelocity.y);
            }
            return;
        }

        // Si se agota la energía pasivamente durante el juego, el jugador cae
        if (currentState != PlayerState.Idle && currentState != PlayerState.Falling && !energyManager.HasEnergy)
        {
            currentState = PlayerState.Falling;
            float fallGravity = GameManager.Instance != null ? GameManager.Instance.FallGravity : 5f;
            rb.linearVelocity = new Vector2(0f, -fallGravity);
        }

        float beltSpeed = (LevelManager.Instance != null) ? LevelManager.Instance.BeltAgainstSpeed : beltAgainstSpeed;

        // Avance en contra de la cinta mientras el lado actual esté presionado
        if (currentState == PlayerState.AgainstBelt)
        {
            rb.linearVelocity = new Vector2(0f, -GetSlideDirection() * beltSpeed);
        }

        // Aplicar gravedad del arco mientras está en el aire saltando
        if (currentState == PlayerState.Jumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - (jumpArcGravity * Time.fixedDeltaTime));
        }

        if (currentState == PlayerState.Falling)
        {
            float fallGravity = GameManager.Instance != null ? GameManager.Instance.FallGravity : 5f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - fallGravity * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Inicia el avance en contra de la cinta: mientras el dedo se mantenga presionado
    /// sobre el lado de la pared actual, el jugador avanza en sentido contrario a la cinta.
    /// </summary>
    private void StartBeltAgainst(int touchId)
    {
        energyManager.StartGame();
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyFirstInput();

        beltTouchId = touchId;
        currentState = PlayerState.AgainstBelt;

        // Animación: pegado a la pared avanzando contra la cinta
        if (animator != null)
        {
            animator.SetBool("Anim_Clinging", true);
            animator.SetBool("Anim_Jumping", false);
        }
    }

    /// <summary>
    /// Detiene el avance contra la cinta y vuelve al deslizamiento normal en la pared.
    /// </summary>
    private void StopBeltAgainst()
    {
        beltTouchId = -1;
        if (currentState == PlayerState.AgainstBelt)
        {
            currentState = PlayerState.Clinging;
            ApplyWallSlide();

            // Animación: deslizamiento normal en la pared (Clinging)
            if (animator != null)
            {
                animator.SetBool("Anim_Clinging", true);
                animator.SetBool("Anim_Jumping", false);
            }
        }
    }

    /// <summary>
    /// Realiza el salto hacia la pared indicada, consumiendo energía.
    /// El impulso vertical inicial se calcula para que el arco aterrice
    /// a la misma altura de origen, en la pared contraria.
    /// </summary>
    private void Jump(WallSide targetWall)
    {
        // Notificar que el juego comenzó para iniciar el drenado pasivo
        energyManager.StartGame();
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyFirstInput();

        // Verificar energía a través del EnergyManager
        if (!energyManager.TryConsumeJumpEnergy())
        {
            // Sin energía: el jugador cae
            currentState = PlayerState.Falling;
            float fallGravity = GameManager.Instance != null ? GameManager.Instance.FallGravity : 5f;
            rb.linearVelocity = new Vector2(0f, -fallGravity);

            // Animación: caída libre (en el aire, sin control)
            if (animator != null)
            {
                animator.SetBool("Anim_Clinging", false);
                animator.SetBool("Anim_Jumping", true);
            }
            return;
        }

        currentState = PlayerState.Jumping;

        float horizontalDir = (targetWall == WallSide.Left) ? -1f : 1f;
        float vx = horizontalDir * jumpForceX;

        // Arco balístico: vy inicial tal que, con la gravedad del arco,
        // el jugador vuelva a la altura actual justo al llegar a la otra pared
        float vy = ComputeJumpInitialVy(targetWall, vx);

        rb.linearVelocity = new Vector2(vx, vy);

        SetFacing(horizontalDir);

        // Animación: saltando hacia la pared opuesta
        if (animator != null)
        {
            animator.SetBool("Anim_Clinging", false);
            animator.SetBool("Anim_Jumping", true);
        }
    }

    /// <summary>
    /// Calcula la velocidad vertical inicial del salto para un arco que
    /// termina a la misma altura de inicio sobre la pared destino:
    /// vy = gravedad * tiempoDeVuelo / 2.
    /// </summary>
    private float ComputeJumpInitialVy(WallSide targetWall, float vx)
    {
        float targetX = GetWallTargetX(targetWall);
        float distanceX = Mathf.Abs(targetX - transform.position.x);

        float flightTime = distanceX / Mathf.Abs(vx);
        return 0.5f * jumpArcGravity * flightTime;
    }

    /// <summary>
    /// Devuelve la X donde debe aterrizar el jugador al llegar a la pared indicada
    /// (borde interno de la pared + medio ancho del jugador). Si no encuentra la
    /// pared, usa los límites de pantalla como fallback.
    /// </summary>
    private float GetWallTargetX(WallSide wall)
    {
        string wallTag = (wall == WallSide.Left) ? "WallLeft" : "WallRight";
        GameObject wallObject = GameObject.FindGameObjectWithTag(wallTag);

        Collider2D wallCollider = (wallObject != null) ? wallObject.GetComponent<Collider2D>() : null;
        Collider2D playerCollider = GetComponent<Collider2D>();

        if (wallCollider != null && playerCollider != null)
        {
            float sign = (wall == WallSide.Left) ? 1f : -1f;
            return wallCollider.bounds.center.x + sign * (wallCollider.bounds.extents.x + playerCollider.bounds.extents.x);
        }

        // Fallback: bordes de pantalla
        if (GameManager.Instance != null)
        {
            return (wall == WallSide.Left) ? GameManager.Instance.ScreenLeft : GameManager.Instance.ScreenRight;
        }

        return transform.position.x;
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

            // Animación: caída por cancelación sin energía
            if (animator != null)
            {
                animator.SetBool("Anim_Clinging", false);
                animator.SetBool("Anim_Jumping", true);
            }
            return;
        }

        // Dirección de regreso hacia la pared de origen
        float returnHorizontalDir = (currentWall == WallSide.Left) ? -1f : 1f;

        // Solo invertir la velocidad horizontal hacia la pared de origen:
        // la velocidad vertical y la gravedad del arco se conservan, así la
        // trayectoria continúa su caída natural de vuelta (sin cambios bruscos)
        rb.linearVelocity = new Vector2(returnHorizontalDir * jumpForceX, rb.linearVelocity.y);

        SetFacing(returnHorizontalDir);
        // El jugador sigue en el aire regresando: mantiene Anim_Jumping activo
    }

    /// <summary>
    /// Devuelve la dirección vertical de la cinta de la pared actual (+1 sube, -1 baja).
    /// </summary>
    private float GetSlideDirection()
    {
        return GetSlideDirection(currentWall);
    }

    /// <summary>
    /// Devuelve la dirección vertical de la cinta de una pared (+1 sube, -1 baja).
    /// </summary>
    private float GetSlideDirection(WallSide wall)
    {
        if (wall == WallSide.Left)
        {
            return leftWallGoesUp ? 1f : -1f;
        }
        return leftWallGoesUp ? -1f : 1f;
    }

    /// <summary>
    /// Voltea el modelo horizontalmente según la dirección de movimiento
    /// (+1 = mirando a la derecha como al inicio, -1 = volteado hacia la izquierda).
    /// </summary>
    private void SetFacing(float horizontalDir)
    {
        Vector3 scale = initialScale;
        scale.x *= (horizontalDir >= 0f) ? 1f : -1f;
        transform.localScale = scale;
    }

    /// <summary>
    /// Aplica la velocidad de deslizamiento según la pared actual y la configuración.
    /// </summary>
    private void ApplyWallSlide()
    {
        float slideSpeed = (LevelManager.Instance != null) ? LevelManager.Instance.WallSlideSpeed : wallSlideSpeed;
        rb.linearVelocity = new Vector2(0f, GetSlideDirection() * slideSpeed);
    }

    /// <summary>
    /// Detecta colisiones con las paredes usando Tags.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Antes de que la partida comience: quedarse quieto en la pared sin deslizarse
        if (GameManager.Instance != null && !GameManager.Instance.HasGameStarted)
        {
            currentState = PlayerState.Idle;
            rb.linearVelocity = Vector2.zero;

            // Animación: idle esperando el primer tap
            if (animator != null)
            {
                animator.SetBool("Anim_Clinging", false);
                animator.SetBool("Anim_Jumping", false);
            }
            return;
        }

        if (collision.gameObject.CompareTag("WallLeft"))
        {
            currentWall = WallSide.Left;
            currentState = PlayerState.Clinging;
            ApplyWallSlide();

            // Animación: aterrizó en la pared izquierda
            if (animator != null)
            {
                animator.SetBool("Anim_Clinging", true);
                animator.SetBool("Anim_Jumping", false);
            }
        }
        else if (collision.gameObject.CompareTag("WallRight"))
        {
            currentWall = WallSide.Right;
            currentState = PlayerState.Clinging;
            ApplyWallSlide();

            // Animación: aterrizó en la pared derecha
            if (animator != null)
            {
                animator.SetBool("Anim_Clinging", true);
                animator.SetBool("Anim_Jumping", false);
            }
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

        // Animación: rebote por choque con púas (sigue en el aire)
        if (animator != null)
        {
            animator.SetBool("Anim_Clinging", false);
            animator.SetBool("Anim_Jumping", true);
        }
    }

    private void RespawnAtWall()
    {
        currentState = PlayerState.Clinging;
        transform.position = new Vector3(0f, 0f, 0f);
        rb.linearVelocity = Vector2.zero;
        ApplyWallSlide();
    }

    public bool IsGodModeEnabled => isGodMode;
}
