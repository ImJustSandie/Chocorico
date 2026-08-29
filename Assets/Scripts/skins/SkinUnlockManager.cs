using UnityEngine;

/// <summary>
/// Administra el desbloqueo de skins mediante PlayerPrefs.
/// Cada skin tiene una condición específica para desbloquearse.
/// Se auto-crea como singleton si no existe en la escena.
/// </summary>
public class SkinUnlockManager : MonoBehaviour
{
    public const string PREFS_PATITO = "SkinUnlock_Patito";
    public const string PREFS_AGUARDIENTE_TIER1 = "SkinUnlock_AguardienteTier1";
    public const string PREFS_AGUARDIENTE_TIER2 = "SkinUnlock_AguardienteTier2";
    public const string PREFS_AGUARDIENTE_TIER3 = "SkinUnlock_AguardienteTier3";
    public const string PREFS_GOLDEN = "SkinUnlock_Golden";

    private const int AGUARDIENTE_REQUIRED_COUNT = 5;
    private const int GOLDEN_SCORE_THRESHOLD = 1000;

    private static SkinUnlockManager instance;

    public static SkinUnlockManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Buscar en la escena
                instance = FindObjectOfType<SkinUnlockManager>();
                
                // Si no existe, crear uno nuevo
                if (instance == null)
                {
                    GameObject go = new GameObject("SkinUnlockManager");
                    instance = go.AddComponent<SkinUnlockManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Registra el consumo de un patito (ítem negativo).
    /// Desbloquea la skin Patito la primera vez.
    /// </summary>
    public void RegisterPatitoConsumed()
    {
        if (!IsPatitoUnlocked())
        {
            PlayerPrefs.SetInt(PREFS_PATITO, 1);
            PlayerPrefs.Save();
            Debug.Log("Skin desbloqueada: Patito");
        }
    }

    /// <summary>
    /// Registra el consumo de un aguardiente del tier especificado.
    /// Desbloquea la skin correspondiente al llegar a 5 consumos.
    /// </summary>
    public void RegisterAguardienteConsumed(AguardienteItem.Tier tier)
    {
        string prefsKey = GetAguardientePrefsKey(tier);
        int currentCount = PlayerPrefs.GetInt(prefsKey, 0);

        if (currentCount < AGUARDIENTE_REQUIRED_COUNT)
        {
            currentCount++;
            PlayerPrefs.SetInt(prefsKey, currentCount);
            PlayerPrefs.Save();

            if (currentCount >= AGUARDIENTE_REQUIRED_COUNT)
            {
                Debug.Log($"Skin desbloqueada: Aguardiente {tier}");
            }
        }
    }

    /// <summary>
    /// Registra el puntaje actual y desbloquea la skin Golden si se alcanza el umbral.
    /// </summary>
    public void CheckGoldenUnlock(int currentScore)
    {
        if (!IsGoldenUnlocked() && currentScore >= GOLDEN_SCORE_THRESHOLD)
        {
            PlayerPrefs.SetInt(PREFS_GOLDEN, 1);
            PlayerPrefs.Save();
            Debug.Log("Skin desbloqueada: GOLDEN");
        }
    }

    /// <summary>
    /// Verifica si la skin Patito está desbloqueada.
    /// </summary>
    public bool IsPatitoUnlocked()
    {
        return PlayerPrefs.GetInt(PREFS_PATITO, 0) == 1;
    }

    /// <summary>
    /// Verifica si la skin de aguardiente del tier especificado está desbloqueada.
    /// </summary>
    public bool IsAguardienteUnlocked(AguardienteItem.Tier tier)
    {
        string prefsKey = GetAguardientePrefsKey(tier);
        return PlayerPrefs.GetInt(prefsKey, 0) >= AGUARDIENTE_REQUIRED_COUNT;
    }

    /// <summary>
    /// Verifica si la skin Golden está desbloqueada.
    /// </summary>
    public bool IsGoldenUnlocked()
    {
        return PlayerPrefs.GetInt(PREFS_GOLDEN, 0) == 1;
    }

    /// <summary>
    /// Obtiene el progreso de consumo de aguardiente del tier especificado (0-5).
    /// </summary>
    public int GetAguardienteCount(AguardienteItem.Tier tier)
    {
        string prefsKey = GetAguardientePrefsKey(tier);
        return PlayerPrefs.GetInt(prefsKey, 0);
    }

    /// <summary>
    /// Verifica si una skin en el índice especificado está desbloqueada.
    /// Índice 0 = ChocoRiko (siempre desbloqueada)
    /// Índice 1 = Patito
    /// Índice 2 = Aguardiente AZUL (Tier 1)
    /// Índice 3 = Aguardiente ROJO (Tier 2)
    /// Índice 4 = Aguardiente VERDE (Tier 3)
    /// Índice 5 = GOLDEN
    /// </summary>
    public bool IsSkinUnlocked(int skinIndex)
    {
        return skinIndex switch
        {
            0 => true,
            1 => IsPatitoUnlocked(),
            2 => IsAguardienteUnlocked(AguardienteItem.Tier.Tier1),
            3 => IsAguardienteUnlocked(AguardienteItem.Tier.Tier2),
            4 => IsAguardienteUnlocked(AguardienteItem.Tier.Tier3),
            5 => IsGoldenUnlocked(),
            _ => false
        };
    }

    /// <summary>
    /// Obtiene la condición de desbloqueo para la skin en el índice especificado.
    /// </summary>
    public string GetUnlockCondition(int skinIndex)
    {
        return skinIndex switch
        {
            0 => "",
            1 => "Come un Patito por primera vez",
            2 => "Toma 5 aguardientes azules",
            3 => "Toma 5 aguardientes rojos",
            4 => "Toma 5 aguardientes verdes",
            5 => "Alcanza 1000 puntos en una partida",
            _ => ""
        };
    }

    private string GetAguardientePrefsKey(AguardienteItem.Tier tier)
    {
        return tier switch
        {
            AguardienteItem.Tier.Tier1 => PREFS_AGUARDIENTE_TIER1,
            AguardienteItem.Tier.Tier2 => PREFS_AGUARDIENTE_TIER2,
            AguardienteItem.Tier.Tier3 => PREFS_AGUARDIENTE_TIER3,
            _ => PREFS_AGUARDIENTE_TIER1
        };
    }
}
