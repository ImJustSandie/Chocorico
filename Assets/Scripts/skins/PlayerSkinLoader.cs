using UnityEngine;

/// <summary>
/// Aplica el o los Materiales de la skin seleccionada al personaje al iniciar la escena de juego.
/// Lee la clave 'SelectedSkinIndex' guardada en PlayerPrefs.
/// </summary>
public class PlayerSkinLoader : MonoBehaviour
{
    [Header("Base de Datos de Skins")]
    [Tooltip("Base de datos ScriptableObject. Si se deja nula, intentará cargar 'Resources/SkinDatabase'.")]
    [SerializeField] private SkinDatabase database;

    [Header("Renderers del Personaje")]
    [Tooltip("Renderer principal del personaje (si tiene 1 solo mesh)")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Lista/Arreglo de Renderers del personaje (para personajes compuestos de varios meshes, ej. 3)")]
    [SerializeField] private Renderer[] targetRenderers;

    private void Awake()
    {
        ApplySelectedSkin();
    }

    /// <summary>
    /// Lee la skin seleccionada de PlayerPrefs y la aplica a los Renderers del jugador.
    /// </summary>
    public void ApplySelectedSkin()
    {
        // 1. Si no se asignó nada en Inspector, buscar automáticamente los Renderers en los hijos
        if ((targetRenderers == null || targetRenderers.Length == 0) && targetRenderer == null)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }

        // 2. Cargar la base de datos si no fue asignada manualmente
        if (database == null)
        {
            database = SkinDatabase.GetDefaultDatabase();
        }

        if (database == null || database.Count == 0)
        {
            Debug.LogWarning("PlayerSkinLoader: SkinDatabase no está disponible o no contiene skins.");
            return;
        }

        // 3. Obtener el índice guardado de PlayerPrefs
        int selectedIndex = PlayerPrefs.GetInt(SkinSelectionManager.SKIN_PREFS_KEY, 0);
        SkinData selectedSkin = database.GetSkin(selectedIndex);

        if (selectedSkin == null)
        {
            Debug.LogWarning($"PlayerSkinLoader: No se pudo obtener la skin en el índice {selectedIndex}.");
            return;
        }

        // 4. Aplicar materiales a los renderers
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null)
                {
                    Material mat = selectedSkin.GetMaterialForRenderer(i);
                    if (mat != null)
                    {
                        targetRenderers[i].material = mat;
                    }
                }
            }
        }
        else if (targetRenderer != null)
        {
            Material mat = selectedSkin.GetMaterialForRenderer(0);
            if (mat != null)
            {
                targetRenderer.material = mat;
            }
        }
    }
}
