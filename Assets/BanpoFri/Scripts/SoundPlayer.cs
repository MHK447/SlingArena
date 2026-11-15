using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BanpoFri;
using UnityEngine.AddressableAssets;

public class SoundPlayer : SingletonScriptableObject<SoundPlayer>, ILoader
{
    [System.Serializable]
    public class SoundData
    {
        public string soundKey;
        public AudioClip audioData;
        [Range(0, 1)]
        public float Volume = 1;
    }
    [SerializeField]
    private List<SoundData> soundDatas = new List<SoundData>();
    private Dictionary<string, AudioClip> soundDataAudioDic = new Dictionary<string, AudioClip>();
    public List<SoundData> SoundDataList { get { return soundDatas; } }
    public Dictionary<string, SoundData> SoundDataDic = new Dictionary<string, SoundData>();

    private List<AudioSource> cachedSources = new List<AudioSource>();
    private List<AudioSource> cachedSpaceSources = new List<AudioSource>();
    private AudioSource BGMSource = null;
    private AudioClip recoveryBGMClip = null;
    private Transform Root = null;
    private float currentBGMVolume = -1f; // 현재 설정된 BGM 볼륨 저장
    private bool bgmStopped = false; // 전투 시작 등으로 BGM을 의도적으로 끈 상태. 광고/포커스 복귀 시 자동 재생을 막는다.

    public float BGM_VOLUME { get { return SoundDataDic["bgm"].Volume; } }

    private bool soundon = true;

    [SerializeField]
    private AudioClip TestAudioClip;

    [Range(0, 1)]
    public float TestEditorVolume = 1;

    private AudioSource EditorSource;

    public AudioClip GetSoundData(string key)
    {
        return key != null && soundDataAudioDic.TryGetValue(key, out var clip) ? clip : null;
    }

    private bool TryGetSound(string key, out SoundData sound)
    {
        sound = null;
        return key != null && SoundDataDic.TryGetValue(key, out sound) && sound.audioData != null;
    }

    public void SetRoot(Transform _root)
    {
        Root = _root;
    }

    public void PlaySound(string key)
    {
        if (!soundon || !TryGetSound(key, out var sound)) return;
        var audio = GetAudioSource();
        audio.volume = sound.Volume;
        audio.loop = false;
        audio.PlayOneShot(sound.audioData);
    }

