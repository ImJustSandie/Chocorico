using UnityEngine;

/// <summary>
/// Representa un objeto/ítem coleccionable en la escena.
/// Cae continuamente hacia abajo y cambia de color según su tipo:
/// - Positivo: Verde (recarga energía)
/// - Negativo: Rojo (activa penalización de salto por 3s)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnergyItem : MonoBehaviour
{
    public enum ItemType
    {
        Positive, // Otorga energía al jugador (Verde)
        Negative  // Activa el debuff de 3s donde cada salto drena energía (Rojo)
    }

    [Header("Configuración del Ítem")]
    [Tooltip("Tipo de objeto: Positivo (Verde/energía) o Negativo (Rojo/debuff)")]
    [SerializeField] private ItemType itemType = ItemType.Positive;

    [Tooltip("Cantidad de energía que recarga (solo si es Positivo)")]
    [SerializeField] private float energyToRestore = 25f;

    [Header("Movimiento")]
    [Tooltip("Velocidad de caída del objeto")]
    [SerializeField] private float fallSpeed = 3f;

    [Header("Sprites por Tipo")]
    [Tooltip("Sprite para el ítem Positivo (ej. Chocorramo)")]
    [SerializeField] private Sprite positiveSprite;
    [Tooltip("Sprite para el ítem Negativo (ej. Gansito)")]
    [SerializeField] private Sprite negativeSprite;

    [Header("Colores (Fallback si no hay sprite)")]
    [SerializeField] private Color positiveColor = new Color(0.2f, 0.9f, 0.2f); // Verde
    [SerializeField] private Color negativeColor = new Color(0.9f, 0.2f, 0.2f); // Rojo

    [Header("Efectos")]
    [Tooltip("Destruir el objeto al ser recogido por el jugador")]
    [SerializeField] private bool destroyOnCollect = true;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyVisuals();
    }

    void Update()
    {
        // Mover el objeto hacia abajo
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

        // Destruir únicamente si cae por debajo del límite inferior de la pantalla
        if (GameManager.Instance != null && transform.position.y < (GameManager.Instance.ScreenBottom - 2f))
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Configura el tipo de ítem desde código (útil para el Spawner).
    /// </summary>
    public void SetItemType(ItemType type)
    {
        itemType = type;
        ApplyVisuals();
    }

    /// <summary>
    /// Aplica el sprite o color correspondiente en el SpriteRenderer.
    /// </summary>
    private void ApplyVisuals()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            Sprite targetSprite = (itemType == ItemType.Positive) ? positiveSprite : negativeSprite;
            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
                spriteRenderer.color = Color.white; // Respetar colores originales de la imagen
            }
            else
            {
                spriteRenderer.color = (itemType == ItemType.Positive) ? positiveColor : negativeColor;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar si colisionó con el jugador o un objeto que tenga EnergyManager
        EnergyManager energyManager = other.GetComponent<EnergyManager>();

        if (energyManager != null)
        {
            if (itemType == ItemType.Positive)
            {
                // Efecto Positivo: Otorga energía
                energyManager.AddEnergy(energyToRestore);
            }
            else if (itemType == ItemType.Negative)
            {
                // Efecto Negativo: Activa penalización de 3s al saltar
                energyManager.ApplyJumpDrainDebuff();
            }

            if (destroyOnCollect)
            {
                Destroy(gameObject);
            }
        }
    }
}
