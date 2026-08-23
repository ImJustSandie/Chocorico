using UnityEngine;

/// <summary>
/// Representa la información visual de una skin basada en cambio de Material.
/// </summary>
[System.Serializable]
public class SkinData
{
    [Tooltip("Nombre de la skin (ej. 'Default', 'Gold', 'Neon')")]
    public string skinName;

    [TextArea(2, 5)]
    [Tooltip("Descripción detallada de la skin")]
    public string description;

    [Tooltip("Material principal (para personaje de 1 solo mesh o fallback)")]
    public Material skinMaterial;

    [Tooltip("Materiales específicos para personajes con múltiples meshes (ej. Mesh 1, Mesh 2, Mesh 3)")]
    public Material[] skinMaterials;

    [Tooltip("Ícono opcional para mostrar en la interfaz de usuario")]
    public Sprite icon;

    /// <summary>
    /// Obtiene el material correspondiente para el renderer en el índice especificado.
    /// Si hay un material en skinMaterials[index], lo retorna; de lo contrario usa skinMaterial como fallback.
    /// </summary>
    public Material GetMaterialForRenderer(int index)
    {
        if (skinMaterials != null && index >= 0 && index < skinMaterials.Length && skinMaterials[index] != null)
        {
            return skinMaterials[index];
        }
        return skinMaterial;
    }
}
