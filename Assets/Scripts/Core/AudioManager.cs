using UnityEngine;

/// <summary>
/// Administra el audio del juego con dos capas:
/// - Música: una sola canción que suena mientras la partida está activa.
/// - Efectos de sonido: sonidos puntuales (aguardiente, ítem positivo,
///   ítem negativo y herida con púas).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum SfxType
    {
        Aguardiente,   // Al tomar un aguardiente
        PositiveItem,  // Al comer un objeto positivo
        NegativeItem,  // Al comer un objeto negativo
        PuaHit         // Al ser herido con púas
    }

    public static AudioManager Instance { get; private set; }

    [Header("Capa de Música")]
    [Tooltip("AudioSource dedicado a la música (recomendado: Play On Awake desactivado, Loop activado)")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Clip de música que se reproduce durante la partida")]
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Capa de Efectos de Sonido")]
    [Tooltip("AudioSource dedicado a los efectos (recomendado: Play On Awake desactivado, Loop desactivado)")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Sonido al tomar un aguardiente")]
    [SerializeField] private AudioClip aguardienteClip;

    [Tooltip("Sonido al comer un objeto positivo")]
    [SerializeField] private AudioClip positiveItemClip;

    [Tooltip("Sonido al comer un objeto negativo")]
    [SerializeField] private AudioClip negativeItemClip;

    [Tooltip("Sonido al ser herido con púas")]
    [SerializeField] private AudioClip puaHitClip;

    // Estado de mute por capa (independiente de que el AudioSource exista)
    private bool musicMuted = false;
    private bool sfxMuted = false;

    /// <summary>Indica si la capa de música está muteada.</summary>
    public bool IsMusicMuted => musicMuted;

    /// <summary>Indica si la capa de efectos está muteada.</summary>
    public bool IsSfxMuted => sfxMuted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            if (gameplayMusic != null)
                musicSource.clip = gameplayMusic;
        }
        
        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Inicia la música de gameplay (la llama GameManager al comenzar la partida).
    /// </summary>
    public void PlayMusic()
    {
        if (gameplayMusic == null || musicSource == null)
            return;

        if (musicSource.clip == gameplayMusic && musicSource.isPlaying)
            return;

        musicSource.clip = gameplayMusic;
        musicSource.Play();
    }

    /// <summary>
    /// Detiene la música (la llama GameManager al terminar la partida).
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
    }

    /// <summary>
    /// Reproduce un efecto de sonido puntual.
    /// </summary>
    public void PlaySfx(SfxType type)
    {
        if (sfxSource == null)
            return;

        AudioClip clip;
        switch (type)
        {
            case SfxType.Aguardiente: clip = aguardienteClip; break;
            case SfxType.PositiveItem: clip = positiveItemClip; break;
            case SfxType.NegativeItem: clip = negativeItemClip; break;
            case SfxType.PuaHit: clip = puaHitClip; break;
            default: clip = null; break;
        }

        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Mutea o desmutea la capa de música. No detiene la reproducción:
    /// al desmutear la música sigue desde donde iba.
    /// </summary>
    public void SetMusicMuted(bool muted)
    {
        musicMuted = muted;

        if (musicSource != null)
            musicSource.mute = muted;
    }

    /// <summary>
    /// Mutea o desmutea la capa de efectos de sonido.
    /// </summary>
    public void SetSfxMuted(bool muted)
    {
        sfxMuted = muted;

        if (sfxSource != null)
            sfxSource.mute = muted;
    }

    /// <summary>
    /// Alterna el mute de la capa de música (conectar directo a un Button).
    /// </summary>
    public void ToggleMusicMute() => SetMusicMuted(!musicMuted);

    /// <summary>
    /// Alterna el mute de la capa de efectos (conectar directo a un Button).
    /// </summary>
    public void ToggleSfxMute() => SetSfxMuted(!sfxMuted);
}
