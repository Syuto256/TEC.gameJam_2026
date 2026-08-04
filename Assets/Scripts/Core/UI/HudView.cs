using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD へ渡す 1 フレーム分の表示値。</summary>
public readonly struct HudSnapshot
{
    public HudSnapshot(int hp, int maxHp, int score, float remainingTimeSec, bool isEndless, GameDifficulty difficulty)
    {
        Hp = hp;
        MaxHp = maxHp;
        Score = score;
        RemainingTimeSec = remainingTimeSec;
        IsEndless = isEndless;
        Difficulty = difficulty;
    }

    public int Hp { get; }
    public int MaxHp { get; }
    public int Score { get; }
    public float RemainingTimeSec { get; }
    public bool IsEndless { get; }
    public GameDifficulty Difficulty { get; }
}

/// <summary>HP・残り時間・スコア・難易度の表示と、ポーズ要求だけを担当する。</summary>
public sealed class HudView : MonoBehaviour
{
    [Header("【必須】")]
    [Tooltip("Image Type を Filled にしたバー。fillAmount で HP 比率を表示する。")]
    [SerializeField] private Image hpBarFill;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button pauseButton;

    [Header("【任意】")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI difficultyText;

    [Header("【演出部品】")]
    [SerializeField] private TextMeshProUGUI centerScorePopupText;
    [SerializeField] private TextMeshProUGUI comboPopupText;

    [Header("【演出設定】")]
    [Tooltip("ポップアップの初期出現位置（画面中央からのオフセット）")]
    [SerializeField] private Vector2 popupOffset = new Vector2(0f, 100f); // 例: 中央より少し上

    [Tooltip("コンボポップアップの初期出現位置（画面中央からのオフセット）")]
    [SerializeField] private Vector2 comboPopupOffset = new Vector2(0f, 160f); // ★追加: スコアより少し上に配置

    [Tooltip("浮き上がる距離")]
    [SerializeField] private float popupMoveDistance = 50f;

    
    private Coroutine scorePopupCoroutine;
 
    private int lastHp = int.MinValue;
    private int lastScore = int.MinValue;
    private string lastTimeText;
    private bool initialized;

    /// <summary>ポーズボタンが押された。</summary>
    public event Action PauseRequested;

    /// <summary>参照を検証し、自身の入力を配線する。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this, (nameof(hpBarFill), hpBarFill), (nameof(timeText), timeText),
                (nameof(scoreText), scoreText), (nameof(pauseButton), pauseButton)))
        {
            return false;
        }

        pauseButton.onClick.AddListener(() => PauseRequested?.Invoke());
        initialized = true;
        return true;
    }

    public void Render(in HudSnapshot snapshot)
    {
        if (!initialized)
        {
            return;
        }

        if (snapshot.Hp != lastHp)
        {
            lastHp = snapshot.Hp;
            hpBarFill.fillAmount = snapshot.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)snapshot.Hp / snapshot.MaxHp);
            if (hpText != null)
            {
                hpText.text = snapshot.Hp.ToString();
            }
        }

        if (snapshot.Score != lastScore)
        {
            lastScore = snapshot.Score;
            scoreText.text = $"スコア: {snapshot.Score:N0}";
        }

        var time = FormatTime(snapshot.RemainingTimeSec, snapshot.IsEndless);
        if (time != lastTimeText)
        {
            lastTimeText = time;
            timeText.text = time;
        }

        if (difficultyText != null)
        {
            difficultyText.text = snapshot.Difficulty.ToString();
        }
    }

    private static string FormatTime(float remainingSec, bool isEndless)
    {
        if (isEndless)
        {
            return "--:--";
        }

    var totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSec));
        return (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
    }
    /// <summary>画面中央に獲得スコアとコンボ数のポップアップ演出を表示する</summary>
    public void ShowScorePopup(int addedScore, int comboCount)
    {
        if (centerScorePopupText == null) return;

        if (scorePopupCoroutine != null)
        {
            StopCoroutine(scorePopupCoroutine);
        }

        scorePopupCoroutine = StartCoroutine(AnimateScorePopup(addedScore, comboCount));
    }

    private System.Collections.IEnumerator AnimateScorePopup(int addedScore, int comboCount)
    {
        // 1. スコアテキストの準備
        centerScorePopupText.text = $"+{addedScore}";
        centerScorePopupText.gameObject.SetActive(true);
    
        // 2. コンボテキストの準備（2コンボ以上かつ参照がある場合のみ表示）
        var showCombo = comboCount >= 2 && comboPopupText != null;
        if (showCombo)
        {
            comboPopupText.text = $"{comboCount} COMBO!";
            comboPopupText.gameObject.SetActive(true);
        }
    
        var scoreRect = centerScorePopupText.rectTransform;
        var comboRect = showCombo ? comboPopupText.rectTransform : null;
    
        var scoreStartPos = popupOffset;
        var comboStartPos = comboPopupOffset;
    
        var duration = 0.8f;
        var elapsed = 0f;
    
        var scoreInitialColor = centerScorePopupText.color;
        var comboInitialColor = showCombo ? comboPopupText.color : Color.white;
    
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;
    
            // スコアの移動・フェードアウト
            scoreRect.anchoredPosition = scoreStartPos + new Vector2(0f, Mathf.Lerp(0f, popupMoveDistance, t));
            centerScorePopupText.color = new Color(scoreInitialColor.r, scoreInitialColor.g, scoreInitialColor.b, Mathf.Lerp(1f, 0f, t));
    
            // コンボの移動・フェードアウト
            if (showCombo)
            {
                comboRect.anchoredPosition = comboStartPos + new Vector2(0f, Mathf.Lerp(0f, popupMoveDistance, t));
                comboPopupText.color = new Color(comboInitialColor.r, comboInitialColor.g, comboInitialColor.b, Mathf.Lerp(1f, 0f, t));
            }
    
            yield return null;
        }
    
        // 後始末（スコア）
        centerScorePopupText.gameObject.SetActive(false);
        centerScorePopupText.color = scoreInitialColor;
        scoreRect.anchoredPosition = scoreStartPos;
    
        // 後始末（コンボ）
        if (showCombo)
        {
            comboPopupText.gameObject.SetActive(false);
            comboPopupText.color = comboInitialColor;
            comboRect.anchoredPosition = comboStartPos;
        }
    
        scorePopupCoroutine = null;
    }
}
