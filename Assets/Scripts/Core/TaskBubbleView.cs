using DG.Tweening;
using TMPro;
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
    [Tooltip("AI が作業しているあいだ重ねる暗幕。未設定なら暗くならない。")]
    [SerializeField] private Image workingDimmer;

    [Tooltip("AI が作業しているあいだの暗幕の濃さ。")]
    [Range(0f, 1f)] [SerializeField] private float workingDimAlpha = 0.55f;

    [Header("【AI に任せているときの表示】")]
    [Tooltip("作業中と結果をまとめた層。AI に任せているあいだだけ有効になる。未設定なら何も出ない。")]
    [SerializeField] private GameObject aiOverlay;

    [Tooltip("進み具合を表す円ゲージ。Image Type を Filled、Fill Method を Radial 360 にすること。")]
    [SerializeField] private Image aiGauge;

    [Tooltip("円ゲージの下に敷く輪。減っていない部分を見せる。無くてもよい。")]
    [SerializeField] private Image aiGaugeBase;

    [Tooltip("「AIが作業中」と結果を出す文字。色も状態に応じて変える。")]
    [SerializeField] private TextMeshProUGUI aiLabel;

    [Tooltip("失敗を表すバツ印。円ゲージと入れ替えて出す。無くてもよい。")]
    [SerializeField] private GameObject aiFailMark;

    [Tooltip("作業中に出す文字。")]
    [SerializeField] private string aiWorkingLabel = "AIが作業中";

    [Tooltip("成功したときに出す文字。")]
    [SerializeField] private string aiSuccessLabel = "Success";

    [Tooltip("失敗したときに出す文字。")]
    [SerializeField] private string aiFailureLabel = "FAILED";

    [Tooltip("作業中の色。")]
    [SerializeField] private Color aiWorkingColor = new Color(0.35f, 0.70f, 1f, 1f);

    [Tooltip("成功の色。")]
    [SerializeField] private Color aiSuccessColor = new Color(0.35f, 1f, 0.50f, 1f);

    [Tooltip("失敗の色。")]
    [SerializeField] private Color aiFailureColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Tooltip("結果を見せてから吹き出しが閉じ始めるまでの秒数。\n" +
             "0 にすると結果を見せずにすぐ閉じる。")]
    [Min(0f)] [SerializeField] private float aiResultHoldSec = 0.6f;

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

        if (aiOverlay != null)
        {
            aiOverlay.SetActive(false);
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

        UpdateAiWorking(task.State == TaskState.AiProcessing);
        UpdateWarning(lifetimeRatio);
    }

    /// <summary>AI が作業しているあいだ、暗幕と円ゲージを出して進み具合を見せる。</summary>
    /// <remarks>
    /// 残り秒数の数値は出さない。円ゲージと同じことを二重に言うためである。
    /// ゲージが読めるだけの長さは AI の処理時間しだいなので、1 秒を下回る設定にしないこと。
    /// </remarks>
    private void UpdateAiWorking(bool working)
    {
        if (workingDimmer != null)
        {
            workingDimmer.enabled = working;
            if (working)
            {
                var color = workingDimmer.color;
                color.a = workingDimAlpha;
                workingDimmer.color = color;
            }
        }

        if (aiOverlay == null)
        {
            return;
        }

        // 結果を見せている最中は、こちらから触らない。
        if (exiting)
        {
            return;
        }

        aiOverlay.SetActive(working);
        if (!working)
        {
            return;
        }

        if (aiFailMark != null)
        {
            aiFailMark.SetActive(false);
        }

        if (aiLabel != null)
        {
            aiLabel.text = aiWorkingLabel;
            aiLabel.color = aiWorkingColor;
        }

        if (aiGauge != null)
        {
            aiGauge.enabled = true;
            aiGauge.color = aiWorkingColor;

            // 満ちていくほど完了に近い。全体が 0 のときは満タン扱いにして、空のまま止めない。
            var total = task.AiTotalProcessSec;
            aiGauge.fillAmount = total <= 0f
                ? 1f
                : Mathf.Clamp01(1f - task.AiRemainingProcessSec / total);
        }

        if (aiGaugeBase != null)
        {
            aiGaugeBase.enabled = true;
        }
    }

    /// <summary>AI の結果を吹き出しの上に出す。閉じるのはこのあと。</summary>
    private void ShowAiResult(bool succeeded)
    {
        if (aiOverlay == null)
        {
            return;
        }

        aiOverlay.SetActive(true);
        var color = succeeded ? aiSuccessColor : aiFailureColor;

        if (aiLabel != null)
        {
            aiLabel.text = succeeded ? aiSuccessLabel : aiFailureLabel;
            aiLabel.color = color;
        }

        // 成功は円が満ちる。失敗は円を消してバツに差し替える。
        if (aiGauge != null)
        {
            aiGauge.enabled = succeeded;
            aiGauge.color = color;
            aiGauge.fillAmount = 1f;
        }

        if (aiFailMark != null)
        {
            aiFailMark.SetActive(!succeeded);
            foreach (var graphic in aiFailMark.GetComponentsInChildren<Graphic>(true))
            {
                graphic.color = color;
            }
        }
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

    /// <summary>決着を見せてから消滅アニメーションを再生し、終わったら自分を破棄する。</summary>
    /// <returns>
    /// 閉じ始めるまでに結果を見せる秒数。呼び出し側は、この時間だけ別の決着演出を遅らせて
    /// 表示が重ならないようにする。見せるものが無ければ 0。
    /// </returns>
    public float PlayExitAndDestroy(TaskResolution resolution)
    {
        if (exiting)
        {
            return 0f;
        }

        exiting = true;
        appearTween?.Kill();
        appearTween = null;
        warningTween?.Kill();
        warningTween = null;
        LeaveSlot();

        // AI の決着だけは、吹き出しの上に結果を出してから閉じる。
        var isAiResult = resolution == TaskResolution.AiSuccess || resolution == TaskResolution.AiFailure;
        var holdSec = isAiResult && aiOverlay != null ? aiResultHoldSec : 0f;
        if (holdSec > 0f)
        {
            ShowAiResult(resolution == TaskResolution.AiSuccess);
        }

        if (disappearDurationSec <= 0f && holdSec <= 0f)
        {
            Destroy(gameObject);
            return 0f;
        }

        transform.DOScale(0f, Mathf.Max(0.0001f, disappearDurationSec))
            .SetDelay(holdSec)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));

        return holdSec;
    }

    /// <summary>消え始めるときに枠から抜ける。見た目の位置はそのまま保つ。</summary>
    /// <remarks>
    /// 枠は「子がいなければ空き」で判定しているため、消えるあいだ居座ると、待機列から
    /// 繰り上がってきたタスクの置き場所が無くなる。枠の親へ移し、その場で消える。
    /// </remarks>
    private void LeaveSlot()
    {
        var slot = transform.parent;
        if (slot != null && slot.parent != null)
        {
            transform.SetParent(slot.parent, true);
        }
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
