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

        initialized = true;
        return true;
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
