using DG.Tweening;
using UnityEngine;

/// <summary>1 つのデバイス面の表示状態と、タスク吹き出しの生成先だけを担当する。</summary>
/// <remarks>
/// 非表示側も GameObject は有効なままにする。`SetActive(false)` で枝を止めると、
/// 吹き出しの演出や Coroutine が止まり、切替演出も作れなくなるためである。
/// </remarks>
[RequireComponent(typeof(CanvasGroup))]
public sealed class DeviceWorkspaceView : MonoBehaviour
{
    [Header("【必須】")]
    [Tooltip("この面が受け持つタスクの所属。各面で重複しないこと。")]
    [SerializeField] private TaskSurface surface;
    [SerializeField] private RectTransform leftSpawnArea;
    [SerializeField] private RectTransform rightSpawnArea;

    [Header("【任意】")]
    [Tooltip("未設定なら同じ GameObject の CanvasGroup を使う。")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("【被弾時の揺れ】")]
    [Tooltip("HPが減ったときにこの面が揺れる幅（ピクセル）。0 にすると揺れない。\n" +
             "背景は画面より上下左右 10px 大きく作ってあるため、10 を超えると画面端に隙間が見えることがある。")]
    [Min(0f)] [SerializeField] private float damageShakeStrength = 8f;

    [Tooltip("揺れが収まるまでの秒数。長くすると衝撃が重くなる。")]
    [Min(0f)] [SerializeField] private float damageShakeDurationSec = 0.25f;

    [Tooltip("揺れの細かさ。大きいほど細かく震える。")]
    [Min(1)] [SerializeField] private int damageShakeVibrato = 20;

    private RectTransform rectTransform;
    private Vector2 shakeOrigin;
    private Tween shakeTween;
    private bool initialized;

    public TaskSurface Surface => surface;

    /// <summary>参照を検証する。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this,
                (nameof(leftSpawnArea), leftSpawnArea), (nameof(rightSpawnArea), rightSpawnArea)))
        {
            return false;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            Debug.LogError("DeviceWorkspaceView (" + name + "): CanvasGroup が見つかりません。", this);
            return false;
        }

        rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            shakeOrigin = rectTransform.anchoredPosition;
        }

        initialized = true;
        return true;
    }

    /// <summary>被弾を表す短い揺れを再生する。表示されていない面は揺らさない。</summary>
    /// <remarks>
    /// 揺らすのはこの面だけで、HUD は動かさない。残り時間や HP の数値を読めなくしないためである。
    /// 揺れ幅・時間・細かさは Prefab Variant ごとに Inspector で調整する。
    /// </remarks>
    public void PlayDamageShake()
    {
        if (!initialized || rectTransform == null || damageShakeStrength <= 0f || damageShakeDurationSec <= 0f)
        {
            return;
        }

        if (canvasGroup == null || canvasGroup.alpha <= 0f)
        {
            return;
        }

        // 揺れが重なると原点がずれるため、前の揺れは終端まで進めてから始める。
        shakeTween?.Complete();
        shakeTween = rectTransform
            .DOShakeAnchorPos(damageShakeDurationSec, damageShakeStrength, damageShakeVibrato)
            .OnKill(() => rectTransform.anchoredPosition = shakeOrigin);
    }

    private void OnDestroy()
    {
        shakeTween?.Kill();
        shakeTween = null;
    }

    /// <summary>表示・非表示を切り替える。非表示側もタスクの寿命と演出は進み続ける。</summary>
    public void SetVisible(bool value)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = value ? 1f : 0f;
        canvasGroup.interactable = value;
        canvasGroup.blocksRaycasts = value;
    }

    /// <summary>表示（alpha）はそのまま維持し、タスクへの操作（クリック等）の可否だけを切り替える。</summary>
    public void SetInteractionEnabled(bool enabled)
    {
        if (canvasGroup == null)
        {
            return;
        }
    
        // 表示中（alpha > 0）の面だけ操作権限を変更する
        // （非表示の面が誤ってクリック可能になるのを防ぐため）
        if (canvasGroup.alpha > 0f)
        {
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }
    }

    /// <summary>吹き出しの少ない側の生成先を返す。同数なら左を使う。</summary>
    public RectTransform PickSpawnArea()
    {
        if (leftSpawnArea == null)
        {
            return rightSpawnArea;
        }

        if (rightSpawnArea == null)
        {
            return leftSpawnArea;
        }

        return rightSpawnArea.childCount < leftSpawnArea.childCount ? rightSpawnArea : leftSpawnArea;
    }
}
