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

        // Solo drenar energía pasiva una vez que el jugador inicia la partida
        if (isGameStarted && currentEnergy > 0f && drainPauseTimer <= 0f)
        {
            currentEnergy -= passiveDrainPerSecond * Time.deltaTime;
            currentEnergy = Mathf.Max(currentEnergy, 0f);
            UpdateEnergyBar();
        }

        // Contador del debuff negativo
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
            Debug.Log("¡Sin energía! No se puede saltar.");
            return false;
        }

        // Si el debuff negativo está activo, se drena energía en este salto
        if (IsJumpDrainActive)
        {
            currentEnergy -= jumpDrainOnDebuff;
            currentEnergy = Mathf.Max(currentEnergy, 0f);
            UpdateEnergyBar();
            Debug.Log($"[Debuff Activo] Salto costó {jumpDrainOnDebuff} de energía. Restante: {currentEnergy}");
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
            Debug.Log("¡Sin energía! No se puede cancelar el salto.");
            return false;
        }

        // Costo base, o el doble si tiene el debuff del Gansito activo
        float cost = IsJumpDrainActive ? (cancelJumpBaseCost * 2f) : cancelJumpBaseCost;

        currentEnergy -= cost;
        currentEnergy = Mathf.Max(currentEnergy, 0f);
        UpdateEnergyBar();

        Debug.Log($"[Cancel Jump] Consumió {cost} de energía (Debuff Gansito: {IsJumpDrainActive}). Restante: {currentEnergy}");
        return true;
    }

    /// <summary>
    /// Activa el efecto negativo: durante 'jumpDrainDebuffDuration' segundos, saltar costará energía.
    /// </summary>
    public void ApplyJumpDrainDebuff()
    {
        debuffTimer = jumpDrainDebuffDuration;
        Debug.Log($"¡Efecto negativo activado! Saltar costará energía durante {jumpDrainDebuffDuration} segundos.");
    }

    /// <summary>
    /// Añade energía (usado por el objeto positivo).
    /// </summary>
    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        UpdateEnergyBar();
        Debug.Log($"Energía recargada (+{amount}). Total: {currentEnergy}/{maxEnergy}");
    }

    /// <summary>
    /// Pausa el drenaje pasivo de energía durante la duración especificada.
    /// </summary>
    public void PauseDrain(float duration)
    {
        drainPauseTimer = duration;
        Debug.Log($"Drenaje de energía pausado por {duration} segundos.");
    }

    /// <summary>
    /// Drena energía directamente (usado por objetos dañinos como púas).
    /// </summary>
    public void DrainEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Max(currentEnergy, 0f);
        UpdateEnergyBar();
        Debug.Log($"Energía drenada (-{amount}). Restante: {currentEnergy}");
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
