using UnityEngine;
using UnityEngine.EventSystems;

// 左側の「素材」に付けるスクリプト
// 必要コンポーネント: Image（見た目）, CanvasGroup（Raycast制御用）
[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("この素材が対応する箱のID（記号）。DropBox側のacceptIdと一致させる")]
    public string matchId;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Vector2 startAnchoredPos;
    private Transform startParent;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startAnchoredPos = rectTransform.anchoredPosition;
        startParent = transform.parent;

        // ドラッグ中は自分自身がRaycastを塞がないようにする（Dropを検出するため）
        canvasGroup.blocksRaycasts = false;

        // 最前面に出すためCanvas直下に一時的に移動
        transform.SetParent(rootCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // DropBoxのOnDropで処理されなかった場合（外れた場合）は元の位置に戻す
        if (transform.parent == rootCanvas.transform)
        {
            ReturnToStart();
        }
    }

    public void ReturnToStart()
    {
        transform.SetParent(startParent, true);
        rectTransform.anchoredPosition = startAnchoredPos;
    }
}