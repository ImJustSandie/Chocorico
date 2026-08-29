using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la selección de skins en la escena de skins (skin.unity).
/// Permite navegar entre las distintas skins (Materiales), previsualizar el cambio
/// en el modelo 3D y guardar la elección para la escena de juego.
/// </summary>
public class SkinSelectionManager : MonoBehaviour
{
    public const string SKIN_PREFS_KEY = "SelectedSkinIndex";

    [Header("Base de Datos de Skins")]
    [Tooltip("Base de datos ScriptableObject. Si se deja nula, intentará cargar 'Resources/SkinDatabase'.")]
    [SerializeField] private SkinDatabase database;

    [Header("Previsualización 3D")]
    [Tooltip("Renderer principal del personaje (si tiene 1 solo mesh)")]
    [SerializeField] private Renderer previewRenderer;

    [Tooltip("Renderers del personaje cuando está compuesto por múltiples meshes (ej. 3 meshes)")]
    [SerializeField] private Renderer[] previewRenderers;

    [Tooltip("Transform raíz/padre del personaje a rotar. Si se deja nulo, intentará rotar el objeto de previewRenderer.")]
    [SerializeField] private Transform characterTransform;

    [Header("Rotación Continua")]
    [Tooltip("Activa la rotación automática de 360° indefinida del personaje.")]
    [SerializeField] private bool autoRotate = true;

    [Tooltip("Velocidad de rotación en grados por segundo (eje Y).")]
    [SerializeField] private float rotationSpeed = 45f;

    [Header("UI (Opcional)")]
    [Tooltip("Texto para mostrar el nombre de la skin seleccionada")]
    [SerializeField] private TextMeshProUGUI skinNameText;

    [Tooltip("Texto para mostrar la descripción de la skin seleccionada")]
    [SerializeField] private TextMeshProUGUI skinDescriptionText;

    [Tooltip("Ícono visual para la skin seleccionada")]
    [SerializeField] private Image skinIconImage;

    [Header("Escena a Cargar")]
    [Tooltip("Nombre de la escena de juego a cargar tras seleccionar skin")]
    [SerializeField] private string gameSceneName = "game";

    [Header("Skins Bloqueadas")]
    [Tooltip("Material negro para mostrar skins bloqueadas")]
    [SerializeField] private Material lockedSkinMaterial;

    [Tooltip("Botón de jugar/seleccionar (se deshabilita si la skin está bloqueada)")]
    [SerializeField] private Button playButton;

    [Tooltip("Texto de condición de desbloqueo (se muestra en lugar de la descripción si está bloqueada)")]
    [SerializeField] private TextMeshProUGUI unlockConditionText;

    private int currentIndex = 0;

    private void Start()
    {
        // 1. Obtener base de datos si no fue asignada en Inspector
        if (database == null)
        {
            database = SkinDatabase.GetDefaultDatabase();
        }

        if (database == null || database.Count == 0)
        {
            Debug.LogWarning("SkinSelectionManager: No se encontró SkinDatabase o está vacía.");
            return;
        }

        // 2. Cargar el último índice guardado en PlayerPrefs
        currentIndex = PlayerPrefs.GetInt(SKIN_PREFS_KEY, 0);
        if (currentIndex < 0 || currentIndex >= database.Count)
        {
            currentIndex = 0;
        }

        // 3. Actualizar la previsualización inicial
        UpdatePreview();
    }

