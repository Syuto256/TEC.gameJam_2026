using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// カーソルが乗った時、または選択（Select）された時に
/// 左上・右下の「」枠線を表示するコンポーネント。
/// </summary>
public class SelectionCornerHighlight : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, 
    ISelectHandler, IDeselectHandler
{
    [Header("【表示する枠線UI】")]
    [Tooltip("「」が配置された子オブジェクト（FocusFrame）")]
    [SerializeField] private GameObject focusFrame;

    private void Awake()
    {
        SetFrameActive(false);
    }

    // マウスカーソルが乗った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetFrameActive(true);
    }

    // マウスカーソルが離れた時
    public void OnPointerExit(PointerEventData eventData)
    {
        SetFrameActive(false);
    }

    // キーボード/コントローラーでフォーカスされた時
    public void OnSelect(BaseEventData eventData)
    {
        SetFrameActive(true);
    }

    // フォーカスが外れた時
    public void OnDeselect(BaseEventData eventData)
    {
        SetFrameActive(false);
    }

    private void SetFrameActive(bool active)
    {
        if (focusFrame != null)
        {
            focusFrame.SetActive(active);
        }
    }

    private void OnDisable()
    {
        SetFrameActive(false);
    }
}