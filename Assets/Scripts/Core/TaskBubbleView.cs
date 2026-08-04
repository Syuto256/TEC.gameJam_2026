using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>一件の TaskInstance を表す吹き出し。左クリックは自力、右クリックは AI を依頼する。</summary>
/// <remarks>
/// 大きさ・配色・文字・配置は Prefab と `TaskSpawnArea` の Layout Group で調整する。
/// このクラスはタスクの状態を割り当てられた表示先へ書き込むだけで、座標もサイズも持たない。
/// </remarks>
public sealed class TaskBubbleView : MonoBehaviour, IPointerClickHandler
{
    [Header("【必須】")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI kindText;
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("【任意】")]
    [SerializeField] private TextMeshProUGUI timeText;
    [Tooltip("残り寿命を表すバー。Sprite を割り当て、Image Type を Filled にする。")]
    [SerializeField] private Image lifetimeGauge;
    [Tooltip("種別アイコン。MiniGameCatalog の icon が空なら非表示にする。")]
    [SerializeField] private Image kindIcon;

    [Header("【状態ごとの色】")]
    [SerializeField] private Color availableColor = new Color(0.16f, 0.42f, 0.66f, 1f);
    [SerializeField] private Color playerPlayingColor = new Color(0.56f, 0.24f, 0.59f, 1f);
    [SerializeField] private Color aiProcessingColor = new Color(0.70f, 0.46f, 0.10f, 1f);
    [SerializeField] private Color resolvedColor = new Color(0.24f, 0.24f, 0.24f, 1f);

    [Header("【状態ごとの表示文字】")]
    [SerializeField] private string availableLabel = "L: SELF / R: AI";
    [SerializeField] private string playerPlayingLabel = "SELF PLAYING";
    [SerializeField] private string aiProcessingLabel = "AI PROCESSING";
    [SerializeField] private string resolvedLabel = "RESOLVED";

    [Header("【出現・消滅の演出】")]
    [Tooltip("出現するとき、この倍率から等倍まで拡大する。1 にすると拡大せずそのまま出る。")]
    [Min(0f)] [SerializeField] private float appearFromScale = 0.7f;

    [Tooltip("出現アニメーションの秒数。0 にすると即座に出る。")]
    [Min(0f)] [SerializeField] private float appearDurationSec = 0.18f;

    [Tooltip("消滅アニメーションの秒数。この時間だけ吹き出しの消滅が遅れる。0 にすると即座に消える。")]
    [Min(0f)] [SerializeField] private float disappearDurationSec = 0.15f;

    [Header("【寿命警告】")]
    [Tooltip("残り寿命がこの割合を下回ったら、吹き出しが脈打って知らせる。0 にすると警告しない。")]
    [Range(0f, 1f)] [SerializeField] private float warningLifetimeRatio = 0.3f;

    [Tooltip("警告中にふくらむ倍率。大きいほど目立つ。")]
    [Min(1f)] [SerializeField] private float warningPulseScale = 1.08f;

    [Tooltip("脈動 1 往復にかかる秒数。短いほど焦らせる。")]
    [Min(0.05f)] [SerializeField] private float warningPulseCycleSec = 0.5f;

    private MainGameController controller;
    private TaskInstance task;
    private string kindLabel;
    private Tween appearTween;
    private Tween warningTween;
    private bool exiting;

    public int TaskId => task?.Id ?? -1;

    /// <summary>Prefab の必須参照を検証する。生成前に一度だけ呼ぶ。</summary>
    public bool ValidateReferences()
    {
        return SceneUiValidation.Require(this,
            (nameof(background), background), (nameof(kindText), kindText), (nameof(stateText), stateText));
    }

    /// <summary>表示するタスクと通知先を割り当てる。表示名とアイコンは MiniGameCatalog の登録内容を渡す。</summary>
    public void Bind(MainGameController owner, TaskInstance instance, string displayName, Sprite icon)
    {
        controller = owner;
        task = instance;
        kindLabel = displayName;

        if (kindIcon != null)
        {
            kindIcon.sprite = icon;
            kindIcon.enabled = icon != null;
        }

        PlayAppear();
        Refresh();
    }

