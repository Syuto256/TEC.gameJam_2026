using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameTuningSettings settings;
    [SerializeField] private MiniGameBase currentMiniGame; // テスト用

    private int currentHP;
    private int currentScore;

    private void Start()
    {
        // パラメータの初期化
        if (settings != null)
        {
            currentHP = settings.maxHP;
        }

        // テスト用のミニゲームがあればイベントを登録して開始
        if (currentMiniGame != null)
        {
            // イベントの購読（通知が来たら OnMiniGameFinished を実行する予約）
            currentMiniGame.OnCompleted += OnMiniGameFinished;

            // ミニゲーム初期化（難易度1、制限時間4秒）
            currentMiniGame.Initialize(1, settings.miniGameTimes.rapidClick);
        }
    }

    /// <summary>
    /// ミニゲームから OnCompleted の通知が飛んできた時に呼ばれる関数
    /// </summary>
    private void OnMiniGameFinished(bool isSuccess, string reason)
    {
        if (isSuccess)
        {
            currentScore += settings.score.baseScoreDiff1;
            Debug.Log($"<color=green>【成功】</color> スコア加算! 現在のスコア: {currentScore} (理由: {reason})");
        }
        else
        {
            currentHP -= settings.damage.playerFail;
            Debug.Log($"<color=red>【失敗】</color> HPダメージ! 残りHP: {currentHP} (理由: {reason})");
        }

        // イベント解除（メモリリーク防止）
        if (currentMiniGame != null)
        {
            currentMiniGame.OnCompleted -= OnMiniGameFinished;
        }
    }
}