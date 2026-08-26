using UnityEngine;

public class WallObjectSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject aguardientePrefab;
    [SerializeField] private GameObject puaPrefab;

    [Header("Spawn Aguardiente (Nivel 1 por defecto)")]
    [SerializeField] private float minAguardienteInterval = 8f;
    [SerializeField] private float maxAguardienteInterval = 15f;

    [Header("Spawn Púas (Nivel 1 por defecto)")]
    [SerializeField] private float minPuaInterval = 4f;
    [SerializeField] private float maxPuaInterval = 8f;
    [SerializeField] private float puaSpacing = 0.8f;

    [Header("Pared")]
    [SerializeField] private bool spawnOnLeftWall = true;
    [SerializeField] private bool leftWallGoesUp = true;

    [Header("Posición de Spawn")]
    [Tooltip("La X de este GameObject define dónde aparecen los objetos (colócalo en el borde de la pared). La Y se calcula automáticamente fuera de pantalla.")]
    [SerializeField] private bool useSpawnerX = true;

    [Tooltip("Si useSpawnerX es false, se usa el borde de pantalla + este offset")]
    [SerializeField] private float screenEdgeFallbackOffset = 0.5f;

    [Header("Gizmo")]
    [SerializeField] private bool drawSpawnGizmo = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    private float nextAguardienteSpawn;
    private float nextPuaSpawn;
    private bool aguardienteActive = false;

    private const float spawnRetryDelay = 0.5f;

    private WallSpawnCoordinator.Side Side => spawnOnLeftWall ? WallSpawnCoordinator.Side.Left : WallSpawnCoordinator.Side.Right;

    void Start()
    {
        ScheduleNextAguardiente();
        ScheduleNextPua();
    }

    void Update()
    {
        // No generar objetos si la partida no ha comenzado o ya terminó
        if (GameManager.Instance == null || !GameManager.Instance.CanSpawn)
            return;

        if (Time.time >= nextAguardienteSpawn && !aguardienteActive)
        {
            if (WallSpawnCoordinator.CanSpawnAguardiente(Side))
            {
                SpawnAguardiente();
                ScheduleNextAguardiente();
            }
            else
            {
                // Pared ocupada por púas u otro aguardiente: reintentar pronto
                nextAguardienteSpawn = Time.time + spawnRetryDelay;
            }
        }

        if (Time.time >= nextPuaSpawn)
        {
            if (WallSpawnCoordinator.CanSpawnPua(Side))
            {
                SpawnPuaGroup();
                ScheduleNextPua();
            }
            else
            {
                // Hay púas activas en otra pared o un aguardiente en esta: reintentar pronto
                nextPuaSpawn = Time.time + spawnRetryDelay;
            }
        }
    }

    private void ScheduleNextAguardiente()
    {
        float min = (LevelManager.Instance != null) ? LevelManager.Instance.AguardienteMinInterval : minAguardienteInterval;
        float max = (LevelManager.Instance != null) ? LevelManager.Instance.AguardienteMaxInterval : maxAguardienteInterval;
        nextAguardienteSpawn = Time.time + Random.Range(min, max);
    }

    private void ScheduleNextPua()
    {
        float min = (LevelManager.Instance != null) ? LevelManager.Instance.PuaMinInterval : minPuaInterval;
        float max = (LevelManager.Instance != null) ? LevelManager.Instance.PuaMaxInterval : maxPuaInterval;
        nextPuaSpawn = Time.time + Random.Range(min, max);
    }

    private void SpawnAguardiente()
    {
        if (aguardientePrefab == null) return;

        WallDirection dir = GetWallDirection();
        Vector3 spawnPos = CalculateSpawnPosition(dir);

        GameObject obj = Instantiate(aguardientePrefab, spawnPos, Quaternion.identity);

        if (GameManager.Instance != null && GameManager.Instance.SpawnedObjectsContainer != null)
        {
            obj.transform.SetParent(GameManager.Instance.SpawnedObjectsContainer);
        }

        WallObject wallObj = obj.GetComponent<WallObject>();
        if (wallObj != null)
        {
            wallObj.Initialize(dir, Side);
        }

        AguardienteItem item = obj.GetComponent<AguardienteItem>();
        if (item != null)
        {
            int randomTier = Random.Range(0, 3);
            item.SetTier((AguardienteItem.Tier)randomTier);
        }

        aguardienteActive = true;
        Invoke(nameof(ResetAguardienteActive), 10f);
    }

    private void ResetAguardienteActive()
    {
        aguardienteActive = false;
    }

    private void SpawnPuaGroup()
    {
        if (puaPrefab == null) return;

        WallDirection dir = GetWallDirection();
        int count = Random.Range(1, 5);
        Vector3 basePos = CalculateSpawnPosition(dir);

        // Orientar las púas según la pared: en la pared izquierda se espejan (180° en Y)
        // para que las puntas apunten hacia el centro de la pantalla
        Quaternion rotation = spawnOnLeftWall ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

        for (int i = 0; i < count; i++)
        {
            float offsetY = (dir == WallDirection.Up) ? -(i * puaSpacing) : (i * puaSpacing);
            Vector3 pos = basePos + new Vector3(0f, offsetY, 0f);

            GameObject obj = Instantiate(puaPrefab, pos, rotation);

            if (GameManager.Instance != null && GameManager.Instance.SpawnedObjectsContainer != null)
            {
                obj.transform.SetParent(GameManager.Instance.SpawnedObjectsContainer);
            }

            WallObject wallObj = obj.GetComponent<WallObject>();
            if (wallObj != null)
            {
                wallObj.Initialize(dir, Side);
            }
        }
    }

    private WallDirection GetWallDirection()
    {
        if (spawnOnLeftWall)
            return leftWallGoesUp ? WallDirection.Up : WallDirection.Down;
        else
            return leftWallGoesUp ? WallDirection.Down : WallDirection.Up;
    }

    private Vector3 CalculateSpawnPosition(WallDirection dir)
    {
        Vector3 pos = transform.position;
        GetScreenBounds(out float screenLeft, out float screenRight, out float screenTop, out float screenBottom);

        if (!useSpawnerX)
        {
            if (spawnOnLeftWall)
                pos.x = screenLeft + screenEdgeFallbackOffset;
            else
                pos.x = screenRight - screenEdgeFallbackOffset;
        }

        if (dir == WallDirection.Up)
            pos.y = screenBottom - 2f;
        else
            pos.y = screenTop + 2f;

        return pos;
    }

    private void GetScreenBounds(out float left, out float right, out float top, out float bottom)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
            Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
            left = bottomLeft.x;
            bottom = bottomLeft.y;
            right = topRight.x;
            top = topRight.y;
        }
        else
        {
            left = -9f; right = 9f; bottom = -5f; top = 5f;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawSpawnGizmo) return;

        Gizmos.color = gizmoColor;
        Vector3 spawnPos = CalculateSpawnPosition(GetWallDirection());
        Gizmos.DrawWireCube(spawnPos, Vector3.one * 0.5f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }
}
