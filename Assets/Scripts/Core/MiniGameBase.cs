using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// すべてのミニゲームが継承する抽象基底クラス。
/// 各ミニゲーム担当者はこのクラスを継承して実装してください。
/// </summary>
/// <remarks>
/// 残り時間の表示（ゲージと数値）は全ミニゲーム共通の約束としてここが持つ。
/// Prefab に置いて Inspector で割り当てれば動き、置かなければ何もしない。
/// 各ミニゲームは自分固有の表示だけを持てばよい。
/// </remarks>
public abstract class MiniGameBase : MonoBehaviour
{
    [Header("共通 UI（任意）")]
    [Tooltip("残り時間ゲージ。Source Image を空にした単色 Image を割り当てる。残り時間は幅で表す。")]
    [SerializeField] private Image timeGaugeFill;

    [Tooltip("残り時間の数値表示。画面右上に置くのが共通の並びである。")]
    [SerializeField] private TMP_Text timeText;

    [Tooltip("残り時間の書式。{0} に残り秒数が入る。")]
    [SerializeField] private string timeFormat = "残り時間: {0:F1} 秒";

    [Tooltip("決着を窓の中で知らせる層。共通ウィンドウフレームが持っている。\n" +
             "未設定なら結果を出さず、従来どおり即座に閉じる。")]
    [SerializeField] private MiniGameResultView resultView;

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

    /// <summary>決着を見せるために窓を開いたままにする秒数。決着した後に読むこと。</summary>
    /// <remarks>
    /// <see cref="MainGameController"/> がこの秒数だけ、窓を閉じるのと担当中の印を落とすのを遅らせる。
    /// 結果の層を置いていないミニゲームでは 0 のままなので、従来どおり即座に閉じる。
    /// </remarks>
    public float ResultHoldSec { get; private set; }

    /// <summary>
    /// ミニゲームの初期化。生成時に MainGameController から呼び出される。
    /// </summary>
    public virtual void Initialize(int difficulty, float timeLimit)
    {
        Difficulty = difficulty;
        TimeLimit = timeLimit;
        TimeRemaining = timeLimit;
        IsPlaying = true;
        RefreshTimeUi();
    }

    protected virtual void Update()
    {
        if (!IsPlaying) return;

        // 共通タイマーカウントダウン
        TimeRemaining -= Time.deltaTime;
        RefreshTimeUi();

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
    /// 一手ごとの成否を鳴らす。QTE の 1 押し、連打の 1 回、タイピングの 1 文字などで呼ぶ。
    /// </summary>
    /// <remarks>
    /// クリア・失敗そのものの音は <see cref="MainGameController"/> がまとめて鳴らすため、
    /// 各ミニゲームが鳴らすのは途中経過だけでよい。
    /// 音源が未登録でも無音になるだけなので、素材が揃う前から呼んでよい。
    /// </remarks>
    protected static void PlayInputFeedback(bool hit)
    {
        AudioManager.PlaySfx(hit ? AudioCue.MiniGameInputHit : AudioCue.MiniGameInputMiss);
    }

    /// <summary>ミニゲーム固有の音を鳴らす。<see cref="AudioCue"/> へ種類を足してから使う。</summary>
    protected static void PlayCue(AudioCue cue)
    {
        AudioManager.PlaySfx(cue);
    }

    /// <summary>
    /// ミニゲームの終了を通知する関数。
    /// 成功時・失敗時に各ミニゲームクラスから呼び出すこと。
    /// </summary>
    protected void FinishGame(bool success, string reason = "")
    {
        if (!IsPlaying) return; // 二重発火防止ガード

        IsPlaying = false;

        // 通知より先に結果を出す。通知を受けた側がこの秒数を読んで窓の片付けを遅らせるためである。
        ResultHoldSec = resultView != null ? resultView.Show(success) : 0f;

        OnCompleted?.Invoke(success, reason);
    }

    /// <summary>共通の残り時間表示を更新する。</summary>
    private void RefreshTimeUi()
    {
        var remaining = Mathf.Max(0f, TimeRemaining);

        if (timeGaugeFill != null)
        {
            var ratio = TimeLimit <= 0f ? 0f : Mathf.Clamp01(remaining / TimeLimit);
            var fillRect = timeGaugeFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        if (timeText != null)
        {
            timeText.text = string.Format(timeFormat, remaining);
        }
    }

    protected virtual void OnDestroy()
    {
        // メモリリーク及び多重発火防止のため、破棄時にイベントハンドラを全解除
        OnCompleted = null;
    }
}
