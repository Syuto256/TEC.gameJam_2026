using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>選択難易度と終了結果をシーン間で保持し、ゲーム全体の遷移を管理する。</summary>
public sealed class GameFlowController : MonoBehaviour
{
    public const string TitleSceneName = "Title";
    public const string DifficultySelectSceneName = "DifficultySelect";
    public const string GameSceneName = "Game";
    public const string ClearSceneName = "Clear";
    public const string GameOverSceneName = "GameOver";

    public static GameFlowController Instance { get; private set; }

    public GameDifficulty SelectedDifficulty { get; private set; } = GameDifficulty.Easy;
    public GameSessionResult LastSessionResult { get; private set; }

    public static GameFlowController EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var gameObject = new GameObject(nameof(GameFlowController));
        return gameObject.AddComponent<GameFlowController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OpenDifficultySelect()
    {
        Transition(DifficultySelectSceneName);
    }

    public void SelectDifficulty(GameDifficulty difficulty)
    {
        SelectedDifficulty = difficulty;
        LastSessionResult = null;
        Transition(GameSceneName);
    }

    public void Retry()
    {
        LastSessionResult = null;
        Transition(GameSceneName);
    }

    public void PresentResult(GameSessionResult result)
    {
        LastSessionResult = result;
        Transition(result.EndState == GameEndState.Clear ? ClearSceneName : GameOverSceneName);
    }

    /// <summary>暗転をはさんでシーンを切り替える。暗幕が用意できない場合はそのまま切り替える。</summary>
    /// <remarks>
    /// 遷移中の再要求は暗幕側が弾く。暗幕は遷移のあいだ入力も遮るため、
    /// 暗転中にボタンを連打されても二重にシーンを読み込まない。
    /// </remarks>
    private void Transition(string sceneName)
    {
        var overlay = FadeOverlayView.EnsureInstance();
        if (overlay == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        // 受け付けられなかった場合は遷移中である。ここで直接読み込むと二重遷移になるため、何もしない。
        overlay.TryRun(() => SceneManager.LoadScene(sceneName));
    }
}
