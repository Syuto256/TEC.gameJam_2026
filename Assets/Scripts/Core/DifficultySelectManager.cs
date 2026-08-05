using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro を使う場合はこの using が必要（標準の Text なら不要）

/// <summary>DifficultySelect シーンの入口。ボタンと難易度の対応、ハイスコアの表示を行う。</summary>
/// <remarks>
/// 難易度を増減する場合は、<c>DifficultySelect.unity</c> にボタンを置いて
/// <see cref="choices"/> へ 1 行足す。コードは変更しない。
/// </remarks>
public sealed class DifficultySelectManager : MonoBehaviour
{
    [Serializable]
    private sealed class Choice
    {
        public GameDifficulty difficulty;
        public Button button;

        [Tooltip("その難易度のハイスコアを表示するテキスト（未設定でもエラーにはならない）")]
        public TextMeshProUGUI highScoreText; // 標準の Text を使う場合は public Text highScoreText;
    }

    [Header("【必須】")]
    [Tooltip("表示する難易度とボタンの対応。並び順は Scene 側の配置で決める。")]
    [SerializeField] private Choice[] choices = Array.Empty<Choice>();

    private void Start()
    {
        AppServices.Ensure();
        if (choices == null || choices.Length == 0)
        {
            Debug.LogError("DifficultySelectManager (" + name + "): choices が空です。", this);
            return;
        }

        foreach (var choice in choices)
        {
            if (choice == null || choice.button == null)
            {
                Debug.LogError("DifficultySelectManager (" + name + "): button が未設定の行があります。", this);
                continue;
            }

            var difficulty = choice.difficulty;
            choice.button.onClick.AddListener(() => Select(difficulty));

            // ★追加: ハイスコアを取得してテキストに表示する
            UpdateHighScoreDisplay(choice);
        }
    }

    /// <summary>保存されたハイスコアを読み込んで UI に反映する。</summary>
    /// <remarks>
    /// **数値だけを書く。** 「記録:」の見出しはカードの絵に描かれているため、
    /// ここで "BEST:" のような文字を足すと見出しが二重になる。
    /// </remarks>
    private void UpdateHighScoreDisplay(Choice choice)
    {
        if (choice.highScoreText == null) return;

        // 保存先のキーは HighScoreManager が持つ。ここで組み立てると、
        // 片方だけ変えたときに黙って 0 が出るようになる。
        var highScore = HighScoreManager.GetHighScore(choice.difficulty);

        // 例: "1,234" / 未プレイ時は "0"
        choice.highScoreText.text = highScore.ToString("N0");
    }

    private void Select(GameDifficulty difficulty)
    {
        AppServices.PlayConfirm();
        GameFlowController.EnsureInstance().SelectDifficulty(difficulty);
    }
}