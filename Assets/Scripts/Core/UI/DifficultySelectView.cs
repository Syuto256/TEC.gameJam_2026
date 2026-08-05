using UnityEngine;
using UnityEngine.UI;

public sealed class DifficultySelectView : MonoBehaviour
{
    [Header("【難易度ボタン】")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;

    [Header("【確認ダイアログ】")]
    [SerializeField] private TutorialConfirmDialog confirmDialog;

    private void Start()
    {
        if (easyButton != null)
        {
            easyButton.onClick.AddListener(() => OnDifficultySelected(GameDifficulty.Easy));
        }

        if (normalButton != null)
        {
            normalButton.onClick.AddListener(() => OnDifficultySelected(GameDifficulty.Normal));
        }

        if (hardButton != null)
        {
            hardButton.onClick.AddListener(() => OnDifficultySelected(GameDifficulty.Hard));
        }
    }

    private void OnDifficultySelected(GameDifficulty difficulty)
    {
        var isTargetDifficulty = (difficulty == GameDifficulty.Easy || difficulty == GameDifficulty.Normal);
        
        // 原因特定用ログ
        Debug.Log($"[Check] TargetDiff: {isTargetDifficulty}, ShowConfirm: {GameSettings.ShowTutorialConfirm}, DialogExist: {confirmDialog != null}");

        if (isTargetDifficulty && GameSettings.ShowTutorialConfirm && confirmDialog != null)
        {
            Debug.Log("[Check] ダイアログを表示します！");
            confirmDialog.Show(
                onYes: () => GameFlowController.EnsureInstance().StartTutorial(),
                onNo: () => GameFlowController.EnsureInstance().StartMainGame(difficulty)
            );
            return;
        }

        Debug.Log("[Check] ダイアログを出さずに直接ゲームを開始します。");
        GameFlowController.EnsureInstance().StartMainGame(difficulty);
    }
}