    /// <summary>出現時にふくらませる。位置は動かさない。</summary>
    /// <remarks>
    /// 吹き出しの位置は <c>TaskSpawnArea</c> の Layout Group が driven property として支配しているため、
    /// 座標のトゥイーンは効かない。拡大縮小は Layout Group の管轄外なので、こちらで演出する。
    /// </remarks>
    private void PlayAppear()
    {
        if (appearDurationSec <= 0f || Mathf.Approximately(appearFromScale, 1f))
        {
            transform.localScale = Vector3.one;
            return;
        }

        transform.localScale = Vector3.one * appearFromScale;
        appearTween = transform.DOScale(1f, appearDurationSec).SetEase(Ease.OutBack);
    }

    public void Refresh()
    {
        if (task == null)
        {
            return;
        }

        if (kindText != null)
        {
            kindText.text = (string.IsNullOrWhiteSpace(kindLabel) ? task.Kind.ToString() : kindLabel) + "  Lv." + task.Level;
        }

        if (stateText != null)
        {
            stateText.text = task.State switch
            {
                TaskState.Available => availableLabel,
                TaskState.PlayerPlaying => playerPlayingLabel,
                TaskState.AiProcessing => aiProcessingLabel,
                _ => resolvedLabel
            };
        }

        if (timeText != null)
        {
            timeText.text = task.RemainingLifetimeSec.ToString("0.0");
        }

        var lifetimeRatio = task.InitialLifetimeSec <= 0f
            ? 0f
            : Mathf.Clamp01(task.RemainingLifetimeSec / task.InitialLifetimeSec);

        if (lifetimeGauge != null)
        {
            lifetimeGauge.fillAmount = lifetimeRatio;
        }

        UpdateWarning(lifetimeRatio);

        if (background != null)
        {
            background.color = task.State switch
            {
                TaskState.Available => availableColor,
                TaskState.PlayerPlaying => playerPlayingColor,
                TaskState.AiProcessing => aiProcessingColor,
                _ => resolvedColor
            };
        }
    }

    /// <summary>残り寿命が少なくなったら脈動させ、戻ったら止める。状態が変わったときだけ触る。</summary>
    /// <remarks>
    /// <see cref="Refresh"/> は毎フレーム呼ばれるため、ここで毎回トゥイーンを作り直さないこと。
    /// また <see cref="background"/> の色は毎フレーム状態色で上書きされるので、警告に色は使えない。
    /// </remarks>
    private void UpdateWarning(float lifetimeRatio)
    {
        if (exiting)
        {
            return;
        }

        var shouldWarn = warningLifetimeRatio > 0f
            && task.State == TaskState.Available
            && lifetimeRatio <= warningLifetimeRatio;

        if (shouldWarn == (warningTween != null))
        {
            return;
        }

        if (shouldWarn)
        {
            appearTween?.Kill();
            appearTween = null;
            transform.localScale = Vector3.one;
            warningTween = transform.DOScale(warningPulseScale, warningPulseCycleSec * 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            warningTween.Kill();
            warningTween = null;
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>消滅アニメーションを再生し、終わったら自分を破棄する。</summary>
    /// <remarks>
    /// 破棄が <see cref="disappearDurationSec"/> だけ遅れるため、その間は Layout Group の枠を占有し、
    /// 残りの吹き出しは詰め直されない。詰め直しを早めたい場合はこの秒数を短くする。
    /// </remarks>
    public void PlayExitAndDestroy()
    {
        if (exiting)
        {
            return;
        }

        exiting = true;
        appearTween?.Kill();
        appearTween = null;
        warningTween?.Kill();
        warningTween = null;

        if (disappearDurationSec <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.DOScale(0f, disappearDurationSec)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (task == null || controller == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            controller.TryAssignPlayer(task.Id);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            controller.TryAssignAi(task.Id);
        }
    }

    private void OnDestroy()
    {
        appearTween?.Kill();
        warningTween?.Kill();
        transform.DOKill();
    }
}
