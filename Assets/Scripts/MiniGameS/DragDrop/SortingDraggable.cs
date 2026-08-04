using UnityEngine;
using UnityEngine.EventSystems;

namespace Overwork.MiniGames.DragDrop
{
    /// <summary>仕分けるカード 1 枚。掴んで動かし、離すと元の位置へ戻る。</summary>
    /// <remarks>
    /// 見た目・大きさ・初期位置は Prefab 上のこの GameObject で調整する。
    /// 掴んでいる間は兄弟の最後尾へ回して最前面に出す。Motonaga の試作では Canvas 直下へ
    /// 付け替えていたが、ミニゲームが破棄されたときにカードだけが残るため、
    /// ミニゲーム内で並び順だけを変える形にしている。
    /// </remarks>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class SortingDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("正解となる箱の categoryId。同じ文字列の SortingDropBox が正解になる。")]
        [SerializeField] private string categoryId;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        private Vector2 originalAnchoredPosition;
        private int originalSiblingIndex;

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
            originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
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
            ReturnToStart();
        }

        /// <summary>掴む前の位置と並び順へ戻す。正解で消えなかったカードは必ずここへ戻る。</summary>
        public void ReturnToStart()
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
            transform.SetSiblingIndex(originalSiblingIndex);
        }
    }
}
