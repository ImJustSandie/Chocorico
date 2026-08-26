using UnityEngine;

/// <summary>
/// Controlador de animaciones para el personaje.
/// Administra las animaciones de Tap (jump), Hold (run/mantener presionado),
/// reposo en pared (idle), vuelo/caída (fly) y deslizamiento.
/// Compatible con parámetros nombrados "idle", "jump", "run", "fly" o "Anim_*".
/// </summary>
public class AnimationManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al Animator del personaje. Si no se asigna, intentará obtenerlo en Awake/Start.")]
    [SerializeField] private Animator animator;

    [Header("Nombres de Parámetros Animator")]
    [Tooltip("Nombre del parámetro bool o trigger para reposo.")]
    [SerializeField] private string idleParam = "idle";

    [Tooltip("Nombre del parámetro bool o trigger para salto / tap.")]
    [SerializeField] private string jumpParam = "jump";

    [Tooltip("Nombre del parámetro bool o trigger para avanzar/mantener presionado (Hold).")]
    [SerializeField] private string runParam = "run";

    [Tooltip("Nombre del parámetro bool o trigger para volar/caer sin energía.")]
    [SerializeField] private string flyParam = "fly";

    [Tooltip("Nombre del parámetro bool secundario para pegado en pared (opcional).")]
    [SerializeField] private string clingingParam = "Anim_Clinging";

    // Hashes para optimizar el acceso a parámetros del Animator
    private int idleHash;
    private int jumpHash;
    private int runHash;
    private int flyHash;
    private int clingingHash;

    private bool isHolding = false;

    private void Awake()
    {
        CacheHashes();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }

    private void CacheHashes()
    {
        idleHash = !string.IsNullOrEmpty(idleParam) ? Animator.StringToHash(idleParam) : 0;
        jumpHash = !string.IsNullOrEmpty(jumpParam) ? Animator.StringToHash(jumpParam) : 0;
        runHash = !string.IsNullOrEmpty(runParam) ? Animator.StringToHash(runParam) : 0;
        flyHash = !string.IsNullOrEmpty(flyParam) ? Animator.StringToHash(flyParam) : 0;
        clingingHash = !string.IsNullOrEmpty(clingingParam) ? Animator.StringToHash(clingingParam) : 0;
    }

    /// <summary>
    /// Permite asignar dinámicamente un nuevo Animator (útil al cambiar de skin).
    /// </summary>
    public void SetAnimator(Animator newAnimator)
    {
        animator = newAnimator;
    }

    /// <summary>
    /// Establece el estado de Hold (mantener presionado el toque sobre el mismo lado).
    /// </summary>
    public void SetHolding(bool holding)
    {
        isHolding = holding;
        if (holding)
        {
            PlayHoldAgainstBelt();
        }
    }

    /// <summary>
    /// Activa la animación de Reposo (Idle).
    /// </summary>
    public void PlayIdle()
    {
        isHolding = false;
        if (animator == null) return;

        SetParameterValue(idleHash, true);
        SetParameterValue(jumpHash, false);
        SetParameterValue(runHash, false);
        SetParameterValue(flyHash, false);
        SetParameterValue(clingingHash, false);
    }

    /// <summary>
    /// Activa la animación de Salto / Tap al hacer pulsación rápida hacia el lado contrario.
    /// </summary>
    public void PlayTapOrJump()
    {
        isHolding = false;
        if (animator == null) return;

        SetParameterValue(idleHash, false);
        SetParameterValue(jumpHash, true);
        SetParameterValue(runHash, false);
        SetParameterValue(flyHash, false);
        SetParameterValue(clingingHash, false);
    }

    /// <summary>
    /// Activa la animación de Avance / Escalada (Run / Hold) mientras se mantiene presionado el mismo lado.
    /// </summary>
    public void PlayHoldAgainstBelt()
    {
        isHolding = true;
        if (animator == null) return;

        SetParameterValue(idleHash, false);
        SetParameterValue(jumpHash, false);
        SetParameterValue(runHash, true);
        SetParameterValue(flyHash, false);
        SetParameterValue(clingingHash, true);
    }

    /// <summary>
    /// Activa la animación de Deslizamiento normal en la pared al no presionar ningún botón.
    /// </summary>
    public void PlayWallSlide()
    {
        isHolding = false;
        if (animator == null) return;

        // Al deslizarse, si existe el parámetro clinging lo usa; de lo contrario vuelve a idle
        if (clingingHash != 0 && HasParameter(clingingHash))
        {
            SetParameterValue(clingingHash, true);
            SetParameterValue(runHash, false);
            SetParameterValue(idleHash, false);
        }
        else
        {
            SetParameterValue(idleHash, true);
            SetParameterValue(runHash, false);
        }

        SetParameterValue(jumpHash, false);
        SetParameterValue(flyHash, false);
    }

    /// <summary>
    /// Activa la animación de Vuelo / Caída Libre (Fly) al agotarse la energía.
    /// </summary>
    public void PlayFalling()
    {
        isHolding = false;
        if (animator == null) return;

        SetParameterValue(idleHash, false);
        SetParameterValue(jumpHash, false);
        SetParameterValue(runHash, false);
        SetParameterValue(flyHash, true);
        SetParameterValue(clingingHash, false);
    }

    private void SetParameterValue(int hash, bool boolValue)
    {
        if (hash == 0 || animator == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == hash)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(hash, boolValue);
                }
                else if (param.type == AnimatorControllerParameterType.Trigger && boolValue)
                {
                    animator.SetTrigger(hash);
                }
                break;
            }
        }
    }

    private bool HasParameter(int paramHash)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == paramHash) return true;
        }
        return false;
    }
}
