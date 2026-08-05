using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>DifficultySelect シーンの入口。ボタンと難易度の対応、ハイスコアの表示を行う。</summary>
public sealed class DifficultySelectManager : MonoBehaviour
{
    [Serializable]
    private sealed class Choice
    {
        public GameDifficulty difficulty;
        public Button button;

        [Tooltip("その難易度のハイスコアを表示するテキスト（未設定でもエラーにはならない）")]
        public TextMeshProUGUI highScoreText;
    }

    [Header("【必須】")]
    [Tooltip("表示する難易度とボタンの対応。並び順は Scene 側の配置で決める。")]
    [SerializeField] private Choice[] choices = Array.Empty<Choice>();

    [Header("【チュートリアル確認ダイアログ】")]
    [Tooltip("イージー・ノーマル選択時に表示する確認ダイアログ（Hierarchy 上の UIPanel をセット）")]
    [SerializeField] private TutorialConfirmDialog confirmDialog;

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

            UpdateHighScoreDisplay(choice);
        }
    }

    private void UpdateHighScoreDisplay(Choice choice)
    {
        if (choice.highScoreText == null) return;

        var key = "HighScore_" + choice.difficulty.ToString();
        int highScore = PlayerPrefs.GetInt(key, 0);

        choice.highScoreText.text = $"BEST: {highScore:N0}";
    }

    private void Select(GameDifficulty difficulty)
    {
        AppServices.PlayConfirm();

        // Easy または Normal、かつ設定で確認ONになっている場合
        var isTargetDifficulty = (difficulty == GameDifficulty.Easy || difficulty == GameDifficulty.Normal);

        if (isTargetDifficulty && GameSettings.ShowTutorialConfirm && confirmDialog != null)
        {
            // ダイアログを表示してワンクッション挟む
            confirmDialog.Show(
                onYes: () =>
                {
                    // 「はい」: チュートリアルへ
                    GameFlowController.EnsureInstance().StartTutorial();
                },
                onNo: () =>
                {
                    // 「いいえ」: そのまま選択した難易度で本編開始
                    GameFlowController.EnsureInstance().SelectDifficulty(difficulty);
                }
            );
            return;
        }

        // 確認OFF、または Hard などの場合は即本編開始
        GameFlowController.EnsureInstance().SelectDifficulty(difficulty);
    }
}