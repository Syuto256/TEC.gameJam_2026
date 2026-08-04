using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>一件の TaskInstance を表す吹き出し。左クリックは自力、右クリックは AI を依頼する。</summary>
/// <remarks>
/// 種別名・アイコン・難易度の星・配色は絵そのものに描き込まれているため、このクラスは
/// <c>MiniGameCatalog</c> から渡された絵を貼るだけで、文字は一切書かない。
/// 大きさと配置は Prefab と出現先が決める。
///
/// 残り時間は「吹き出し自体をゲージにする」方式で表す。無彩色の同じ絵を背面に敷き、
/// 手前の色つきを <c>Filled</c> で削ることで、色が残っている量が残り時間になる。
/// 吹き出しの横に別のゲージを置く方式は採らなかった。同時に 4 つ以上出るため、
/// 1 個あたりの追加要素がそのまま画面の混雑になるからである。
/// </remarks>
public sealed class TaskBubbleView : MonoBehaviour, IPointerClickHandler
{
    [Header("【必須】")]
    [Tooltip("残り時間で削られる、色つきの絵。\n" +
             "Image Type を Filled、Fill Method を Vertical、Fill Origin を Bottom にすること。")]
    [SerializeField] private Image colorFill;

    [Tooltip("背面に敷く同じ絵。削られた部分がここに見える。\n" +
             "Assets/Materials/UIGrayscale.mat を割り当てること。\n" +
             "Image の色に灰色を入れても無彩色にはならない（乗算のため色相が残る）。")]
    [SerializeField] private Image grayBase;

    [Header("【任意】")]
    [Tooltip("AI が作業しているあいだ重ねる暗幕。未設定なら暗くならない。\n" +
             "作業中の文字や円ゲージは、この上に別途載せる。")]
    [SerializeField] private Image workingDimmer;

    [Tooltip("AI が作業しているあいだの暗幕の濃さ。")]
    [Range(0f, 1f)] [SerializeField] private float workingDimAlpha = 0.55f;

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
    private Tween appearTween;
    private Tween warningTween;
    private bool exiting;

    public int TaskId => task?.Id ?? -1;

    /// <summary>Prefab の必須参照を検証する。生成前に一度だけ呼ぶ。</summary>
    public bool ValidateReferences()
    {
        return SceneUiValidation.Require(this, (nameof(colorFill), colorFill), (nameof(grayBase), grayBase));
    }

    /// <summary>表示するタスクと通知先を割り当てる。絵は MiniGameCatalog が種別とレベルから選んだものを渡す。</summary>
    public void Bind(MainGameController owner, TaskInstance instance, Sprite bubbleSprite)
    {
        controller = owner;
        task = instance;

        // 手前と背面は必ず同じ絵にする。ずれるとゲージの境目で別の絵が出てしまう。
        colorFill.sprite = bubbleSprite;
        grayBase.sprite = bubbleSprite;

        if (workingDimmer != null)
        {
            workingDimmer.enabled = false;
        }

        PlayAppear();
        Refresh();
    }

    /// <summary>出現時にふくらませる。</summary>
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

        var lifetimeRatio = task.InitialLifetimeSec <= 0f
            ? 0f
            : Mathf.Clamp01(task.RemainingLifetimeSec / task.InitialLifetimeSec);

        // 色が残っている量がそのまま残り時間になる。
        colorFill.fillAmount = lifetimeRatio;

        if (workingDimmer != null)
        {
            var working = task.State == TaskState.AiProcessing;
            workingDimmer.enabled = working;
            if (working)
            {
                var color = workingDimmer.color;
                color.a = workingDimAlpha;
                workingDimmer.color = color;
            }
        }

        UpdateWarning(lifetimeRatio);
    }

    /// <summary>残り寿命が少なくなったら脈動させ、戻ったら止める。状態が変わったときだけ触る。</summary>
    /// <remarks><see cref="Refresh"/> は毎フレーム呼ばれるため、ここで毎回トゥイーンを作り直さないこと。</remarks>
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
