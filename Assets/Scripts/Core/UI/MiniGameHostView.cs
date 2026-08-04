using UnityEngine;

/// <summary>共通ミニゲーム表示領域の表示状態と、Prefab の生成先だけを担当する。</summary>
public sealed class MiniGameHostView : MonoBehaviour
{
    [Header("【必須】")]
    [Tooltip("ミニゲーム Prefab の生成先。この下の子だけを差し替える。")]
    [SerializeField] private RectTransform contentArea;

    [Header("【任意】")]
    [Tooltip("表示・非表示を切り替える枝。未設定ならこの GameObject 自身を使う。")]
    [SerializeField] private GameObject root;

    private bool initialized;

    public bool IsVisible => Root != null && Root.activeSelf;

    private GameObject Root => root != null ? root : gameObject;

    /// <summary>参照を検証する。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this, (nameof(contentArea), contentArea)))
        {
            return false;
        }

        initialized = true;
        return true;
    }

    public void Show()
    {
        SetVisible(true);
    }

    /// <summary>ミニゲーム Prefab を生成先へ差し替える。前の内容は破棄する。</summary>
    /// <remarks>
    /// 生成物の大きさ・位置は Prefab 自身の <see cref="RectTransform"/> が決める。ここでは上書きしない。
    /// Host いっぱいに広げたい場合は Prefab 側でアンカーを Stretch にする。
    /// </remarks>
    public MiniGameBase Spawn(MiniGameBase prefab)
    {
        if (prefab == null || contentArea == null)
        {
            return null;
        }

        ClearContent();
        return Instantiate(prefab, contentArea, false);
    }

    public void Hide()
    {
        ClearContent();
        SetVisible(false);
    }

    private void SetVisible(bool value)
    {
        var target = Root;
        if (target != null)
        {
            target.SetActive(value);
        }
    }

    private void ClearContent()
    {
        if (contentArea == null)
        {
            return;
        }

        for (var i = contentArea.childCount - 1; i >= 0; i--)
        {
            Destroy(contentArea.GetChild(i).gameObject);
        }
    }
}
