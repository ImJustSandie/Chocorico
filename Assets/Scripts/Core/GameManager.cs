using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Administra la lógica general del juego: detección de muerte, reinicio de escena,
/// y límites de pantalla.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Límites de pantalla")]
    [Tooltip("Margen extra (en unidades) antes de considerar que el jugador salió de la pantalla")]
    [SerializeField] private float offScreenMargin = 1.5f;

    [Header("Caída")]
    [Tooltip("Gravedad aplicada al jugador cuando cae sin energía")]
    [SerializeField] private float fallGravity = 5f;

    [Header("UI & Canvases")]
    [Tooltip("Canvas/Panel del Menú Inicial o Tutorial.")]
    [SerializeField] private GameObject menuCanvas;

    [Tooltip("Canvas/Panel de la UI de Juego (HUD con puntuación, energía, etc.).")]
    [SerializeField] private GameObject inGameCanvas;

    [Tooltip("Panel de derrota que se muestra al perder. El botón de 'Nueva Partida' debe llamar a GameManager.RestartGame.")]
    [SerializeField] private GameObject defeatPanel;

    [Tooltip("Bordes del menú que se animan (DOTween) al iniciar la partida.")]
    [SerializeField] private MenuBorders menuBorders;

    [Tooltip("Indica si se debe ocultar la UI del juego (inGameCanvas) cuando aparezca el panel de derrota.")]
    [SerializeField] private bool hideInGameCanvasOnDefeat = true;

    // Límites de pantalla (calculados en Start)
    private float screenTop;
    private float screenBottom;
    private float screenLeft;
    private float screenRight;

    /// <summary>
    /// Gravedad de caída (solo lectura, para que PlayerManager la use).
    /// </summary>
    public float FallGravity => fallGravity;

    public float ScreenLeft => screenLeft;
    public float ScreenRight => screenRight;
    public float ScreenTop => screenTop;
    public float ScreenBottom => screenBottom;

    /// <summary>
    /// Instancia singleton para acceso global.
    /// </summary>
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Indica si la partida ya comenzó (primer input de gameplay del jugador).
    /// Los spawners dependen de este estado.
    /// </summary>
    public bool HasGameStarted { get; private set; } = false;

    /// <summary>
    /// Indica si se presionó el botón de jugar: la UI del menú está oculta
    /// y los inputs táctiles de gameplay están habilitados.
    /// </summary>
    public bool IsPlaying { get; private set; } = false;

    /// <summary>
    /// Indica si el jugador ya perdió (evita procesar la muerte varias veces).
    /// </summary>
    public bool IsGameOver { get; private set; } = false;

    /// <summary>
    /// Los spawners solo deben generar objetos mientras la partida esté activa.
    /// </summary>
    public bool CanSpawn => HasGameStarted && !IsGameOver;

    void Awake()
    {
        // Singleton simple
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        CalculateScreenBounds();
        InitializeUI();
    }

    /// <summary>
    /// Configura la visibilidad inicial de los Canvases antes de empezar a jugar.
    /// </summary>
    private void InitializeUI()
    {
        if (menuCanvas != null) menuCanvas.SetActive(true);
        if (inGameCanvas != null) inGameCanvas.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    /// <summary>
    /// Calcula los límites de la pantalla en coordenadas del mundo.
    /// </summary>
    private void CalculateScreenBounds()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
            Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
            screenLeft = bottomLeft.x;
            screenRight = topRight.x;
            screenBottom = bottomLeft.y;
            screenTop = topRight.y;
        }
        else
        {
            Debug.LogWarning("GameManager: No se encontró cámara principal. Usando valores por defecto.");
            screenLeft = -9f;
            screenRight = 9f;
            screenBottom = -5f;
            screenTop = 5f;
        }
    }

    /// <summary>
    /// Verifica si una posición está fuera de los límites de la pantalla (con margen).
    /// </summary>
    public bool IsOffScreen(Vector3 position)
    {
        return position.y > screenTop + offScreenMargin
            || position.y < screenBottom - offScreenMargin
            || position.x > screenRight + offScreenMargin
            || position.x < screenLeft - offScreenMargin;
    }

    /// <summary>
    /// Inicia la partida (conectar al botón de Jugar del menú principal).
    /// Oculta el menú, activa la UI de juego y habilita los inputs de gameplay.
    /// </summary>
    public void StartGame()
    {
        if (IsPlaying) return;

        IsPlaying = true;

        if (inGameCanvas != null) inGameCanvas.SetActive(true);

        if (menuBorders != null)
        {
            menuBorders.Hide();

            // Ocultar el menú al terminar la animación de los bordes,
            // para no desaparecerlos (y a sus bordes hijos) antes de animarse
            if (menuCanvas != null)
            {
                DOVirtual.DelayedCall(menuBorders.Duration, () => menuCanvas.SetActive(false));
            }
        }
        else if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// Marca el primer input real de gameplay (primer salto/avance del jugador).
    /// Habilita los spawners e inicia temporizador y música.
    /// </summary>
    public void NotifyFirstInput()
    {
        if (HasGameStarted) return;

        HasGameStarted = true;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.StartTimer();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic();
        }
    }

    /// <summary>
    /// Maneja la muerte del jugador: muestra el panel de derrota.
    /// El reinicio se realiza desde el botón del panel (RestartGame).
    /// </summary>
    public void PlayerDied()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Debug.Log("¡El jugador murió!");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // Guardar la puntuación acumulada si es un nuevo récord
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveHighScore();
        }

        if (hideInGameCanvasOnDefeat && inGameCanvas != null)
        {
            inGameCanvas.SetActive(false);
        }

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
        }
        else
        {
            // Fallback: si no hay panel asignado, reinicia directamente
            RestartGame();
        }
    }

    /// <summary>
    /// Inicia una nueva partida: reinicia el estado al inicio recargando la escena actual.
    /// Conectar este método al botón del panel de derrota.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("Iniciando nueva partida...");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ReloadCurrentScene();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
