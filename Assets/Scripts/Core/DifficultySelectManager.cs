using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>DifficultySelect シーンの入口。ボタンと難易度の対応だけを持つ。</summary>
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
    }

    [Header("Required")]
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
        }
    }

    private void Select(GameDifficulty difficulty)
    {
        AppServices.PlayConfirm();
        GameFlowController.EnsureInstance().SelectDifficulty(difficulty);
    }
}
