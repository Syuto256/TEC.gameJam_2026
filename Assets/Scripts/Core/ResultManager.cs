using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Clear / GameOver シーンの入口。直前の結果の書き込みとボタン接続だけを担当する。</summary>
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
        summaryText.text = Format(flow.LastSessionResult);

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

    private string Format(GameSessionResult result)
    {
        if (result == null)
        {
            return emptyResultText;
        }

        return "Difficulty: " + result.Difficulty
            + "\nScore: " + result.FinalScore
            + "\nHP: " + result.FinalHp;
    }
}
