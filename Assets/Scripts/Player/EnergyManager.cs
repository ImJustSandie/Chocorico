using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Administra la energía del jugador: consumo pasivo con el tiempo,
/// bonificaciones/penalizaciones y efectos de objetos (positivos/negativos).
/// </summary>
public class EnergyManager : MonoBehaviour
{
    [Header("Energía")]
    [Tooltip("Energía máxima del jugador")]
    [SerializeField] private float maxEnergy = 100f;

    [Tooltip("Energía que se consume pasivamente cada segundo")]
    [SerializeField] private float passiveDrainPerSecond = 5f;

    [Tooltip("Energía base que consume cancelar un salto en el aire")]
    [SerializeField] private float cancelJumpBaseCost = 10f;

    [Tooltip("Energía que consume el salto SOLO durante el estado negativo")]
    [SerializeField] private float jumpDrainOnDebuff = 15f;

    [Tooltip("Duración del efecto negativo en segundos")]
    [SerializeField] private float jumpDrainDebuffDuration = 3f;

    [Header("UI")]
    [Tooltip("Referencia a la Image (Filled) de la barra de energía")]
    [SerializeField] private Image energyBar;

    private float currentEnergy;
    private float debuffTimer = 0f;
    private float drainPauseTimer = 0f;
    private bool isGameStarted = false;

    /// <summary>
    /// Energía actual del jugador (solo lectura).
    /// </summary>
    public float CurrentEnergy => currentEnergy;

    /// <summary>
    /// Energía máxima del jugador (solo lectura).
    /// </summary>
    public float MaxEnergy => maxEnergy;

    /// <summary>
    /// Indica si el jugador aún tiene energía.
    /// </summary>
    public bool HasEnergy => currentEnergy > 0f;

    /// <summary>
    /// Indica si el debuff negativo de salto está activo.
    /// </summary>
    public bool IsJumpDrainActive => debuffTimer > 0f;

    public bool IsGodModeActive
    {
        get
        {
            PlayerManager pm = GetComponent<PlayerManager>();
            if (pm != null)
            {
                return pm.IsGodModeEnabled;
            }
            return false;
        }
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        UpdateEnergyBar();
    }

    void Update()
    {
        if (drainPauseTimer > 0f)
        {
            drainPauseTimer -= Time.deltaTime;
        }

        if (isGameStarted && currentEnergy > 0f && drainPauseTimer <= 0f && !IsGodModeActive)
        {
            currentEnergy -= passiveDrainPerSecond * Time.deltaTime;
            currentEnergy = Mathf.Max(currentEnergy, 0f);
            UpdateEnergyBar();
        }

        if (debuffTimer > 0f)
        {
            debuffTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Inicia el drenado pasivo (se llama al dar el primer salto/tap).
    /// </summary>
    public void StartGame()
    {
        isGameStarted = true;
    }

    /// <summary>
    /// Verifica y procesa el salto. Por defecto NO gasta energía a menos que el debuff esté activo.
    /// Retorna false si el jugador no tiene energía.
    /// </summary>
    public bool TryConsumeJumpEnergy()
    {
        if (currentEnergy <= 0f)
        {
            return false;
        }

        if (IsJumpDrainActive)
        {
            currentEnergy -= jumpDrainOnDebuff;
            currentEnergy = Mathf.Max(currentEnergy, 0f);
            UpdateEnergyBar();
        }

        return true;
    }

    /// <summary>
    /// Verifica y consume la energía necesaria para cancelar el salto en el aire y devolverse a la pared de origen.
    /// Si está bajo el efecto negativo (Gansito), consume el doble de energía.
    /// </summary>
    public bool TryConsumeCancelJumpEnergy()
    {
        if (currentEnergy <= 0f)
        {
            return false;
        }

        float cost = IsJumpDrainActive ? (cancelJumpBaseCost * 2f) : cancelJumpBaseCost;

        currentEnergy -= cost;
        currentEnergy = Mathf.Max(currentEnergy, 0f);
        UpdateEnergyBar();

        return true;
    }

    public void ApplyJumpDrainDebuff()
    {
        debuffTimer = jumpDrainDebuffDuration;
    }

    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        UpdateEnergyBar();
    }

    public void PauseDrain(float duration)
    {
        drainPauseTimer = duration;
    }

    public void DrainEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Max(currentEnergy, 0f);
        UpdateEnergyBar();
    }

    /// <summary>
    /// Restaura la energía al máximo.
    /// </summary>
    public void ResetEnergy()
    {
        currentEnergy = maxEnergy;
        debuffTimer = 0f;
        drainPauseTimer = 0f;
        UpdateEnergyBar();
    }

    /// <summary>
    /// Actualiza la barra de energía en la UI.
    /// </summary>
    private void UpdateEnergyBar()
    {
        if (energyBar != null)
        {
            energyBar.fillAmount = currentEnergy / maxEnergy;
        }
    }
}
