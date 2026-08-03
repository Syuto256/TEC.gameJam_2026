using UnityEngine;

/// <summary>共通ミニゲーム表示領域の表示状態と、Prefab の生成先だけを担当する。</summary>
public sealed class MiniGameHostView : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("ミニゲーム Prefab の生成先。Launcher はこの下の子だけを破棄する。")]
    [SerializeField] private RectTransform contentArea;

    [Header("Optional")]
    [Tooltip("表示・非表示を切り替える枝。未設定ならこの GameObject 自身を使う。")]
    [SerializeField] private GameObject root;

    private bool initialized;

    /// <summary>Launcher へ渡すミニゲームの親。</summary>
    public GameObject ContentRoot => contentArea != null ? contentArea.gameObject : null;

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
