using System.Collections; 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>BGM と SE の再生を一手に引き受ける常駐サービス。</summary>
public sealed class AudioManager : MonoBehaviour
{
    private const string CatalogResourcePath = "AudioCatalog";
    private const string BgmVolumeKey = "audio.bgmVolume";
    private const string SfxVolumeKey = "audio.sfxVolume";

    private static AudioManager instance;
    private static float bgmVolume = 1f;
    private static float sfxVolume = 1f;
    private static bool volumeLoaded;

    [SerializeField] private AudioCatalog catalog;
    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private float currentBgmClipVolume = 1f;

    private Coroutine ambientCoroutine;

    public static float BgmVolume
    {
        get
        {
            LoadVolumes();
            return bgmVolume;
        }
        set
        {
            LoadVolumes();
            bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
            if (instance != null)
            {
                instance.ApplyBgmVolume();
            }
        }
    }

    public static float SfxVolume
    {
        get
        {
            LoadVolumes();
            return sfxVolume;
        }
        set
        {
            LoadVolumes();
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        }
    }

    public static AudioManager EnsureInstance()
    {
        if (instance != null) return instance;
        var managerObject = new GameObject(nameof(AudioManager));
        return managerObject.AddComponent<AudioManager>();
    }

    public static void PlaySfx(AudioCue cue)
    {
        EnsureInstance().PlayOneShot(cue);
    }

    private static void LoadVolumes()
    {
        if (volumeLoaded) return;
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        volumeLoaded = true;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadVolumes();
        catalog ??= Resources.Load<AudioCatalog>(CatalogResourcePath);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        sfxSource = gameObject.AddComponent<AudioSource>();
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        PlaySceneBgm(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            instance = null;
        }
    }

    private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        PlaySceneBgm(nextScene.name);
    }

    private void PlaySceneBgm(string sceneName)
    {
        StopAmbient();
        PlaySceneJingle(sceneName);

        var cue = sceneName switch
        {
            GameFlowController.TitleSceneName => AudioCue.TitleBgm,
            GameFlowController.DifficultySelectSceneName => AudioCue.DifficultySelectBgm,
            GameFlowController.GameSceneName => AudioCue.GameBgm,
            GameFlowController.ClearSceneName => AudioCue.ClearBgm,
            GameFlowController.GameOverSceneName => AudioCue.GameOverBgm,
            _ => AudioCue.TitleBgm
        };

        if (sceneName == GameFlowController.GameSceneName)
        {
            StartAmbient();
        }

        if (catalog == null || !catalog.TryGet(cue, out var clip, out var volume))
        {
            if (sceneName == GameFlowController.GameSceneName)
            {
                StopBgm();
            }
            return;
        }

        // TitleBgm と DifficultySelectBgm に同じ AudioClip がセットされている場合、
        // 以下の処理により曲が最初からリセットされず、シームレスに流れる仕様となっています
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            currentBgmClipVolume = volume;
            ApplyBgmVolume();
            return;
        }

        bgmSource.clip = clip;
        currentBgmClipVolume = volume;
        ApplyBgmVolume();
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    private void StartAmbient()
    {
        if (catalog == null || catalog.AmbientCues == null || catalog.AmbientCues.Count == 0)
        {
            return;
        }

        ambientCoroutine = StartCoroutine(PlayAmbientRoutine());
    }

    private void StopAmbient()
    {
        if (ambientCoroutine != null)
        {
            StopCoroutine(ambientCoroutine);
            ambientCoroutine = null;
        }
    }

    private IEnumerator PlayAmbientRoutine()
    {
        while (true)
        {
            var minSec = catalog.AmbientMinIntervalSec;
            var maxSec = Mathf.Max(minSec, catalog.AmbientMaxIntervalSec);
            var waitTime = Random.Range(minSec, maxSec);

            yield return new WaitForSeconds(waitTime);

            var cues = catalog.AmbientCues;
            if (cues != null && cues.Count > 0)
            {
                var selectedCue = cues[Random.Range(0, cues.Count)];
                PlayOneShot(selectedCue);
            }
        }
    }

    private void PlaySceneJingle(string sceneName)
    {
        AudioListener.pause = false;

        if (sceneName == GameFlowController.ClearSceneName)
        {
            PlayOneShot(AudioCue.ClearJingle);

            if (catalog != null)
            {
                StartCoroutine(PlayDelayedSfx(AudioCue.ClearJingle2, catalog.ClearJingle2DelaySec));
            }
        }
        else if (sceneName == GameFlowController.GameOverSceneName)
        {
            PlayOneShot(AudioCue.GameOverJingle);
        }
    }

    /// <summary>指定した秒数（delaySec）だけ遅れて SE を再生する（ポーズ中も動く）</summary>
    private IEnumerator PlayDelayedSfx(AudioCue cue, float delaySec)
    {
        if (delaySec > 0f)
        {
            yield return new WaitForSecondsRealtime(delaySec);
        }

        PlayOneShot(cue);
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = currentBgmClipVolume * bgmVolume;
        }
    }

    private void PlayOneShot(AudioCue cue)
    {
        if (catalog != null && catalog.TryGet(cue, out var clip, out var volume))
        {
            sfxSource.PlayOneShot(clip, volume * sfxVolume);
        }
    }
}