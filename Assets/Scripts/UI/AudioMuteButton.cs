using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón que muta/desmuta una capa de audio del AudioManager (música o efectos).
/// Tiene dos estados visuales: un sprite cuando la capa está audible y otro
/// cuando está muteada. El sprite se refresca automáticamente al habilitarse,
/// así que varios botones sincronizados con el mismo estado siempre muestran
/// el icono correcto.
/// Setup en el Editor:
///   1. Agregar este componente a un objeto UI con Image + Button.
///   2. Elegir la 'layer' que controla este botón (Música o Efectos).
///   3. Asignar los dos sprites: 'unmutedSprite' (sonando) y 'mutedSprite' (muteada).
///   4. No hace falta conectar el onClick: este componente lo engancha solo,
///      pero también se puede llamar a ToggleMute() desde un UnityEvent.
/// </summary>
public class AudioMuteButton : MonoBehaviour
{
    public enum AudioLayer
    {
        Music,  // Capa de música
        Sfx     // Capa de efectos de sonido
    }

    [Header("Capa de audio")]
    [Tooltip("Capa del AudioManager que este botón mutea/desmuta")]
    [SerializeField] private AudioLayer layer = AudioLayer.Music;

    [Header("Estados visuales")]
    [Tooltip("Image que muestra el estado (si queda vacía usa la Image de este objeto)")]
    [SerializeField] private Image targetImage;

    [Tooltip("Sprite cuando la capa está audible (sin mute)")]
    [SerializeField] private Sprite unmutedSprite;

    [Tooltip("Sprite cuando la capa está muteada")]
    [SerializeField] private Sprite mutedSprite;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(ToggleMute);
    }

    private void OnEnable()
    {
        // Refrescar por si otra instancia ya cambió el estado
        Refresh();
    }

    /// <summary>
    /// Alterna el mute de la capa asignada y actualiza el sprite.
    /// </summary>
    public void ToggleMute()
    {
        if (AudioManager.Instance == null)
            return;

        if (layer == AudioLayer.Music)
            AudioManager.Instance.ToggleMusicMute();
        else
            AudioManager.Instance.ToggleSfxMute();

        Refresh();
    }

    /// <summary>
    /// Actualiza el sprite según el estado actual de la capa.
    /// </summary>
    public void Refresh()
    {
        if (AudioManager.Instance == null || targetImage == null)
            return;

        bool muted = (layer == AudioLayer.Music)
            ? AudioManager.Instance.IsMusicMuted
            : AudioManager.Instance.IsSfxMuted;

        Sprite sprite = muted ? mutedSprite : unmutedSprite;
        if (sprite != null)
            targetImage.sprite = sprite;
    }
}
