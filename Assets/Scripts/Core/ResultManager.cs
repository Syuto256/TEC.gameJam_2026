using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Clear / GameOver シーンの入口。直前の結果の書き込み、ハイスコア保存とボタン接続を担当する。</summary>
/// <remarks>
/// 両シーンで同じクラスを使い、違いは Inspector の参照だけで表す。
/// Clear シーンでは <see cref="retryButton"/> を未設定にする。
/// </remarks>
public sealed class ResultManager : MonoBehaviour
{
    [Header("【必須】")]
    [Tooltip("直前のセッション結果を書き込む先。")]
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private Button backToDifficultyButton;

    [Header("【任意】")]
    [Tooltip("GameOver シーンにだけ置く。Clear シーンでは未設定でよい。")]
    [SerializeField] private Button retryButton;

    [Header("【表示する文言】")]
    [Tooltip("結果がまだ無いとき（このシーンから直接再生したとき）の表示。")]
    [SerializeField] private string emptyResultText = "Result will be shown after a session.";

    private void Start()
    {
        AppServices.Ensure();
        if (!SceneUiValidation.Require(this,
                (nameof(summaryText), summaryText),
                (nameof(backToDifficultyButton), backToDifficultyButton)))
        {
            return;
        }

        var flow = GameFlowController.EnsureInstance();
        var result = flow.LastSessionResult;

        // ★ ハイスコアのチェックと保存（新記録なら true が返る）
        bool isNewRecord = TrySaveHighScore(result);

        // ★ テキストの整形・表示（新記録表示含む）
        summaryText.text = Format(result, isNewRecord);

        backToDifficultyButton.onClick.AddListener(() =>
        {
            AppServices.PlayConfirm();
            flow.OpenDifficultySelect();
        });

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() =>
            {
                AppServices.PlayConfirm();
                flow.Retry();
            });
        }
    }

    /// <summary>★追加: ハイスコアをチェックし、更新していれば PlayerPrefs に保存する</summary>
    private bool TrySaveHighScore(GameSessionResult result)
    {
        if (result == null) return false;

        string key = "HighScore_" + result.Difficulty.ToString();
        int currentHighScore = PlayerPrefs.GetInt(key, 0);

        if (result.FinalScore > currentHighScore)
        {
            PlayerPrefs.SetInt(key, result.FinalScore);
            PlayerPrefs.Save();
            return true; // 新記録達成
        }

        return false;
    }

    private string Format(GameSessionResult result, bool isNewRecord)
    {
        if (result == null)
        {
            return emptyResultText;
        }

        // 保存されているハイスコアを取得（更新後の最新スコア）
        string key = "HighScore_" + result.Difficulty.ToString();
        int highScore = PlayerPrefs.GetInt(key, 0);

        string newRecordHeader = isNewRecord ? "★ NEW RECORD! ★\n\n" : "";

        return newRecordHeader
            + "Difficulty: " + result.Difficulty
            + "\nScore: " + result.FinalScore + $" (Best: {highScore})"
            + "\nHP: " + result.FinalHp;
    }
}