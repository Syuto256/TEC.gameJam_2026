using UnityEngine;
using UnityEngine.EventSystems;

namespace Overwork.MiniGames.DragDrop
{
    /// <summary>仕分けるカード 1 枚。掴んで動かし、離すと元の位置へ戻る。</summary>
    /// <remarks>見た目・大きさ・初期位置は Prefab 上のこの GameObject で調整する。</remarks>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class SortingDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("正解となる箱の categoryId。同じ文字列の SortingDropBox が正解になる。")]
        [SerializeField] private string categoryId;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        private Vector2 originalAnchoredPosition;

        public bool Matches(string otherCategoryId) => categoryId == otherCategoryId;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
            // 掴んでいる間は自分がレイキャストを遮らないようにして、下の箱へ Drop を届かせる。
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var scale = parentCanvas != null && parentCanvas.scaleFactor > 0f ? parentCanvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }
}
