using UnityEngine;

/// <summary>
/// Objeto decorativo del fondo: sin colisiones ni lógica de juego.
/// El sprite y color los asigna BackgroundManager al instanciarlo.
/// El MOVIMIENTO lo controla el BackgroundManager (todos caen a la misma velocidad),
/// este componente solo expone datos para posicionarlo y saber cuándo destruirlo.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundDecor : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public SpriteRenderer Renderer
    {
        get
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            return spriteRenderer;
        }
    }

    /// <summary>
    /// Configura el objeto decorativo desde el BackgroundManager.
    /// </summary>
    public void Setup(Sprite sprite, Color color, int sortingOrder)
    {
        if (sprite != null)
        {
            Renderer.sprite = sprite;
        }

        Renderer.color = color;
        Renderer.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// Indica si el objeto ya salió por la parte inferior de la pantalla.
    /// </summary>
    public bool IsBelowScreen(float screenBottom)
    {
        float margin = 3f;
        Sprite sprite = Renderer.sprite;
        if (sprite != null)
        {
            margin += Renderer.bounds.extents.y;
        }

        return transform.position.y < (screenBottom - margin);
    }
}
