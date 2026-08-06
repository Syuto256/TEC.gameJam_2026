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

    [Header("【チュートリアル】")]
    [Tooltip("チュートリアルを始めるボタン。難易度とは並列に扱わない補助導線。\n" +
             "未設定でも他の難易度は選べる（チュートリアルへ入れなくなるだけ）。")]
    [SerializeField] private Button tutorialButton;

    private void Start()
    {
        AppServices.Ensure();

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(SelectTutorial);
        }

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

    /// <summary>チュートリアルとして Game シーンを開く。</summary>
    /// <remarks>
    /// **難易度は現在の選択のままにする。** チュートリアルは難易度の 1 つではないため、
    /// ここで難易度を選び直さない。以前は Easy / Normal を選んだときに確認ダイアログを出して
    /// 分岐させていたが、遊ぶ前に問われても判断できないうえ、
    /// 「チュートリアルを見たい」ときに難易度を選ばされる作りになっていたため、
    /// 独立したボタンへ置き換えた。
    /// </remarks>
    private void SelectTutorial()
    {
        AppServices.PlayConfirm();
        GameFlowController.EnsureInstance().StartTutorial();
    }
}