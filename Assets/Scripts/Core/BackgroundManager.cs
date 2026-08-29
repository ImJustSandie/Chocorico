using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Administra el fondo del juego: genera objetos decorativos que caen
/// (sprite y color según el nivel actual) y cambia el color de fondo de la
/// cámara con transición suave al cambiar de nivel.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelBackgroundConfig
    {
        [Header("Objetos decorativos")]
        [Tooltip("Prefab decorativo de este nivel. Si está vacío usa el 'decorPrefab' general")]
        public GameObject prefab;

        [Tooltip("Sprites alternativos de este nivel (sobrescriben el sprite del prefab; opcional)")]
        public Sprite[] sprites;

        [Tooltip("Color con el que se tiñen los sprites de este nivel")]
        public Color spriteTintColor = Color.white;

        [Header("Fondo")]
        [Tooltip("Color de fondo de la cámara en este nivel")]
        public Color backgroundColor = Color.white;
    }

    [Header("Prefab")]
    [Tooltip("Prefab decorativo por defecto (SpriteRenderer + script BackgroundDecor). Se usa si el nivel no define uno propio")]
    [SerializeField] private GameObject decorPrefab;

    [Header("Configuración por Nivel")]
    [Tooltip("Sprites, tinte y color de fondo para el nivel 1")]
    [SerializeField] private LevelBackgroundConfig level1 = new LevelBackgroundConfig();

    [Tooltip("Sprites, tinte y color de fondo para el nivel 2")]
    [SerializeField] private LevelBackgroundConfig level2 = new LevelBackgroundConfig();

    [Tooltip("Sprites, tinte y color de fondo para el nivel 3")]
    [SerializeField] private LevelBackgroundConfig level3 = new LevelBackgroundConfig();

    [Tooltip("Sprites, tinte y color de fondo para el nivel 4")]
    [SerializeField] private LevelBackgroundConfig level4 = new LevelBackgroundConfig();

    [Header("Nivel 4 - Colores Dinámicos")]
    [Tooltip("Lista de colores para los objetos decorativos del nivel 4 (se elige aleatoriamente)")]
    [SerializeField] private Color[] level4DecorColors = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.cyan,
        Color.magenta
    };

    [Tooltip("Lista de colores para el fondo del nivel 4 (transiciones periódicas)")]
    [SerializeField] private Color[] level4BackgroundColors = new Color[]
    {
        new Color(0.8f, 0.2f, 0.2f),
        new Color(0.2f, 0.8f, 0.2f),
        new Color(0.2f, 0.2f, 0.8f),
        new Color(0.8f, 0.8f, 0.2f),
        new Color(0.2f, 0.8f, 0.8f),
        new Color(0.8f, 0.2f, 0.8f)
    };

    [Tooltip("Intervalo de cambio de color de fondo en el nivel 4 (segundos)")]
    [SerializeField] private float level4ColorChangeInterval = 5f;

    [Header("Generación")]
    [Tooltip("Tiempo mínimo entre generaciones (segundos)")]
    [SerializeField] private float minSpawnInterval = 1.5f;

    [Tooltip("Tiempo máximo entre generaciones (segundos)")]
    [SerializeField] private float maxSpawnInterval = 4f;

    [Tooltip("Separación mínima horizontal entre spawns recientes, para que no salgan tan pegados")]
    [SerializeField] private float minHorizontalSpacing = 2f;

    [Tooltip("Cuántos últimos spawns se tienen en cuenta para la separación mínima")]
    [SerializeField] private int spacingHistorySize = 3;

    [Tooltip("Intentos máximos para encontrar una X que respete la separación mínima")]
    [SerializeField] private int maxPlacementAttempts = 8;

    [Header("Movimiento")]
    [Tooltip("Velocidad de caída de los objetos decorativos")]
    [SerializeField] private float fallSpeed = 2f;

    [Tooltip("Velocidad de desplazamiento lateral (la dirección es aleatoria por objeto)")]
    [SerializeField] private float horizontalDriftSpeed = 0.5f;

    [Tooltip("Velocidad de rotación en grados por segundo")]
    [SerializeField] private float rotationSpeed = 15f;

    [Tooltip("Si es true, la dirección de rotación (horaria/antihoraria) es aleatoria por objeto")]
    [SerializeField] private bool randomRotationDirection = true;

    [Tooltip("Orden en la capa de renderizado (valores bajos quedan detrás del gameplay)")]
    [SerializeField] private int sortingOrder = -10;

    [Header("Área de Generación")]
    [Tooltip("Margen horizontal respecto a los bordes de la pantalla")]
    [SerializeField] private float horizontalPadding = 0.5f;

    [Tooltip("Distancia por encima de la parte superior de la pantalla donde aparecen")]
    [SerializeField] private float spawnOffsetY = 1f;

    [Header("Cámara / Fondo")]
    [Tooltip("Cámara cuyo Background Color cambia. Si está vacío usa Camera.main")]
    [SerializeField] private Camera targetCamera;

    [Header("Transición de Fondo (DOTween)")]
    [Tooltip("Duración de la transición entre colores de fondo (segundos)")]
    [SerializeField] private float transitionDuration = 1f;

    [Tooltip("Curva de easing de la transición de fondo")]
    [SerializeField] private Ease ease = Ease.InOutQuad;

    private float timer = 0f;
    private float nextSpawnTime = 1f;
    private Tween backgroundColorTween;
    private float level4ColorTimer = 0f;
    private int lastLevel4BackgroundColorIndex = -1;

    // Objetos decorativos activos: el manager los mueve y los destruye al salir de pantalla
    private readonly List<ActiveDecor> activeDecor = new List<ActiveDecor>();

    // Últimas X usadas en spawns recientes (para respetar la separación mínima)
    private readonly List<float> recentSpawnXs = new List<float>();

    private class ActiveDecor
    {
        public BackgroundDecor decor;
        public float driftDir;      // -1 o 1: dirección lateral del objeto
        public float rotationDir;   // -1 o 1: sentido de rotación
    }

    void Start()
    {
        SetNextSpawnTime();
        ApplyBackgroundColor(GetCurrentConfig(), instant: true);

        // Suscribirse en Start (no OnEnable) para garantizar que LevelManager
        // ya haya asignado su Instance en Awake
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelChanged.AddListener(OnLevelChanged);
        }
    }

    void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelChanged.RemoveListener(OnLevelChanged);
        }
    }

    void Update()
    {
        // El movimiento de los decorativos corre SIEMPRE (aunque el juego termine),
        // para que el fondo no se congele en pantalla
        MoveDecorObjects();

        if (GameManager.Instance == null || !GameManager.Instance.CanSpawn)
            return;

        timer += Time.deltaTime;
        if (timer >= nextSpawnTime)
        {
            SpawnDecor();
            timer = 0f;
            SetNextSpawnTime();
        }

        // Nivel 4: cambio de color de fondo periódico
        if (GetCurrentLevel() == 4)
        {
            level4ColorTimer += Time.deltaTime;
            if (level4ColorTimer >= level4ColorChangeInterval)
            {
                ApplyLevel4BackgroundColor();
                level4ColorTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Mueve todos los objetos decorativos activos a la MISMA velocidad
    /// y destruye los que salieron por la parte inferior de la pantalla.
    /// </summary>
    private void MoveDecorObjects()
    {
        float screenBottom = (GameManager.Instance != null) ? GameManager.Instance.ScreenBottom : -6f;

        for (int i = activeDecor.Count - 1; i >= 0; i--)
        {
            ActiveDecor entry = activeDecor[i];

            if (entry.decor == null)
            {
                activeDecor.RemoveAt(i);
                continue;
            }

            Vector3 movement = new Vector3(
                entry.driftDir * horizontalDriftSpeed * Time.deltaTime,
                -fallSpeed * Time.deltaTime,
                0f);

            entry.decor.transform.position += movement;
            entry.decor.transform.Rotate(0f, 0f, entry.rotationDir * rotationSpeed * Time.deltaTime);

            if (entry.decor.IsBelowScreen(screenBottom))
            {
                activeDecor.RemoveAt(i);
                Destroy(entry.decor.gameObject);
            }
        }
    }

    private void OnLevelChanged(int level)
    {
        ApplyBackgroundColor(GetConfigForLevel(level), instant: false);
        
        // Resetear timer de color del nivel 4
        if (level == 4)
        {
            level4ColorTimer = 0f;
            lastLevel4BackgroundColorIndex = -1;
        }
    }

    /// <summary>
    /// Instancia un objeto decorativo con el sprite/tinte del nivel actual.
    /// </summary>
    private void SpawnDecor()
    {
        LevelBackgroundConfig config = GetCurrentConfig();

        GameObject prefab = (config.prefab != null) ? config.prefab : decorPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("BackgroundManager: Este nivel no tiene prefab propio y no hay 'decorPrefab' general asignado.");
            return;
        }

        bool hasSprites = config.sprites != null && config.sprites.Length > 0;
        if (!hasSprites && prefab.GetComponent<BackgroundDecor>() == null)
        {
            Debug.LogWarning("BackgroundManager: El prefab asignado no tiene el componente BackgroundDecor.");
            return;
        }

        float minX = -3f;
        float maxX = 3f;
        float spawnY = 6f;

        if (GameManager.Instance != null)
        {
            minX = GameManager.Instance.ScreenLeft + horizontalPadding;
            maxX = GameManager.Instance.ScreenRight - horizontalPadding;
            spawnY = GameManager.Instance.ScreenTop + spawnOffsetY;
        }

        Vector3 spawnPosition = new Vector3(FindSpawnX(minX, maxX), spawnY, transform.position.z);

        GameObject spawnedObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

        BackgroundDecor decor = spawnedObj.GetComponent<BackgroundDecor>();
        if (decor != null)
        {
            Sprite sprite = hasSprites ? config.sprites[Random.Range(0, config.sprites.Length)] : null;
            
            // Nivel 4: usar color aleatorio de la lista
            Color tintColor = config.spriteTintColor;
            if (GetCurrentLevel() == 4 && level4DecorColors != null && level4DecorColors.Length > 0)
            {
                tintColor = level4DecorColors[Random.Range(0, level4DecorColors.Length)];
            }
            
            decor.Setup(sprite, tintColor, sortingOrder);

            activeDecor.Add(new ActiveDecor
            {
                decor = decor,
                driftDir = (Random.value < 0.5f) ? -1f : 1f,
                rotationDir = (!randomRotationDirection || Random.value < 0.5f) ? 1f : -1f
            });
        }
        else
        {
            Debug.LogWarning("BackgroundManager: El prefab instanciado no tiene el componente BackgroundDecor; no se controlará su caída.");
        }
    }

    /// <summary>
    /// Busca una X dentro de [minX, maxX] que respete la separación mínima
    /// respecto a los spawns recientes, para que los objetos no salgan pegados.
    /// </summary>
    private float FindSpawnX(float minX, float maxX)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            float candidateX = Random.Range(minX, maxX);

            bool tooClose = false;
            foreach (float recentX in recentSpawnXs)
            {
                if (Mathf.Abs(candidateX - recentX) < minHorizontalSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose || attempt == maxPlacementAttempts - 1)
            {
                RegisterSpawnX(candidateX);
                return candidateX;
            }
        }

        // Inalcanzable: el último intento siempre retorna, pero por seguridad
        float fallbackX = Random.Range(minX, maxX);
        RegisterSpawnX(fallbackX);
        return fallbackX;
    }

    private void RegisterSpawnX(float x)
    {
        recentSpawnXs.Add(x);
        while (recentSpawnXs.Count > Mathf.Max(1, spacingHistorySize))
        {
            recentSpawnXs.RemoveAt(0);
        }
    }

    /// <summary>
    /// Cambia el color de fondo de la cámara (transición suave con DOTween).
    /// </summary>
    private void ApplyBackgroundColor(LevelBackgroundConfig config, bool instant)
    {
        Camera cam = (targetCamera != null) ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("BackgroundManager: No se encontró cámara para cambiar el fondo.");
            return;
        }

        if (backgroundColorTween != null && backgroundColorTween.IsActive())
        {
            backgroundColorTween.Kill();
        }

        if (instant)
        {
            cam.backgroundColor = config.backgroundColor;
        }
        else
        {
            backgroundColorTween = cam.DOColor(config.backgroundColor, transitionDuration).SetEase(ease);
        }
    }

    private LevelBackgroundConfig GetCurrentConfig()
    {
        return GetConfigForLevel(GetCurrentLevel());
    }

    private LevelBackgroundConfig GetConfigForLevel(int level)
    {
        return level switch
        {
            2 => level2,
            3 => level3,
            4 => level4,
            _ => level1
        };
    }

    private int GetCurrentLevel()
    {
        return (LevelManager.Instance != null) ? LevelManager.Instance.CurrentLevel : 1;
    }

    private void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    /// <summary>
    /// Aplica un color de fondo aleatorio del nivel 4, evitando repetir el mismo color consecutivamente.
    /// </summary>
    private void ApplyLevel4BackgroundColor()
    {
        if (level4BackgroundColors == null || level4BackgroundColors.Length == 0)
            return;

        Camera cam = (targetCamera != null) ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("BackgroundManager: No se encontró cámara para cambiar el fondo.");
            return;
        }

        // Elegir un color diferente al actual
        int colorIndex = Random.Range(0, level4BackgroundColors.Length);
        if (level4BackgroundColors.Length > 1)
        {
            while (colorIndex == lastLevel4BackgroundColorIndex)
            {
                colorIndex = Random.Range(0, level4BackgroundColors.Length);
            }
        }
        lastLevel4BackgroundColorIndex = colorIndex;

        Color targetColor = level4BackgroundColors[colorIndex];

        if (backgroundColorTween != null && backgroundColorTween.IsActive())
        {
            backgroundColorTween.Kill();
        }

        backgroundColorTween = cam.DOColor(targetColor, transitionDuration).SetEase(ease);
    }
}
