using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>BGM と SE の再生を一手に引き受ける常駐サービス。</summary>
/// <remarks>
/// 鳴らす側は <see cref="PlaySfx"/> に種類を渡すだけでよい。実際のクリップと音量は
/// <c>Assets/Resources/AudioCatalog.asset</c> が持つ。未登録の種類は無音になるだけで、
/// エラーにも例外にもならないため、音源が揃う前でも安全に呼び出せる。
/// </remarks>
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

    /// <summary>BGM の音量（0〜1）。オプション画面から操作する。設定は保存される。</summary>
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

    /// <summary>SE の音量（0〜1）。次に鳴らす音から反映される。設定は保存される。</summary>
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
        var cue = sceneName switch
        {
            GameFlowController.TitleSceneName => AudioCue.TitleBgm,
            GameFlowController.DifficultySelectSceneName => AudioCue.DifficultySelectBgm,
            GameFlowController.GameSceneName => AudioCue.GameBgm,
            GameFlowController.ClearSceneName => AudioCue.ClearBgm,
            GameFlowController.GameOverSceneName => AudioCue.GameOverBgm,
            _ => AudioCue.TitleBgm
        };

        // クリップが未登録の間は、直前の BGM を鳴らし続ける。
        // 素材が順に届く途中で、シーンを移るたびに無音になるのを避けるためである。
        if (catalog == null || !catalog.TryGet(cue, out var clip, out var volume))
        {
            return;
        }

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
