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
    
    // ★追加: チュートリアルシーンの識別名（Build Settings のシーン名と一致させる）
    public const string TutorialSceneName = "Tutorial";

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

    /// <summary>タイトルから難易度選択へ。PC の蓋を開ける演出をはさむ。</summary>
    /// <remarks>
    /// **ここだけ暗転を使わない。** 蓋が開いて画面が起動するまでを続けて見せたいので、
    /// 途中で真っ暗にすると演出が切れる。演出が用意できない場合は暗転に戻す。
    /// </remarks>
    public void OpenDifficultySelect()
    {
        var lid = PcLidView.EnsureInstance();
        if (lid != null && lid.TryOpen(() => SceneManager.LoadScene(DifficultySelectSceneName)))
        {
            return;
        }

        Transition(DifficultySelectSceneName);
    }

    /// <summary>難易度を決めてゲームへ。間を置いてから PC の画面が点く演出をはさむ。</summary>
    /// <remarks>
    /// **難易度選択では PC の画面を消したままにしている。** 蓋はもう開いているので動かさず、
    /// 暗い画面のまま少し待ってから点ける。演出が用意できない場合は暗転に戻す。
    /// </remarks>
    public void SelectDifficulty(GameDifficulty difficulty)
    {
        SelectedDifficulty = difficulty;
        LastSessionResult = null;

        var lid = PcLidView.EnsureInstance();
        if (lid != null && lid.TryPowerOn(() => SceneManager.LoadScene(GameSceneName)))
        {
            return;
        }

        Transition(GameSceneName);
    }

    // ★追加: 難易度を指定してメインゲームを開始する（SelectDifficulty と同等）
    public void StartMainGame(GameDifficulty difficulty)
    {
        SelectDifficulty(difficulty);
    }

    // ★追加: チュートリアル画面へ遷移する
    public void StartTutorial()
    {
        LastSessionResult = null;
        Transition(TutorialSceneName);
    }

    public void Retry()
    {
        LastSessionResult = null;
        Transition(GameSceneName);
    }

    /// <summary>ゲームを終えて結果画面へ。PC の蓋を閉じる演出をはさむ。</summary>
    /// <remarks>
    /// クリアでもゲームオーバーでも同じ演出を使う。**どちらも「仕事が終わった」ことに変わりはなく、
    /// 良し悪しは結果画面の中身で伝える。** 蓋が閉じ切ってから次のシーンを読み込むため、
    /// 結果画面の背景は閉じた PC になっている。
    /// </remarks>
    public void PresentResult(GameSessionResult result)
    {
        LastSessionResult = result;
        var sceneName = result.EndState == GameEndState.Clear ? ClearSceneName : GameOverSceneName;

        var lid = PcLidView.EnsureInstance();
        if (lid != null && lid.TryClose(() => SceneManager.LoadScene(sceneName)))
        {
            return;
        }

        Transition(sceneName);
    }

    /// <summary>暗転をはさんでシーンを切り替える。暗幕が用意できない場合はそのまま切り替える。</summary>
    private void Transition(string sceneName)
    {
        var overlay = FadeOverlayView.EnsureInstance();
        if (overlay == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        overlay.TryRun(() => SceneManager.LoadScene(sceneName));
    }
}