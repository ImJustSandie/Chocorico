using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Administra la puntuación del jugador: acumula puntos al recoger objetos
/// y actualiza el texto de la UI.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>
    /// Instancia singleton para acceso global.
    /// </summary>
    public static ScoreManager Instance { get; private set; }

    /// <summary>
    /// Clave de PlayerPrefs usada para guardar la mejor puntuación.
    /// </summary>
    public const string HIGH_SCORE_PREFS_KEY = "HighScore";

    [Header("UI (TextMeshPro)")]
    [Tooltip("Texto de la UI donde se muestra la puntuación actual durante el juego")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Texto de la UI donde se muestra la puntuación total obtenida al finalizar la partida")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Tooltip("Texto de la UI donde se muestra la mejor puntuación histórica (opcional)")]
    [SerializeField] private TextMeshProUGUI highScoreText;

    private int currentScore = 0;
    private int highScore = 0;

    /// <summary>
    /// Puntuación actual del jugador (solo lectura).
    /// </summary>
    public int CurrentScore => currentScore;

    /// <summary>
    /// Mejor puntuación obtenida por el jugador (solo lectura).
    /// </summary>
    public int HighScore => highScore;

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
        LoadHighScore();
        UpdateScoreText();
        UpdateHighScoreText();
    }

    /// <summary>
    /// Carga la puntuación más alta guardada en PlayerPrefs.
    /// </summary>
    public void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_PREFS_KEY, 0);
    }

    /// <summary>
    /// Compara la puntuación actual con la puntuación más alta.
    /// Si la supera, la guarda en PlayerPrefs y actualiza la UI.
    /// </summary>
    /// <returns>True si se logró un nuevo récord.</returns>
    public bool SaveHighScore()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_PREFS_KEY, highScore);
            PlayerPrefs.Save();
            UpdateHighScoreText();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Añade puntos a la puntuación y actualiza la UI.
    /// </summary>
    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = currentScore.ToString();
        }
    }

    private void UpdateHighScoreText()
    {
        if (highScoreText != null)
        {
            highScoreText.text = highScore.ToString();
        }
    }
}
