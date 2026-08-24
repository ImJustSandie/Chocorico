using UnityEngine;

/// <summary>
/// Se encarga de instanciar objetos (positivos o negativos) en la parte superior de la pantalla
/// a intervalos regulares y con posiciones horizontales aleatorias entre las paredes.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    public enum SpawnMode
    {
        AutomaticScreenBounds, // Calcula automáticamente el ancho y alto de la cámara
        AroundSpawnerTransform, // Genera relativo a la posición de este GameObject Spawner
        SpecificSpawnPoints    // Genera aleatoriamente en una lista de GameObjects vacíos (puntos)
    }

    [Header("Modo de Posicionamiento")]
    [Tooltip("Cómo se determina la posición de spawn: Automático (pantalla), Alrededor del Spawner, o Puntos Específicos")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.AroundSpawnerTransform;

    [Header("Puntos de Spawn Específicos")]
    [Tooltip("Lista de GameObjects vacíos donde pueden aparecer los ítems (si el modo es SpecificSpawnPoints)")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Área de Generación (Para modo AroundSpawnerTransform)")]
    [Tooltip("Ancho de la zona de generación centrada en este GameObject")]
    [SerializeField] private float spawnAreaWidth = 6f;

    [Header("Prefab")]
    [Tooltip("Prefab del objeto que contiene el script EnergyItem y SpriteRenderer")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Intervalos de Generación (Nivel 1 por defecto)")]
    [Tooltip("Tiempo mínimo entre cada generación (segundos) - Usado si no hay LevelManager")]
    [SerializeField] private float minSpawnInterval = 1.5f;

    [Tooltip("Tiempo máximo entre cada generación (segundos) - Usado si no hay LevelManager")]
    [SerializeField] private float maxSpawnInterval = 3.0f;

    [Header("Probabilidades")]
    [Tooltip("Probabilidad de que el objeto sea Positivo (0.0 a 1.0). Ejemplo: 0.6 = 60% positivo, 40% negativo - Usado si no hay LevelManager")]
    [Range(0f, 1f)]
    [SerializeField] private float positiveProbability = 0.6f;

    [Header("Área de Pantalla (Para modo AutomaticScreenBounds)")]
    [Tooltip("Margen respecto a los bordes de la pantalla para evitar que aparezca dentro de las paredes")]
    [SerializeField] private float horizontalPadding = 1.5f;

    [Tooltip("Distancia por encima de la parte superior de la pantalla")]
    [SerializeField] private float spawnOffsetY = 1.0f;

    private float timer = 0f;
    private float nextSpawnTime = 2f;

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.CanSpawn)
            return;

        timer += Time.deltaTime;
        if (timer >= nextSpawnTime)
        {
            SpawnItem();
            timer = 0f;
            SetNextSpawnTime();
        }
    }

    /// <summary>
    /// Genera un nuevo ítem según el modo de posicionamiento configurado.
    /// </summary>
    private void SpawnItem()
    {
        if (itemPrefab == null)
        {
            Debug.LogWarning("ItemSpawner: No se ha asignado un 'itemPrefab' en el Inspector.");
            return;
        }

        Vector3 spawnPosition = CalculateSpawnPosition();

        // Instanciar el objeto
        GameObject spawnedObj = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);

        EnergyItem energyItem = spawnedObj.GetComponent<EnergyItem>();
        if (energyItem != null)
        {
            float prob = (LevelManager.Instance != null) ? LevelManager.Instance.PositiveProbability : positiveProbability;
            EnergyItem.ItemType type = (Random.value <= prob)
                ? EnergyItem.ItemType.Positive
                : EnergyItem.ItemType.Negative;

            energyItem.SetItemType(type);
        }
    }

    /// <summary>
    /// Calcula la posición de spawn según el modo seleccionado.
    /// </summary>
    private Vector3 CalculateSpawnPosition()
    {
        switch (spawnMode)
        {
            case SpawnMode.AroundSpawnerTransform:
                // Basado en la posición actual de este GameObject vacío
                float halfWidth = spawnAreaWidth / 2f;
                float posX = Random.Range(transform.position.x - halfWidth, transform.position.x + halfWidth);
                return new Vector3(posX, transform.position.y, transform.position.z);

            case SpawnMode.SpecificSpawnPoints:
                // Elige un punto aleatorio de la lista de Transforms
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    Transform chosenPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    if (chosenPoint != null)
                        return chosenPoint.position;
                }
                // Si no hay puntos asignados, usa la posición del Spawner
                return transform.position;

            case SpawnMode.AutomaticScreenBounds:
            default:
                // Basado en los bordes de la cámara en GameManager
                float minX = -3f;
                float maxX = 3f;
                float spawnY = 6f;

                if (GameManager.Instance != null)
                {
                    minX = GameManager.Instance.ScreenLeft + horizontalPadding;
                    maxX = GameManager.Instance.ScreenRight - horizontalPadding;
                    spawnY = GameManager.Instance.ScreenTop + spawnOffsetY;
                }

                return new Vector3(Random.Range(minX, maxX), spawnY, 0f);
        }
    }

    private void SetNextSpawnTime()
    {
        if (LevelManager.Instance != null)
        {
            nextSpawnTime = Random.Range(
                LevelManager.Instance.ItemMinSpawnInterval,
                LevelManager.Instance.ItemMaxSpawnInterval
            );
        }
        else
        {
            nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    /// <summary>
    /// Dibuja una línea guía en la vista de escena de Unity para ver la zona de generación.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (spawnMode == SpawnMode.AroundSpawnerTransform)
        {
            Vector3 from = transform.position + Vector3.left * (spawnAreaWidth / 2f);
            Vector3 to = transform.position + Vector3.right * (spawnAreaWidth / 2f);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(from, 0.2f);
            Gizmos.DrawWireSphere(to, 0.2f);
        }
    }
}
