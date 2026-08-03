using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AudioManager : MonoBehaviour
{
    private const string CatalogResourcePath = "AudioCatalog";
    private static AudioManager instance;

    [SerializeField] private AudioCatalog catalog;
    private AudioSource bgmSource;
    private AudioSource sfxSource;

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

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
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
            GameFlowController.GameSceneName => AudioCue.GameBgm,
            GameFlowController.ClearSceneName => AudioCue.ClearBgm,
            GameFlowController.GameOverSceneName => AudioCue.GameOverBgm,
            _ => AudioCue.TitleBgm
        };

        if (catalog == null || !catalog.TryGet(cue, out var clip, out var volume))
        {
            bgmSource.Stop();
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.Play();
    }

    private void PlayOneShot(AudioCue cue)
    {
        if (catalog != null && catalog.TryGet(cue, out var clip, out var volume))
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