    public void PlaySound(AudioSource source, AudioClip clip, float volume, float pitch = 1f)
    {
        if (!soundon || source == null || !source.isActiveAndEnabled || clip == null) return;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    public AudioSource PlayLoopSound(string key)
    {
        if (!soundon || !TryGetSound(key, out var sound)) return null;

        var audio = GetAudioSource();
        audio.volume = sound.Volume;
        audio.loop = true;
        audio.PlayOneShot(sound.audioData);

        return audio;
    }



    public void SpacePlaySound(string key, Transform root)
    {
        if (!soundon || !TryGetSound(key, out _)) return;

        var audio = GetSpaceAudioSource(root, PlaySoundAction, key);

        if (audio != null)
        {
            PlaySoundAction(audio, key);
        }
    }

    private void PlaySoundAction(AudioSource source, string key)
    {
        if (!soundon || !TryGetSound(key, out var sound)) return;
        source.loop = false;
        source.volume = sound.Volume;
        source.PlayOneShot(sound.audioData);
    }

    public void PlayBGM(string key, bool recovery = false)
    {
        if (!TryGetSound(key, out var sound)) return;
        if (BGMSource == null)
        {
            BGMSource = new GameObject("sound entity").AddComponent<AudioSource>();
            BGMSource.transform.SetParent(Root);
        }

        BGMSource.loop = true;
        BGMSource.volume = sound.Volume;
        currentBGMVolume = sound.Volume;
        BGMSource.mute = isBgmMuted;
        var clip = sound.audioData;
        if (recovery)
            recoveryBGMClip = clip;
        BGMSource.clip = clip;
        bgmStopped = false;
        BGMSource.Play();
    }

    // BGM 정지 — 전투 시작 시 메인 BGM 을 끄는 용도. (로비 복귀 시 PlayBGM/BgmOn 으로 다시 재생)
    public void StopBGM()
    {
        bgmStopped = true; // 의도적 정지 — 광고/포커스 복귀 시 자동 재생되지 않도록 표시.
        if (BGMSource == null) return;
        BGMSource.Stop();
    }

    public void RecoveryBGM()
    {
        if(BGMSource == null) return;

        BGMSource.clip = recoveryBGMClip;
        BGMSource.mute = isBgmMuted;
        bgmStopped = false;
        BGMSource.Play();
    }

    private bool isBgmMuted = false;

    public void BgmSwitch(bool value)
    {
        isBgmMuted = !value;

        if (BGMSource == null)
        {
            BGMSource = new GameObject("sound entity").AddComponent<AudioSource>();
            BGMSource.transform.SetParent(Root);
        }

        BGMSource.mute = isBgmMuted;
    }
    public void EffectSwitch(bool value)
    {
        soundon = value;
    }
    public void SetBGMVolume(float volume = -1)
    {
        if (volume == -1 && BGMSource != null)
        {
            BGMSource.volume = SoundDataDic["bgm"].Volume;
            currentBGMVolume = SoundDataDic["bgm"].Volume; // 현재 볼륨 저장
            return;
        }


        if (BGMSource != null)
        {
            BGMSource.volume = volume;
            currentBGMVolume = volume; // 현재 볼륨 저장
        }
    }

    public void RestartBGM()
    {
        if (BGMSource != null && BGMSource.clip != null)
        {
            bgmStopped = false;
            BGMSource.time = 0;
            BGMSource.Play();
        }
    }

    // 광고/포커스 손실 후 복귀 시 BGM이 멈춰있으면 이어서 재생.
    // 단, 전투 중(StopBGM으로 의도적으로 끈 상태)에는 다시 켜지 않는다.
    public void ResumeBGMIfStopped()
    {
        if (bgmStopped) return;
        if (BGMSource == null || BGMSource.clip == null) return;
        if (BGMSource.isPlaying) return;
        BGMSource.mute = isBgmMuted;
        BGMSource.Play();
    }


    private AudioSource GetAudioSource()
    {
        AudioSource audio = null;

        foreach (var source in cachedSources)
        {
            if(source == null) continue;

            if (!source.isPlaying)
            {
                audio = source;
                return audio;
            }
        }

        if (audio == null)
        {
            audio = new GameObject("sound entity").AddComponent<AudioSource>();
            cachedSources.Add(audio);
            audio.transform.SetParent(Root);
        }

        return audio;
    }

    private AudioSource GetSpaceAudioSource(Transform root, System.Action<AudioSource, string> AddAction = null, string key = "")
    {
        AudioSource audio = null;

        foreach (var source in cachedSpaceSources)
        {
            if (source == null)
                break;

            if (!source.isPlaying)
            {
                audio = source;
                audio.transform.SetParent(root);
                audio.transform.localPosition = Vector3.zero;

                audio.spatialBlend = 1f;
                audio.dopplerLevel = 0;
                return audio;
            }
        }


        if (audio == null)
        {
            Addressables.InstantiateAsync("SpaceSound").Completed += (handle) =>
            {
                audio = handle.Result.GetComponent<AudioSource>();
                cachedSpaceSources.Add(audio);
                audio.transform.SetParent(root);
                audio.transform.localPosition = Vector3.zero;
                audio.spatialBlend = 1f;
                audio.dopplerLevel = 0;
                AddAction?.Invoke(audio, key);
            };

        }

        return audio;
    }

    public void Init()
    {
        cachedSpaceSources.Clear();
        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
    }

    private void OnAudioConfigurationChanged(bool changed)
    {
        Debug.Log(changed ? "Device was changed" : "Reset was called");
        if (changed)
        {
            AudioConfiguration config = AudioSettings.GetConfiguration();
            config.dspBufferSize = 64;
            AudioSettings.Reset(config);
        }

        if (BGMSource == null || BGMSource.clip == null) return;
        if (currentBGMVolume >= 0) BGMSource.volume = currentBGMVolume;
        BGMSource.mute = isBgmMuted;
        if (!bgmStopped) BGMSource.Play();
    }

    private void OnDisable() => AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;

    public void Load()
    {
        cachedSources.Clear();
        cachedSpaceSources.Clear();
        soundDataAudioDic.Clear();
        SoundDataDic.Clear();
        foreach (var sd in soundDatas)
        {
            SoundDataDic.Add(sd.soundKey, sd);
            soundDataAudioDic.Add(sd.soundKey, sd.audioData);
        }
    }



    #region


    private AudioSource GetEditorAudioSource()
    {
        AudioSource audio = null;
        audio = new GameObject("sound entity").AddComponent<AudioSource>();
        return audio;
    }

    public void EditorPlaySound()
    {
        if (TestAudioClip != null)
        {
            if (EditorSource != null)
                Object.DestroyImmediate(EditorSource.gameObject);

            EditorSource = GetEditorAudioSource();
            EditorSource.volume = TestEditorVolume;
            EditorSource.loop = false;
            EditorSource.clip = TestAudioClip;
            EditorSource.PlayOneShot(TestAudioClip);
        }
        else
        {
            Debug.Log("사운드를 넣어주세요!!");
        }
    }




    public void EditorSoundVolume(float volume)
    {
        if (EditorSource != null)
        {
            EditorSource.volume = volume;
        }
    }


    public void EditorStopSound()
    {
        if (EditorSource != null)
        {
            EditorSource.Stop();

            Object.DestroyImmediate(EditorSource.gameObject);
        }
    }


    #endregion




}
