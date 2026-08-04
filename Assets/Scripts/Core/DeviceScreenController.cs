using System;
using DG.Tweening;
using UnityEngine;

/// <summary>どのデバイス面を主作業画面として表示するかと、切替可否・切替演出を担当する。</summary>
/// <remarks>
/// 切替は「席が横一列に並んでいて、視点のほうが移動する」という見立てで作る。退場する面と入場する面を
/// 画面幅ぶんずらして固定したまま、2 枚まとめて横へ動かす。半透明で重ねないため、隙間も二重像も出ない。
/// 静止中の表示・非表示は従来どおり <c>alpha</c> で表す。座標を使うのは移動中だけである。
/// 非表示面を常に画面外へ置く作りにすると、「見えている面か」を <c>alpha</c> で判定している
/// 既存処理（操作可否・被弾の揺れ）が、画面外の面まで対象にしてしまう。
/// </remarks>
public sealed class DeviceScreenController : MonoBehaviour
{
    [Header("【切替演出】")]
    [Tooltip("画面が横に流れ切るまでの秒数。0 にすると演出なしで即座に切り替わる。")]
    [Min(0f)] [SerializeField] private float slideDurationSec = 0.28f;

    [Tooltip("横移動の緩急。OutQuint は初速が速く、最後にすっと止まる。")]
    [SerializeField] private Ease slideEase = Ease.OutQuint;

    [Tooltip("移動の中間で一瞬だけ拡大する倍率。1 にすると拡大しない。\n" +
             "1 未満にはできない。面は画面と同じ大きさのため、縮めると画面の端と\n" +
             "面の継ぎ目に隙間が空き、背後が覗いてしまう。")]
    [Min(1f)] [SerializeField] private float slidePeakScale = 1.03f;

    [Tooltip("スライドに重ねるモーション線の層。未設定なら線なしで切り替わる。")]
    [SerializeField] private SlideMotionLinesView motionLines;

    private DeviceWorkspaceView[] workspaces = Array.Empty<DeviceWorkspaceView>();
    private DeviceTabsView tabs;
    private bool switchEnabled = true;

    private Sequence slideSequence;
    private DeviceWorkspaceView slideFrom;
    private DeviceWorkspaceView slideTo;
    private float slideDirection;
    private float slideWidth;
    private float slideProgress;
    private float slideScale = 1f;

    public TaskSurface ActiveSurface { get; private set; } = TaskSurface.Pc;

    private bool IsSliding => slideSequence != null;

    public void Initialize(DeviceWorkspaceView[] deviceWorkspaces, DeviceTabsView deviceTabs)
    {
        workspaces = deviceWorkspaces ?? Array.Empty<DeviceWorkspaceView>();
        tabs = deviceTabs;

        if (tabs != null)
        {
            tabs.SurfaceRequested += Show;
        }

        // 線は演出の飾りであり、無くても切替は成立する。用意できなければ切り離す。
        if (motionLines != null && !motionLines.Initialize())
        {
            motionLines = null;
        }

        // 開始時は演出しない。
        ShowImmediate(TaskSurface.Pc);
    }

    public void SetSwitchEnabled(bool enabled)
    {
        switchEnabled = enabled;
        if (tabs != null)
        {
            // 移動中はここで有効に戻さない。移動が終わったときに改めて反映する。
            tabs.SetInteractable(enabled && !IsSliding);
        }
    }

    /// <summary>指定した面へ切り替える。条件がそろわない場合は演出なしで切り替える。</summary>
    public void Show(TaskSurface surface)
    {
        if (!switchEnabled || IsSliding || surface == ActiveSurface)
        {
            return;
        }

        var fromIndex = IndexOf(ActiveSurface);
        var toIndex = IndexOf(surface);
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
        {
            ShowImmediate(surface);
            return;
        }

        // 幅はレイアウト確定後でないと 0 になりうるため、ここで読む。
        var width = workspaces[toIndex].Width;
        if (slideDurationSec <= 0f || width <= 0f)
        {
            ShowImmediate(surface);
            return;
        }

        StartSlide(surface, workspaces[fromIndex], workspaces[toIndex], toIndex > fromIndex ? 1f : -1f, width);
    }

