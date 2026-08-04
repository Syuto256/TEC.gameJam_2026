using UnityEngine;
using UnityEngine.EventSystems;

namespace Overwork.MiniGames.DragDrop
{
    /// <summary>カードの受け皿 1 つ。落とされたカードの categoryId が一致するかを判定する。</summary>
    /// <remarks>見た目・大きさ・位置は Prefab 上のこの GameObject で調整する。</remarks>
    public sealed class SortingDropBox : MonoBehaviour, IDropHandler
    {
        [Tooltip("この箱が受け付けるカードの categoryId。")]
        [SerializeField] private string categoryId;

        private SortingMiniGame game;

        /// <summary>判定結果の通知先を割り当てる。<see cref="SortingMiniGame.Initialize"/> から呼ばれる。</summary>
        public void Bind(SortingMiniGame owner) => game = owner;

        public void OnDrop(PointerEventData eventData)
        {
            if (game == null || eventData.pointerDrag == null)
            {
                return;
            }

            var card = eventData.pointerDrag.GetComponent<SortingDraggable>();
            if (card != null)
            {
                game.Drop(card, card.Matches(categoryId));
            }
        }
    }
}
