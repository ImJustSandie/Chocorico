using UnityEngine;
using DG.Tweening;

/// <summary>
/// Anima los bordes del menú principal: al iniciar la partida, cada borde se
/// desliza hacia su dirección de salida con Ease In-Out usando DOTween,
/// dejando visible solo su propio 'remainingThickness píxeles medidos DENTRO
/// del canvas (aunque el rect se extienda más allá de la pantalla).
/// Setup en el Editor:
///   1. Agregar este componente al canvas del menú (o a un objeto vacío).
///   2. Por cada borde, agregar una entrada a 'borders', asignar su RectTransform
///      y la dirección de salida (ej. (0,-1) baja, (0,1) sube).
///   3. GameManager lo llama automáticamente vía el campo 'menuBorders'.
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

        [Tooltip("Grosor que queda visible de este borde al terminar, medido desde el borde de la pantalla (en píxeles de UI)")]
        public float remainingThickness = 30f;
    }

    [Header("Bordes")]
    [Tooltip("Bordes que se animan al iniciar la partida")]
    [SerializeField] private Border[] borders;

    [Header("Animación (DOTween)")]
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
    /// visible solo 'remainingThickness píxeles dentro del canvas, con Ease In-Out.
    /// </summary>
    public void Hide()
    {
        if (borders == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        // Límites del canvas en coordenadas de mundo
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);

        foreach (Border border in borders)
        {
            if (border.rect == null) continue;

            Vector2 dir = border.exitDirection.normalized;

            // Esquinas del borde en coordenadas de mundo (0=BL, 1=TL, 2=TR, 3=BR)
            Vector3[] corners = new Vector3[4];
            border.rect.GetWorldCorners(corners);

            // Escala mundo <- unidades de UI (anchoredPosition vive en unidades locales del canvas)
            float scale = canvas.transform.lossyScale.x;

            float travelWorld;
            bool vertical = Mathf.Abs(dir.y) > Mathf.Abs(dir.x);

            if (vertical)
            {
                float thicknessWorld = border.remainingThickness * scale;
                if (dir.y < 0f)
                {
                    // Baja: el borde superior del rect debe quedar en (fondo de pantalla + grosor)
                    travelWorld = corners[1].y - (canvasCorners[0].y + thicknessWorld);
                }
                else
                {
                    // Sube: el borde inferior del rect debe quedar en (tope de pantalla - grosor)
                    travelWorld = (canvasCorners[2].y - thicknessWorld) - corners[0].y;
                }
            }
            else
            {
                float thicknessWorld = border.remainingThickness * scale;
                if (dir.x > 0f)
                {
                    // Derecha: el borde izquierdo debe quedar en (derecha de pantalla - grosor)
                    travelWorld = (canvasCorners[2].x - thicknessWorld) - corners[0].x;
                }
                else
                {
                    // Izquierda: el borde derecho debe quedar en (izquierda de pantalla + grosor)
                    travelWorld = corners[2].x - (canvasCorners[0].x + thicknessWorld);
                }
            }

            travelWorld = Mathf.Max(0f, travelWorld);

            // Convertir de unidades de mundo a unidades de anchoredPosition
            float travelAnchored = travelWorld / scale;

            Vector2 target = border.rect.anchoredPosition + dir * travelAnchored;

            border.rect
                .DOAnchorPos(target, duration)
                .SetEase(ease);
        }
    }
}
