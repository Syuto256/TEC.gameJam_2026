using TMPro;
using UnityEngine;

/// <summary>
/// 任意の MiniGameBase を単体起動するデバッグ用ランナー。
/// 対象を差し替えることで、別のミニゲームにも流用できる。
/// </summary>
public sealed class MiniGameDebugRunner : MonoBehaviour
{
    [SerializeField] private MiniGameBase targetMiniGame;
    [SerializeField] [Min(1)] private int difficulty = 1;
    [SerializeField] [Min(0.1f)] private float timeLimit = 10f;
    [SerializeField] private bool startOnStart = true;
    [SerializeField] private TMP_Text resultText;

    private bool isSubscribed;

    private void Start()
    {
        if (startOnStart)
        {
            StartMiniGame();
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    [ContextMenu("Start Mini Game")]
    public void StartMiniGame()
    {
        if (targetMiniGame == null)
        {
            Debug.LogError($"[{nameof(MiniGameDebugRunner)}] 起動対象の MiniGameBase が設定されていません。", this);
            return;
        }

        Unsubscribe();
        targetMiniGame.OnCompleted += HandleCompleted;
        isSubscribed = true;
        SetResult("結果: 実行中");
        targetMiniGame.Initialize(difficulty, timeLimit);
    }

    private void HandleCompleted(bool success, string reason)
    {
        SetResult(success ? $"結果: 成功（{reason}）" : $"結果: 失敗（{reason}）");
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || targetMiniGame == null)
        {
            return;
        }

        targetMiniGame.OnCompleted -= HandleCompleted;
        isSubscribed = false;
    }

    private void SetResult(string result)
    {
        if (resultText != null)
        {
            resultText.text = result;
        }
    }
}
