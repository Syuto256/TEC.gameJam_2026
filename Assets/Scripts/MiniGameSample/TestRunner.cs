using UnityEngine;

public class TestRunner : MonoBehaviour
{
    [SerializeField] private RapidClickMiniGame miniGame;

    private void Start()
    {
        if (miniGame != null)
        {
            // ミニゲーム完了時の通知を受け取るイベント登録
            miniGame.OnCompleted += (success, reason) =>
            {
                if (success)
                {
                    Debug.Log($"<color=green>【クリア!】</color> 理由: {reason}");
                }
                else
                {
                    Debug.Log($"<color=red>【ゲームオーバー】</color> 理由: {reason}");
                }
            };

            // ミニゲームの初期化＆開始（難易度1、制限時間5秒）
            miniGame.Initialize(1, 5.0f);
        }
    }
}