using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD へ渡す 1 フレーム分の表示値。</summary>
public readonly struct HudSnapshot
{
    public HudSnapshot(int hp, int maxHp, int score, float remainingTimeSec, bool isEndless, GameDifficulty difficulty,int comboCount)
    {
        Hp = hp;
        MaxHp = maxHp;
        Score = score;
        RemainingTimeSec = remainingTimeSec;
        IsEndless = isEndless;
        Difficulty = difficulty;
        ComboCount = comboCount;
    }

    public int Hp { get; }
    public int MaxHp { get; }
    public int Score { get; }
    public float RemainingTimeSec { get; }
    public bool IsEndless { get; }
    public GameDifficulty Difficulty { get; }
    public int ComboCount { get; }
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

    [Tooltip("減ったHPぶんを遅れて追いかける赤いバー。\n" +
             "HpBarFill と同じ大きさ・同じ Filled 設定にし、Hierarchy 上で HpBarFill より上（＝奥）に置く。\n" +
             "未設定なら赤ゲージは出ず、従来どおりの見た目になる。")]
    [SerializeField] private Image hpBarDamageFill;

    [SerializeField] private TextMeshProUGUI currentTaskNameText;
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

    [Header("【HPバー】")]
    [Tooltip("HP が減ったとき、バーが追いつくまでの秒数。\n" +
             "0 にすると従来どおり一瞬で減る。長くすると被弾が目で追いやすくなる。")]
    [Min(0f)] [SerializeField] private float hpBarDurationSec = 0.25f;

    [Tooltip("赤いバーが減り始めるまでの待ち時間（秒）。\n" +
             "この間は減ったぶんが赤いまま残る。長いほど「食らった量」が印象に残る。")]
    [Min(0f)] [SerializeField] private float hpDamageBarDelaySec = 0.35f;

    [Tooltip("赤いバーが現在HPに追いつくまでの秒数。0 にすると赤ゲージは残らない。")]
    [Min(0f)] [SerializeField] private float hpDamageBarDurationSec = 0.4f;

    [Header("【常時コンボ表示】")]
    [Tooltip("画面右上に常時表示するコンボテキスト")]
    [SerializeField] private TextMeshProUGUI persistentComboText;

    [Tooltip("コンボが増えたときの拡大・弾み具合")]
    [SerializeField] private Vector3 comboPunchScale = new Vector3(0.3f, 0.3f, 0f);

    [Tooltip("弾む時間（秒）")]
    [SerializeField] private float comboPunchDuration = 0.2f;


    [Header("【タイマー警告演出】")]
    [Tooltip("残り時間がこの秒数以下になったら点滅を開始する")]
    [SerializeField] private float timeWarningThresholdSec = 30f;

    [Tooltip("警告時のテキストカラー（通常色とこの色の間を点滅）")]
    [SerializeField] private Color timeWarningColor = Color.red;

    [Header("【被弾フラッシュ演出】")]
    [Tooltip("画面全体を覆う赤色の Image")]
    [SerializeField] private Image damageOverlay;

    [Tooltip("フラッシュが消えるまでの秒数")]
    [SerializeField] private float flashDurationSec = 0.3f;

    [Tooltip("フラッシュの最大不透明度（0〜1）")]
    [Range(0f, 1f)] [SerializeField] private float maxFlashAlpha = 0.5f;

    private Tween damageFlashTween;
    
    [Tooltip("点滅の1往復にかかる秒数")]
    [SerializeField] private float timeWarningBlinkDuration = 0.5f;

    private Tween timeWarningTween;
    private Color originalTimeTextColor = Color.white;
    private bool isTimeWarningActive;

    private int lastCombo = int.MinValue;
    private Tween comboPunchTween;

    private Tween hpBarTween;
    private Tween hpDamageBarTween;
    private Sequence scorePopupTween;
    private Color scorePopupBaseColor = Color.white;
    private Color comboPopupBaseColor = Color.white;

    private int lastHp = int.MinValue;
    private int lastScore = int.MinValue;
    private string lastTimeText;
    private bool initialized;

    // HudView.cs 内に追加
public GameObject ScoreTextObject => scoreText != null ? scoreText.gameObject : null;
// HudView.cs 内に追加（ScoreTextObject の隣などに）
// HudView.cs 内に追加（ScoreTextObject の隣などに）
public GameObject HpBarObject => hpBarFill != null ? hpBarFill.gameObject : null;
// HudView.cs 内に追加（ScoreTextObject や HpBarObject の隣などに）
public GameObject TimeTextObject => timeText != null ? timeText.gameObject : null;
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

        if (currentTaskNameText != null)
        {
            currentTaskNameText.gameObject.SetActive(false);
        }

        if (timeText != null)
        {
            originalTimeTextColor = timeText.color;
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
            var isFirstRender = lastHp == int.MinValue;
            var decreased = !isFirstRender && snapshot.Hp < lastHp;
            lastHp = snapshot.Hp;

            var targetFill = snapshot.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)snapshot.Hp / snapshot.MaxHp);
            hpBarTween?.Kill();

            // 開始直後は満タンから減る演出になってしまうため、初回だけ即座に反映する。
            if (isFirstRender || hpBarDurationSec <= 0f)
            {
                hpBarFill.fillAmount = targetFill;
            }
            else
            {
                hpBarTween = hpBarFill.DOFillAmount(targetFill, hpBarDurationSec).SetEase(Ease.OutQuad);
            }

            RenderDamageBar(targetFill, isFirstRender, decreased);

            if (hpText != null)
            {
                hpText.text = snapshot.Hp.ToString();
            }
        }

        if (snapshot.Score != lastScore)
        {
            lastScore = snapshot.Score;
            scoreText.text = snapshot.Score.ToString("N0");
        }

        var time = FormatTime(snapshot.RemainingTimeSec, snapshot.IsEndless);
        if (time != lastTimeText)
        {
            lastTimeText = time;
            timeText.text = time;
        }

        var isWarningTime = !snapshot.IsEndless && snapshot.RemainingTimeSec <= timeWarningThresholdSec && snapshot.RemainingTimeSec > 0f;
        if (isWarningTime && !isTimeWarningActive)
        {
            // 30秒以下になったら点滅開始
            isTimeWarningActive = true;
            timeWarningTween?.Kill();
            timeWarningTween = timeText
                .DOColor(timeWarningColor, timeWarningBlinkDuration)
                .SetLoops(-1, LoopType.Yoyo) // 往復無限ループ
                .SetEase(Ease.InOutSine);
        }
        else if (!isWarningTime && isTimeWarningActive)
        {
            // 30秒より多くなった（または0秒になった）ら点滅停止
            StopWarningBlink();
        }

        if (difficultyText != null)
        {
            difficultyText.text = snapshot.Difficulty.ToString();
        }

        if (snapshot.ComboCount != lastCombo)
        {
            var isFirstRender = lastCombo == int.MinValue;
            var increased = !isFirstRender && snapshot.ComboCount > lastCombo;
            lastCombo = snapshot.ComboCount;
            if (persistentComboText != null)
            {
                // 1コンボ以上なら表示、0コンボなら非表示
                if (snapshot.ComboCount > 0)
                {
                    persistentComboText.gameObject.SetActive(true);
                    persistentComboText.text = snapshot.ComboCount.ToString();
                    // コンボが増えた瞬間だけ拡大＆振動演出（DOPunchScale）
                    if (increased)
                    {
                        comboPunchTween?.Kill(true);
                        persistentComboText.transform.localScale = Vector3.one;
                        comboPunchTween = persistentComboText.transform
                            .DOPunchScale(comboPunchScale, comboPunchDuration, 10, 1f);
                    }
                }
                else
                {
                    persistentComboText.gameObject.SetActive(false);
                }
            }
        }
    }


    
    /// <summary>減ったHPぶんを赤いまま残し、少し待ってから現在HPまで追いつかせる。</summary>
    /// <remarks>
    /// 赤いバーは常に現在HPのバー以上の値を保つ。減ったときだけ遅らせ、それ以外は即座に合わせるためである。
    /// 追いつく途中でさらに被弾した場合は、そのときの位置から新しい目標へ引き直す。
    /// </remarks>
    private void RenderDamageBar(float targetFill, bool isFirstRender, bool decreased)
    {
        if (hpBarDamageFill == null)
        {
            return;
        }

        hpDamageBarTween?.Kill();

        // 初回と回復時は残さない。赤いバーが現在HPより少ない状態を作らないため。
        if (isFirstRender || !decreased || hpDamageBarDurationSec <= 0f)
        {
            hpBarDamageFill.fillAmount = targetFill;
            return;
        }

        hpDamageBarTween = hpBarDamageFill
            .DOFillAmount(targetFill, hpDamageBarDurationSec)
            .SetDelay(hpDamageBarDelaySec)
            .SetEase(Ease.InQuad);
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
    private void StopWarningBlink()
    {
        isTimeWarningActive = false;
        timeWarningTween?.Kill();
        timeWarningTween = null;

        if (timeText != null)
        {
            timeText.color = originalTimeTextColor;
        }
    }
    private void OnDestroy()
    {
        scorePopupTween?.Kill();
        scorePopupTween = null;
        hpBarTween?.Kill();
        hpBarTween = null;
        hpDamageBarTween?.Kill();
        hpDamageBarTween = null;
        comboPunchTween?.Kill();
        comboPunchTween = null;
        StopWarningBlink();
        damageFlashTween?.Kill();
    }

    /// <summary>★追加: 現在プレイ中のタスク名を表示する</summary>
    public void ShowCurrentTaskName(string taskName)
    {
        if (currentTaskNameText != null)
        {
            currentTaskNameText.text = taskName;
            currentTaskNameText.gameObject.SetActive(true);
        }
    }

    /// <summary>★追加: タスク名の表示を消す</summary>
    public void HideCurrentTaskName()
    {
        if (currentTaskNameText != null)
        {
            currentTaskNameText.gameObject.SetActive(false);
        }
    }

    /// <summary>被弾時に画面を一瞬赤くフラッシュさせる</summary>
    public void PlayDamageFlash()
    {
        if (damageOverlay == null) return;

        damageFlashTween?.Kill(true);

        // 一瞬で指定のAlpha値まで上げてから、透明(0)へフェードアウトさせる
        var color = damageOverlay.color;
        color.a = maxFlashAlpha;
        damageOverlay.color = color;

        damageFlashTween = damageOverlay.DOFade(0f, flashDurationSec).SetEase(Ease.OutQuad);
    }

    /// <summary>HUD全体（HPバー・タイマー・スコア等）の表示/非表示を一括切り替え</summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

}
