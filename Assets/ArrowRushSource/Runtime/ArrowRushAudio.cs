using UnityEngine;

/// <summary>Lightweight persistent audio service using the reference project's SFX assets.</summary>
public sealed class ArrowRushAudio : MonoBehaviour
{
    private static ArrowRushAudio instance;
    private AudioSource musicSource;
    private AudioSource[] effectSources;
    private int nextEffectSource;
    private bool musicEnabled = true;
    private bool sfxEnabled = true;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    public static ArrowRushAudio Instance => instance;
    public bool MusicEnabled => musicEnabled;
    public bool SfxEnabled => sfxEnabled;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        GameObject root = new GameObject("ArrowRush Audio");
        instance = root.AddComponent<ArrowRushAudio>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        musicEnabled = PlayerPrefs.GetInt("BGMEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
        musicVolume = PlayerPrefs.GetFloat("ArrowRush.MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("ArrowRush.SfxVolume", 1f);
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;
        musicSource.clip = Resources.Load<AudioClip>("BrickBreakoutPuzzle/SFX/BGM");
        effectSources = new AudioSource[3];
        for (int i = 0; i < effectSources.Length; i++)
        {
            effectSources[i] = gameObject.AddComponent<AudioSource>();
            effectSources[i].spatialBlend = 0f;
            effectSources[i].volume = sfxVolume;
        }
        ApplyMusic();
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        PlayerPrefs.SetInt("BGMEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusic();
    }

    public void SetSfxEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        PlayerPrefs.SetInt("SFXEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("ArrowRush.MusicVolume", musicVolume);
        PlayerPrefs.Save();
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("ArrowRush.SfxVolume", sfxVolume);
        PlayerPrefs.Save();
        if (effectSources == null) return;
        foreach (AudioSource source in effectSources)
            if (source != null) source.volume = sfxVolume;
    }

    public void Play(string clipName)
    {
        if (!sfxEnabled || effectSources == null) return;
        AudioClip clip = Resources.Load<AudioClip>("BrickBreakoutPuzzle/SFX/" + clipName);
        if (clip == null) return;
        effectSources[nextEffectSource].PlayOneShot(clip);
        nextEffectSource = (nextEffectSource + 1) % effectSources.Length;
    }

    private void ApplyMusic()
    {
        if (musicSource == null || musicSource.clip == null) return;
        if (musicEnabled && !musicSource.isPlaying) musicSource.Play();
        else if (!musicEnabled && musicSource.isPlaying) musicSource.Stop();
    }
}
