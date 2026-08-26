using UnityEngine;
using DG.Tweening;

/// <summary>
/// Anima los bordes del menú principal: al iniciar la partida, cada borde se
/// desliza hacia su dirección de salida con Ease In-Out usando DOTween,
/// dejando visible solo un grosor delgado (queda como marco del HUD).
/// Setup en el Editor:
///   1. Agregar este componente al canvas del menú (o a un objeto vacío).
///   2. Por cada borde, agregar una entrada a 'borders', asignar su RectTransform
///      y la dirección de salida (ej. (0,-1) baja, (0,1) sube).
///   3. GameManager lo llama automáticamente vía el campo 'menuBorders'.
/// Nota: asume bordes horizontales (el recorrido va sobre su altura).
/// </summary>
public class MenuBorders : MonoBehaviour
{
    [System.Serializable]
    public class Border
    {
        [Tooltip("RectTransform del borde")]
        public RectTransform rect;

        [Tooltip("Dirección de salida normalizada. Ej: (0,-1) el borde baja, (0,1) sube.")]
        public Vector2 exitDirection = new Vector2(0f, -1f);
    }

    [Header("Bordes")]
    [Tooltip("Bordes que se animan al iniciar la partida")]
    [SerializeField] private Border[] borders;

    [Header("Animación (DOTween)")]
    [Tooltip("Grosor que queda visible de cada borde al terminar la animación (en píxeles de UI)")]
    [SerializeField] private float remainingThickness = 30f;

    [Tooltip("Duración de la animación en segundos")]
    [SerializeField] private float duration = 0.9f;

    [Tooltip("Curva de easing (InOut por defecto)")]
    [SerializeField] private Ease ease = Ease.InOutQuad;

    /// <summary>
    /// Duración de la animación (para que otros scripts sincronicen, ej. ocultar el menú al terminar).
    /// </summary>
    public float Duration => duration;

    /// <summary>
    /// Desliza todos los bordes hacia su dirección de salida hasta dejar
    /// visible solo 'remainingThickness píxeles, con Ease In-Out.
    /// </summary>
    public void Hide()
    {
        if (borders == null) return;

        foreach (Border border in borders)
        {
            if (border.rect == null) continue;

            // Recorrer solo lo necesario: altura actual menos el grosor que debe quedar visible
            float travel = Mathf.Max(0f, border.rect.rect.height - remainingThickness);

            Vector2 target = border.rect.anchoredPosition
                + border.exitDirection.normalized * travel;

            border.rect
                .DOAnchorPos(target, duration)
                .SetEase(ease);
        }
    }
}
