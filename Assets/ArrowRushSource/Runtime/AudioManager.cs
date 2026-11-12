using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

     [SerializeField] private AudioSource bgm;
    [SerializeField] private AudioSource primarySFX;
    [SerializeField] private AudioSource secondarySFX;
    [SerializeField] private AudioSource thirdSFX;

    [Header("SFX")]
    [SerializeField] private AudioClip[] sfxClip;

    private bool sfxEnabled = true;
    private bool bgmEnabled = true;
    private int nextSfxSourceIndex;
    private AudioSource[] sfxSources;
    private readonly Dictionary<string, AudioClip> sfxLookup = new Dictionary<string, AudioClip>();

    private const string VolumeKey = "Volume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string BGMKey = "BGMEnabled";
    private const string SFXKey = "SFXEnabled";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.MergeSfxClips(sfxClip);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        sfxSources = new[] { primarySFX, secondarySFX, thirdSFX };
        if (bgm == null) bgm = gameObject.AddComponent<AudioSource>();
        if (bgm.clip == null && sfxClip != null)
            foreach (AudioClip clip in sfxClip)
                if (clip != null && clip.name == "BGM") { bgm.clip = clip; break; }
        bgm.loop = true;
        for (int i = 0; i < sfxSources.Length; i++)
            if (sfxSources[i] == null) sfxSources[i] = gameObject.AddComponent<AudioSource>();
        RebuildSfxLookup();
        DontDestroyOnLoad(gameObject);
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        ApplySFXVolume(PlayerPrefs.GetFloat(SFXVolumeKey, 1f));
        bgmEnabled = PlayerPrefs.GetInt(BGMKey, 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt(SFXKey, 1) == 1;
        bgm.volume = PlayerPrefs.GetFloat("ArrowRush.MusicVolume", 1f);
    }

    private void Start()
    {
        ApplyBGMState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void MergeSfxClips(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return;

        var mergedClips = new System.Collections.Generic.List<AudioClip>();
        if (sfxClip != null)
            mergedClips.AddRange(sfxClip);

        foreach (AudioClip clip in clips)
        {
            if (clip == null)
                continue;

            bool alreadyIncluded = mergedClips.Exists(existing =>
                existing != null && existing.name == clip.name);

            if (!alreadyIncluded)
                mergedClips.Add(clip);
        }

        sfxClip = mergedClips.ToArray();
        RebuildSfxLookup();
    }

    private void RebuildSfxLookup()
    {
        sfxLookup.Clear();
        if (sfxClip == null)
            return;

        foreach (AudioClip clip in sfxClip)
        {
            if (clip != null && !sfxLookup.ContainsKey(clip.name))
                sfxLookup.Add(clip.name, clip);
        }
    }

    public float GetVolume()
    {
        return AudioListener.volume;
    }

    public bool IsBGMEnabled()
    {
        return bgmEnabled;
    }

    public bool IsSFXEnabled()
    {
        return sfxEnabled;
    }

    public bool MusicEnabled => bgmEnabled;
    public bool SfxEnabled => sfxEnabled;
    public float MusicVolume => bgm == null ? 1f : bgm.volume;
    public float SfxVolume => GetSFXVolume();
    public void SetMusicEnabled(bool enabled) => SetBGMEnabled(enabled);
    public void SetSfxEnabled(bool enabled) => SetSFXEnabled(enabled);
    public void SetMusicVolume(float value)
    {
        if (bgm != null) bgm.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("ArrowRush.MusicVolume", Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }
    public void SetSfxVolume(float value) => SetSFXVolume(value);
    public void Play(string clipName) => PlaySFX(clipName);

    public void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        AudioListener.volume = value;

        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    public float GetSFXVolume() => PlayerPrefs.GetFloat(SFXVolumeKey, 1f);

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);
        ApplySFXVolume(value);
        PlayerPrefs.SetFloat(SFXVolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ApplySFXVolume(float value)
    {
        if (sfxSources == null) return;
        foreach (AudioSource source in sfxSources)
            if (source != null) source.volume = value;
    }

    public void SetBGMEnabled(bool isOn)
    {
        bgmEnabled = isOn;
        PlayerPrefs.SetInt(BGMKey, isOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplyBGMState();
    }

    private void ApplyBGMState()
    {
        if (bgmEnabled)
            PlayBGM();
        else
            StopBGM();
    }

    public void SetSFXEnabled(bool isOn)
    {
        sfxEnabled = isOn;

        PlayerPrefs.SetInt(SFXKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void PlayBGM()
    {
        if (bgm != null && !bgm.isPlaying)
            bgm.Play();
    }

    public void StopBGM()
    {
        if (bgm != null && bgm.isPlaying)
            bgm.Stop();
    }

    public void PlaySFX(string clipName)
    {
        if (!sfxEnabled)
            return;

        if (string.IsNullOrEmpty(clipName) || !sfxLookup.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning("SFX not found: " + clipName);
            return;
        }

        for (int offset = 0; offset < sfxSources.Length; offset++)
        {
            int index = (nextSfxSourceIndex + offset) % sfxSources.Length;
            AudioSource source = sfxSources[index];
            if (source == null)
                continue;

            source.PlayOneShot(clip);
            nextSfxSourceIndex = (index + 1) % sfxSources.Length;
            return;
        }

        Debug.LogWarning("No SFX AudioSource is assigned.");
    }
}
