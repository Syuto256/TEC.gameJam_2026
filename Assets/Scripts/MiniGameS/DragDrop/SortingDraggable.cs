using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Overwork.MiniGames.DragDrop
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class SortingDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("正解となるファイルの種類。実行時に SortingMiniGame から設定する。")]
        [SerializeField] private SortingFileKind fileKind;

        [Header("【吸い寄せ（マグネット）設定】")]
        [Tooltip("この距離（ピクセル換算）以内に入ると吸い寄せを開始する")]
        [SerializeField] private float snapDistance = 0f;

        [Tooltip("吸い寄せる強さ（0〜1）。数値が大きいほどカチッと中央へ引っ張られる")]
        [Range(0f, 1f)]
        [SerializeField] private float magnetStrength = 0.00f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        private Vector2 originalAnchoredPosition;
        private int originalSiblingIndex;

        // シーン内の DropBox のキャッシュ
        private SortingDropBox[] dropBoxes;

        /// <summary>生成直後に種類と絵を決める。Prefab には仮の絵だけを置く。</summary>
        /// <remarks>ファイルの色は PNG が持っているため、ここでは染めない。</remarks>
        public void Setup(SortingFileKind kind, Sprite icon)
        {
            fileKind = kind;
            var image = GetComponent<Image>();
            if (image != null)
            {
                image.sprite = icon;
                image.color = Color.white;
            }
        }

        public bool Matches(SortingFileKind otherKind) => fileKind == otherKind;

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
            canvasGroup.blocksRaycasts = false;

            // ドラッグ開始時に有効な DropBox を検索して保持しておく
            dropBoxes = Object.FindObjectsByType<SortingDropBox>(FindObjectsSortMode.None);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var scale = parentCanvas != null && parentCanvas.scaleFactor > 0f ? parentCanvas.scaleFactor : 1f;
            
            // 通常の移動後の位置（anchoredPosition）を計算
            Vector2 nextPosition = rectTransform.anchoredPosition + (eventData.delta / scale);

            // 一時的に計算上の位置へ更新
            rectTransform.anchoredPosition = nextPosition;

            // 最も近い DropBox を探索
            SortingDropBox nearestBox = null;
            float minDistance = float.MaxValue;

            if (dropBoxes != null)
            {
                foreach (var box in dropBoxes)
                {
                    if (box == null || !box.gameObject.activeInHierarchy) continue;

                    // ワールド座標で距離を判定（UIの階層が異なっていても正確に判定可能）
                    float distance = Vector3.Distance(rectTransform.position, box.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestBox = box;
                    }
                }
            }

            // 指定距離以内なら、箱の中心へ吸い寄せる補正（Lerp）を行う
            if (nearestBox != null && minDistance <= snapDistance)
            {
                rectTransform.position = Vector3.Lerp(rectTransform.position, nearestBox.transform.position, magnetStrength);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            dropBoxes = null;
            ReturnToStart();
        }

        public void ReturnToStart()
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
            transform.SetSiblingIndex(originalSiblingIndex);
        }
    }
}