    private void Update()
    {
        if (!autoRotate) return;

        // Determinar el Transform a rotar
        Transform target = characterTransform;
        if (target == null && previewRenderer != null)
        {
            target = previewRenderer.transform;
        }
        else if (target == null && previewRenderers != null && previewRenderers.Length > 0 && previewRenderers[0] != null)
        {
            target = previewRenderers[0].transform.parent != null ? previewRenderers[0].transform.parent : previewRenderers[0].transform;
        }

        if (target != null)
        {
            // Rotar continuamente alrededor del eje vertical (Y)
            target.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    /// <summary>
    /// Cambia a la siguiente skin en la lista.
    /// </summary>
    public void NextSkin()
    {
        if (database == null || database.Count == 0) return;

        currentIndex = (currentIndex + 1) % database.Count;
        UpdatePreview();
    }

    /// <summary>
    /// Cambia a la skin anterior en la lista.
    /// </summary>
    public void PreviousSkin()
    {
        if (database == null || database.Count == 0) return;

        currentIndex = (currentIndex - 1 + database.Count) % database.Count;
        UpdatePreview();
    }

    /// <summary>
    /// Guarda la skin elegida en PlayerPrefs y carga la escena de juego.
    /// </summary>
    public void SelectAndPlay()
    {
        PlayerPrefs.SetInt(SKIN_PREFS_KEY, currentIndex);
        PlayerPrefs.Save();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    /// <summary>
    /// Actualiza el material del modelo y los elementos visuales de UI.
    /// </summary>
    private void UpdatePreview()
    {
        SkinData currentSkin = database.GetSkin(currentIndex);
        if (currentSkin == null) return;

        bool isUnlocked = SkinUnlockManager.Instance != null ? SkinUnlockManager.Instance.IsSkinUnlocked(currentIndex) : true;

        // Aplicar Material(es) a los Renderers del modelo de vista previa
        if (isUnlocked)
        {
            // Skin desbloqueada: aplicar material normal
            if (previewRenderers != null && previewRenderers.Length > 0)
            {
                for (int i = 0; i < previewRenderers.Length; i++)
                {
                    if (previewRenderers[i] != null)
                    {
                        Material mat = currentSkin.GetMaterialForRenderer(i);
                        if (mat != null)
                        {
                            previewRenderers[i].material = mat;
                        }
                    }
                }
            }
            else if (previewRenderer != null)
            {
                Material mat = currentSkin.GetMaterialForRenderer(0);
                if (mat != null)
                {
                    previewRenderer.material = mat;
                }
            }
        }
        else
        {
            // Skin bloqueada: aplicar material negro
            if (lockedSkinMaterial != null)
            {
                if (previewRenderers != null && previewRenderers.Length > 0)
                {
                    for (int i = 0; i < previewRenderers.Length; i++)
                    {
                        if (previewRenderers[i] != null)
                        {
                            previewRenderers[i].material = lockedSkinMaterial;
                        }
                    }
                }
                else if (previewRenderer != null)
                {
                    previewRenderer.material = lockedSkinMaterial;
                }
            }
        }

        // Actualizar UI de texto (nombre y descripción)
        if (skinNameText != null)
        {
            skinNameText.text = string.IsNullOrEmpty(currentSkin.skinName) ? $"Skin {currentIndex + 1}" : currentSkin.skinName;
        }

        // Mostrar descripción o condición de desbloqueo
        if (isUnlocked)
        {
            if (skinDescriptionText != null)
            {
                skinDescriptionText.text = currentSkin.description ?? "";
            }
        }
        else
        {
            string condition = SkinUnlockManager.Instance != null ? SkinUnlockManager.Instance.GetUnlockCondition(currentIndex) : "";
            if (skinDescriptionText != null)
            {
                skinDescriptionText.text = $"Bloqueado: {condition}";
            }
            if (unlockConditionText != null && unlockConditionText != skinDescriptionText)
            {
                unlockConditionText.text = $"Bloqueado: {condition}";
                unlockConditionText.gameObject.SetActive(true);
            }
        }

        // Actualizar UI de ícono
        if (skinIconImage != null)
        {
            if (currentSkin.icon != null)
            {
                skinIconImage.sprite = currentSkin.icon;
                skinIconImage.gameObject.SetActive(true);
            }
            else
            {
                skinIconImage.gameObject.SetActive(false);
            }
        }

        // Habilitar/deshabilitar botón de jugar
        if (playButton != null)
        {
            playButton.interactable = isUnlocked;
        }
    }
}
