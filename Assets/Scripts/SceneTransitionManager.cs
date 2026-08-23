using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Administrador centralizado de transiciones entre escenas en Unity.
/// Ofrece funciones para cargar escenas por nombre o índice, reiniciar la escena actual,
/// salir del juego y realizar transiciones con efecto de desvanecimiento (Fade In / Fade Out) si se asigna un CanvasGroup.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Configuración de Persistencia")]
    [Tooltip("Indica si el gestor debe permanecer entre escenas (DontDestroyOnLoad)")]
    [SerializeField] private bool isPersistent = true;

    [Header("Transición Visual (Opcional)")]
    [Tooltip("CanvasGroup usado para hacer Fade In / Fade Out durante la carga de escena")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("Duración del efecto de desvanecimiento en segundos")]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (isPersistent)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // Asegurar que al iniciar la escena se realice un Fade In si existe el CanvasGroup
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            StartCoroutine(Fade(0f));
        }
    }

    /// <summary>
    /// Carga una escena por su nombre.
    /// </summary>
    /// <param name="sceneName">Nombre exacto de la escena en Build Settings</param>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToScene(sceneName));
    }

    /// <summary>
    /// Carga una escena por su índice en Build Settings.
    /// </summary>
    /// <param name="sceneIndex">Índice de la escena</param>
    public void LoadScene(int sceneIndex)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToScene(sceneIndex));
    }

    /// <summary>
    /// Reinicia la escena activa actualmente.
    /// </summary>
    public void ReloadCurrentScene()
    {
        if (isTransitioning) return;
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(TransitionToScene(activeSceneIndex));
    }

    /// <summary>
    /// Cierra la aplicación (funciona tanto en Builds como en el Editor de Unity).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("SceneTransitionManager: Cerrando el juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Corrutina para manejar la transición a una escena por nombre (con Fade si está configurado).
    /// </summary>
    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f));
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(0f));
        }

        isTransitioning = false;
    }

    /// <summary>
    /// Corrutina para manejar la transición a una escena por índice (con Fade si está configurado).
    /// </summary>
    private IEnumerator TransitionToScene(int sceneIndex)
    {
        isTransitioning = true;

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f));
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(0f));
        }

        isTransitioning = false;
    }

    /// <summary>
    /// Realiza una animación suave de transparencia en el CanvasGroup.
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsedTime = 0f;

        fadeCanvasGroup.blocksRaycasts = true;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.5f;
    }
}
