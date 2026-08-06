using DG.Tweening;
using UnityEngine;

/// <summary>連打（マッシュ）時にテキストや UI を揺らすエフェクト</summary>
public sealed class RapidMashTextEffect : MonoBehaviour
{
    [Header("【揺れ（Position Shake）設定】")]
    [Tooltip("揺らす対象の RectTransform（未設定の場合は自分自身）")]
    [SerializeField] private RectTransform targetTransform;

    [Tooltip("1回の連打で揺れる時間（秒）")]
    [SerializeField] private float duration = 0.15f;

    [Tooltip("揺れの強さ（ピクセル数）")]
    [SerializeField] private float strength = 15f;

    [Tooltip("振動数（1秒間の揺れ回数）")]
    [SerializeField] private int vibrato = 20;

    [Header("【拡大・縮小（Punch Scale）設定】")]
    [Tooltip("連打時に一瞬「パンッ」と拡大させるか")]
    [SerializeField] private bool usePunchScale = true;

    [Tooltip("拡大の跳ね返り強度")]
    [SerializeField] private Vector3 punchScaleAmount = new Vector3(0.2f, 0.2f, 0f);

    private Tween currentShakeTween;
    private Tween currentScaleTween;
    private Vector3 initialScale;

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = GetComponent<RectTransform>();
        }

        if (targetTransform != null)
        {
            initialScale = targetTransform.localScale;
        }
    }

    /// <summary>揺らす対象を差し替える。Prefab の割り当てより後から呼んだものが優先される。</summary>
    public void SetTarget(RectTransform target)
    {
        if (target == null || target == targetTransform) return;

        // 差し替え前の対象を元の見た目に戻してから移る。
        currentShakeTween?.Kill();
        currentScaleTween?.Kill();
        if (targetTransform != null)
        {
            targetTransform.localScale = initialScale;
        }

        targetTransform = target;
        initialScale = target.localScale;
    }

    /// <summary>連打ボタンが押された時に呼び出す</summary>
    public void OnMash()
    {
        if (targetTransform == null) return;

        // 連打に対応するため、実行中の Tween を即時完了させて位置・スケールをリセット
        currentShakeTween?.Kill(true);
        currentScaleTween?.Kill(true);
        targetTransform.localScale = initialScale;

        // 位置のランダムシェイク
        currentShakeTween = targetTransform.DOShakePosition(duration, strength, vibrato)
            .SetUpdate(true); // ポーズ中等でも動かしたい場合は true

        // ポップ感を出すためのパンチスケール（縮小➔復帰）
        if (usePunchScale)
        {
            currentScaleTween = targetTransform.DOPunchScale(punchScaleAmount, duration, 10, 1f)
                .SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        currentShakeTween?.Kill();
        currentScaleTween?.Kill();
        if (targetTransform != null)
        {
            targetTransform.localScale = initialScale;
        }
    }
}