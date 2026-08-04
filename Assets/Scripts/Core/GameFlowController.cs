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
        SceneManager.LoadScene(DifficultySelectSceneName);
    }

    public void SelectDifficulty(GameDifficulty difficulty)
    {
        SelectedDifficulty = difficulty;
        LastSessionResult = null;
        SceneManager.LoadScene(GameSceneName);
    }

    public void Retry()
    {
        LastSessionResult = null;
        SceneManager.LoadScene(GameSceneName);
    }

    public void PresentResult(GameSessionResult result)
    {
        LastSessionResult = result;
        SceneManager.LoadScene(result.EndState == GameEndState.Clear ? ClearSceneName : GameOverSceneName);
    }
}