    /// <summary>席の並び順は <c>workspaces</c> の並び順とする。0 が左端。</summary>
    /// <remarks>面が 3 つ以上になっても、配列に足した位置だけで移動方向が決まる。</remarks>
    private int IndexOf(TaskSurface surface)
    {
        for (var i = 0; i < workspaces.Length; i++)
        {
            if (workspaces[i] != null && workspaces[i].Surface == surface)
            {
                return i;
            }
        }

        return -1;
    }

    private void StartSlide(
        TaskSurface surface, DeviceWorkspaceView from, DeviceWorkspaceView to, float direction, float width)
    {
        ActiveSurface = surface;
        slideFrom = from;
        slideTo = to;
        slideDirection = direction;
        slideWidth = width;
        slideProgress = 0f;
        slideScale = 1f;

        // 入場する面は最初から不透明にする。退場する面と隙間なく並べるためである。
        to.SetVisible(true);
        from.SetInteractionEnabled(false);
        to.SetInteractionEnabled(false);
        ApplySlideFrame();

        if (tabs != null)
        {
            tabs.SetSelected(surface);
            tabs.SetInteractable(false);
        }

        if (motionLines != null)
        {
            motionLines.Play(direction, slideDurationSec);
        }

        var half = slideDurationSec * 0.5f;
        slideSequence = DOTween.Sequence();
        slideSequence.Insert(0f, DOTween
            .To(() => slideProgress, value => { slideProgress = value; ApplySlideFrame(); }, 1f, slideDurationSec)
            .SetEase(slideEase));

        // 移動の勢いを見せる寄り。端が欠けないよう、拡大方向にしか動かさない。
        if (slidePeakScale > 1f && half > 0f)
        {
            slideSequence.Insert(0f, DOTween
                .To(() => slideScale, value => { slideScale = value; ApplySlideFrame(); }, slidePeakScale, half)
                .SetEase(Ease.OutQuad));
            slideSequence.Insert(half, DOTween
                .To(() => slideScale, value => { slideScale = value; ApplySlideFrame(); }, 1f, half)
                .SetEase(Ease.InQuad));
        }

        // ポーズボタンは移動中も押せる位置にある。実時間で動かさないと、
        // 途中で timeScale が 0 になったとき半端な位置で固まってしまう。
        slideSequence.SetUpdate(true).SetLink(gameObject).OnComplete(FinishSlide);
    }

    /// <summary>2 枚を画面幅ぶんずらしたまま、まとめて動かす。</summary>
    private void ApplySlideFrame()
    {
        var travel = -slideDirection * slideWidth * slideProgress;

        if (slideFrom != null)
        {
            slideFrom.ApplySlide(travel, slideScale);
        }

        if (slideTo != null)
        {
            slideTo.ApplySlide(travel + slideDirection * slideWidth, slideScale);
        }
    }

    private void FinishSlide()
    {
        slideSequence = null;

        if (slideFrom != null)
        {
            slideFrom.EndSlide();
            slideFrom.SetVisible(false);
        }

        if (slideTo != null)
        {
            slideTo.EndSlide();
            slideTo.SetVisible(true);
        }

        slideFrom = null;
        slideTo = null;

        // 切替禁止のまま移動が終わることがある。true 固定にはしない。
        if (tabs != null)
        {
            tabs.SetInteractable(switchEnabled);
        }
    }

    private void ShowImmediate(TaskSurface surface)
    {
        ActiveSurface = surface;
        foreach (var workspace in workspaces)
        {
            if (workspace == null)
            {
                continue;
            }

            workspace.EndSlide();
            workspace.SetVisible(workspace.Surface == surface);
        }

        if (tabs != null)
        {
            tabs.SetSelected(surface);
        }
    }

    private void OnDestroy()
    {
        slideSequence?.Kill();
        slideSequence = null;

        if (tabs != null)
        {
            tabs.SurfaceRequested -= Show;
        }
    }
}
