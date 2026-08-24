using UnityEngine;
using UnityEngine.UI;

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

    [Header("UI")]
    [Tooltip("Texto de la UI donde se muestra la puntuación")]
    [SerializeField] private Text scoreText;

    private int currentScore = 0;

    /// <summary>
    /// Puntuación actual del jugador (solo lectura).
    /// </summary>
    public int CurrentScore => currentScore;

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
        UpdateScoreText();
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
    }
}
