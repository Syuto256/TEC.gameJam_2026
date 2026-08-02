using System;
using UnityEngine;

/// <summary>
/// すべてのミニゲームが継承する抽象基底クラス。
/// 各ミニゲーム担当者はこのクラスを継承して実装してください。
/// </summary>
public abstract class MiniGameBase : MonoBehaviour
{
    /// <summary>
    /// ミニゲーム完了時に発火するイベント
    /// arg1: 成功フラグ (true=成功, false=失敗)
    /// arg2: 理由メッセージ ("TIME OUT", "MISSED" など)
    /// </summary>
    public event Action<bool, string> OnCompleted;

    public int Difficulty { get; private set; }
    public float TimeLimit { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// ミニゲームの初期化。生成時にGameManager等から呼び出される。
    /// </summary>
    public virtual void Initialize(int difficulty, float timeLimit)
    {
        Difficulty = difficulty;
        TimeLimit = timeLimit;
        TimeRemaining = timeLimit;
        IsPlaying = true;
    }

    protected virtual void Update()
    {
        if (!IsPlaying) return;

        // 共通タイマーカウントダウン
        TimeRemaining -= Time.deltaTime;

        // 各ミニゲーム固有の更新処理を呼び出す
        OnUpdate(Time.deltaTime);

        // 時間切れチェック
        if (TimeRemaining <= 0f)
        {
            FinishGame(false, "TIME OUT");
        }
    }

    /// <summary>
    /// [必須実装] 各ミニゲームの毎フレームの固有ロジックを書く場所。
    /// </summary>
    protected abstract void OnUpdate(float deltaTime);

    /// <summary>
    /// ミニゲームの終了を通知する関数。
    /// 成功時・失敗時に各ミニゲームクラスから呼び出すこと。
    /// </summary>
    protected void FinishGame(bool success, string reason = "")
    {
        if (!IsPlaying) return; // 二重発火防止ガード

        IsPlaying = false;
        OnCompleted?.Invoke(success, reason);
    }

    protected virtual void OnDestroy()
    {
        // メモリリーク及び多重発火防止のため、破棄時にイベントハンドラを全解除
        OnCompleted = null;
    }
}