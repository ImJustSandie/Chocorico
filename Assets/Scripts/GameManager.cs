using UnityEngine;
using UnityEngine.SceneManagement;

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
    /// Maneja la muerte del jugador y reinicia la partida.
    /// </summary>
    public void PlayerDied()
    {
        Debug.Log("¡El jugador murió! Reiniciando partida...");

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
