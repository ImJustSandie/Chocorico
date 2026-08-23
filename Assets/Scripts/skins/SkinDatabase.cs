using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catálogo/Base de datos de skins disponibles en el juego.
/// Se puede crear como asset desde el menú Assets -> Create -> Chocorico -> Skin Database.
/// Si se guarda en una carpeta 'Resources' con el nombre 'SkinDatabase', se cargará automáticamente.
/// </summary>
[CreateAssetMenu(fileName = "SkinDatabase", menuName = "Chocorico/Skin Database")]
public class SkinDatabase : ScriptableObject
{
    [Header("Lista de Skins")]
    [SerializeField] private List<SkinData> skins = new List<SkinData>();

    public IReadOnlyList<SkinData> Skins => skins;
    public int Count => skins.Count;

    /// <summary>
    /// Obtiene la skin en el índice especificado (con validación de rango).
    /// </summary>
    public SkinData GetSkin(int index)
    {
        if (skins == null || skins.Count == 0)
        {
            Debug.LogWarning("SkinDatabase: No hay skins configuradas.");
            return null;
        }

        int clampedIndex = Mathf.Clamp(index, 0, skins.Count - 1);
        return skins[clampedIndex];
    }

    private static SkinDatabase cachedInstance;

    /// <summary>
    /// Intenta cargar la base de datos desde la carpeta Resources/SkinDatabase si no hay referencia asignada.
    /// </summary>
    public static SkinDatabase GetDefaultDatabase()
    {
        if (cachedInstance == null)
        {
            cachedInstance = Resources.Load<SkinDatabase>("SkinDatabase");
        }
        return cachedInstance;
    }
}
