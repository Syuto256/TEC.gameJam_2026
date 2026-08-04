using System;
using DG.Tweening;
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

    [Tooltip("ポップアップが浮き上がって消えるまでの秒数。短いほど慌ただしくなる。")]
    [Min(0.05f)] [SerializeField] private float popupDurationSec = 0.8f;

    [Tooltip("浮き上がる動きの緩急。Linear は等速。OutCubic にすると最初が速く、最後がゆっくりになる。")]
    [SerializeField] private Ease popupEase = Ease.Linear;

    private Sequence scorePopupTween;
    private Color scorePopupBaseColor = Color.white;
    private Color comboPopupBaseColor = Color.white;

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

        // 演出で alpha を 0 まで落とすため、元の色をここで控えておく。
        // 演出中に次のポップアップが割り込んでも、褪せた色を引き継がないようにする。
        if (centerScorePopupText != null)
        {
            scorePopupBaseColor = centerScorePopupText.color;
        }

        if (comboPopupText != null)
        {
            comboPopupBaseColor = comboPopupText.color;
        }

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
    /// <summary>画面中央に獲得スコアとコンボ数のポップアップを浮かび上がらせる。</summary>
    /// <remarks>
    /// 演出中に呼ばれた場合は前の演出を打ち切り、位置と色を元に戻してから作り直す。
    /// 表示時間・緩急・移動量は Inspector で調整する。
    /// </remarks>
    public void ShowScorePopup(int addedScore, int comboCount)
    {
        if (!initialized || centerScorePopupText == null)
        {
            return;
        }

        scorePopupTween?.Kill();

        var showCombo = comboCount >= 2 && comboPopupText != null;

        centerScorePopupText.text = "+" + addedScore;
        ResetPopup(centerScorePopupText, popupOffset, scorePopupBaseColor);

        if (comboPopupText != null)
        {
            if (showCombo)
            {
                comboPopupText.text = comboCount + " COMBO!";
                ResetPopup(comboPopupText, comboPopupOffset, comboPopupBaseColor);
            }
            else
            {
                // 前回のコンボ表示が残っていることがあるため、伸びていないときは必ず消す。
                comboPopupText.gameObject.SetActive(false);
            }
        }

        scorePopupTween = DOTween.Sequence()
            .Join(MoveUp(centerScorePopupText, popupOffset))
            .Join(centerScorePopupText.DOFade(0f, popupDurationSec).SetEase(popupEase));

        if (showCombo)
        {
            scorePopupTween
                .Join(MoveUp(comboPopupText, comboPopupOffset))
                .Join(comboPopupText.DOFade(0f, popupDurationSec).SetEase(popupEase));
        }

        scorePopupTween.OnComplete(HidePopups);
    }

    private Tween MoveUp(TextMeshProUGUI target, Vector2 startPosition)
    {
        return target.rectTransform
            .DOAnchorPosY(startPosition.y + popupMoveDistance, popupDurationSec)
            .SetEase(popupEase);
    }

    private static void ResetPopup(TextMeshProUGUI target, Vector2 startPosition, Color baseColor)
    {
        target.rectTransform.anchoredPosition = startPosition;
        target.color = baseColor;
        target.gameObject.SetActive(true);
    }

    private void HidePopups()
    {
        if (centerScorePopupText != null)
        {
            centerScorePopupText.gameObject.SetActive(false);
            centerScorePopupText.rectTransform.anchoredPosition = popupOffset;
            centerScorePopupText.color = scorePopupBaseColor;
        }

        if (comboPopupText != null)
        {
            comboPopupText.gameObject.SetActive(false);
            comboPopupText.rectTransform.anchoredPosition = comboPopupOffset;
            comboPopupText.color = comboPopupBaseColor;
        }
    }

    private void OnDestroy()
    {
        scorePopupTween?.Kill();
        scorePopupTween = null;
    }

}
