using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelConfig
    {
        [Header("Velocidades de Cinta")]
        public float wallSlideSpeed = 3f;
        public float beltAgainstSpeed = 2f;

        [Header("Spawn de Items (Positivos/Negativos)")]
        public float itemMinSpawnInterval = 1.5f;
        public float itemMaxSpawnInterval = 3.0f;
        [Range(0f, 1f)]
        public float positiveProbability = 0.6f;

        [Header("Spawn de Aguardiente")]
        public float aguardienteMinInterval = 8f;
        public float aguardienteMaxInterval = 15f;

        [Header("Spawn de Púas")]
        public float puaMinInterval = 4f;
        public float puaMaxInterval = 8f;
    }

    [Header("Configuración de Niveles")]
    [SerializeField] private LevelConfig level1Config = new LevelConfig
    {
        wallSlideSpeed = 3f,
        beltAgainstSpeed = 2f,
        itemMinSpawnInterval = 1.5f,
        itemMaxSpawnInterval = 3.0f,
        positiveProbability = 0.6f,
        aguardienteMinInterval = 8f,
        aguardienteMaxInterval = 15f,
        puaMinInterval = 4f,
        puaMaxInterval = 8f
    };

    [SerializeField] private LevelConfig level2Config = new LevelConfig
    {
        wallSlideSpeed = 4f,
        beltAgainstSpeed = 2.5f,
        itemMinSpawnInterval = 1.2f,
        itemMaxSpawnInterval = 2.5f,
        positiveProbability = 0.5f,
        aguardienteMinInterval = 6f,
        aguardienteMaxInterval = 12f,
        puaMinInterval = 3f,
        puaMaxInterval = 6f
    };

    [SerializeField] private LevelConfig level3Config = new LevelConfig
    {
        wallSlideSpeed = 5f,
        beltAgainstSpeed = 3f,
        itemMinSpawnInterval = 1.0f,
        itemMaxSpawnInterval = 2.0f,
        positiveProbability = 0.4f,
        aguardienteMinInterval = 5f,
        aguardienteMaxInterval = 10f,
        puaMinInterval = 2f,
        puaMaxInterval = 5f
    };

    [Header("Temporizador de Nivel")]
    [Tooltip("Tiempo en segundos para avanzar al siguiente nivel automáticamente")]
    [SerializeField] private float timeToNextLevel = 30f;
    [Tooltip("Si es true, avanza automáticamente de nivel cada 'timeToNextLevel' segundos")]
    [SerializeField] private bool autoAdvanceLevels = false;

    [Header("UI (TextMeshPro)")]
    [Tooltip("Texto de la UI donde se muestra el nivel actual (opcional)")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Tooltip("Texto o prefijo que acompaña al número de nivel (ej: 'Nivel ' o 'Level ')")]
    [SerializeField] private string levelPrefix = "Nivel ";

    [Header("Eventos")]
    public UnityEvent<int> OnLevelChanged;

    public static LevelManager Instance { get; private set; }

    private int currentLevel = 1;
    private float levelTimer = 0f;
    private bool timerActive = false;

    public int CurrentLevel => currentLevel;
    public float TimeToNextLevel => timeToNextLevel;
    public bool AutoAdvanceLevels => autoAdvanceLevels;

    public float LevelTimerProgress
    {
        get
        {
            if (currentLevel >= 3) return 1f;
            return Mathf.Clamp01(levelTimer / timeToNextLevel);
        }
    }

    public LevelConfig CurrentConfig
    {
        get
        {
            return currentLevel switch
            {
                2 => level2Config,
                3 => level3Config,
                _ => level1Config
            };
        }
    }

    public float WallSlideSpeed => CurrentConfig.wallSlideSpeed * slowdownMultiplier;
    public float BeltAgainstSpeed => CurrentConfig.beltAgainstSpeed * slowdownMultiplier;

    public float ItemMinSpawnInterval => CurrentConfig.itemMinSpawnInterval;
    public float ItemMaxSpawnInterval => CurrentConfig.itemMaxSpawnInterval;
    public float PositiveProbability => CurrentConfig.positiveProbability;

    public float AguardienteMinInterval => CurrentConfig.aguardienteMinInterval;
    public float AguardienteMaxInterval => CurrentConfig.aguardienteMaxInterval;

    public float PuaMinInterval => CurrentConfig.puaMinInterval;
    public float PuaMaxInterval => CurrentConfig.puaMaxInterval;

    private float slowdownMultiplier = 1f;
    private float slowdownTimer = 0f;

    public bool IsSlowdownActive => slowdownTimer > 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateLevelText();
    }

    void Update()
    {
        if (timerActive && autoAdvanceLevels && currentLevel < 3)
        {
            levelTimer += Time.deltaTime;
            if (levelTimer >= timeToNextLevel)
            {
                AdvanceLevel();
                levelTimer = 0f;
            }
        }

        if (slowdownTimer > 0f)
        {
            slowdownTimer -= Time.deltaTime;
            if (slowdownTimer <= 0f)
            {
                slowdownMultiplier = 1f;
            }
        }
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, 3);
        levelTimer = 0f;
        UpdateLevelText();
        OnLevelChanged?.Invoke(currentLevel);
        Debug.Log($"LevelManager: Nivel establecido a {currentLevel}");
    }

    public void AdvanceLevel()
    {
        if (currentLevel < 3)
        {
            currentLevel++;
            levelTimer = 0f;
            UpdateLevelText();
            OnLevelChanged?.Invoke(currentLevel);
            Debug.Log($"LevelManager: Avanzando al nivel {currentLevel}");
        }
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = $"{levelPrefix}{currentLevel}";
        }
    }

    public void StartTimer()
    {
        timerActive = true;
    }

    public void StopTimer()
    {
        timerActive = false;
    }

    public void ResetTimer()
    {
        levelTimer = 0f;
    }

    public void ApplyConveyorSlowdown(float duration, float multiplier)
    {
        slowdownTimer = duration;
        slowdownMultiplier = multiplier;
    }
}
