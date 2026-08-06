using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Overwork.MiniGames.DragDrop
{
    /// <summary>ファイルを受け入れるフォルダ 1 つ。</summary>
    /// <remarks>見た目・大きさ・位置は Prefab 上のこの GameObject で調整する。</remarks>
    public sealed class SortingDropBox : MonoBehaviour, IDropHandler
    {
        [Tooltip("このフォルダが受け付けるファイルの種類。実行時に SortingMiniGame から設定する。")]
        [SerializeField] private SortingFileKind fileKind;

        [Header("【表示先】")]
        [SerializeField] private Image folderImage;
        [SerializeField] private TMP_Text labelText;

        private SortingMiniGame game;

        /// <summary>判定結果の通知先を割り当てる。<see cref="SortingMiniGame.Initialize"/> から呼ばれる。</summary>
        public void Bind(SortingMiniGame owner) => game = owner;

        /// <summary>生成直後にフォルダの種類・絵・色・ラベルを設定する。</summary>
        public void Setup(SortingFileKind kind, Sprite icon, Color tint, string label)
        {
            fileKind = kind;
            if (folderImage != null)
            {
                folderImage.sprite = icon;
                folderImage.color = tint;
            }

            if (labelText != null)
            {
                labelText.text = label;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (game == null || eventData.pointerDrag == null)
            {
                return;
            }

            var card = eventData.pointerDrag.GetComponent<SortingDraggable>();
            if (card != null)
            {
                game.Drop(card, card.Matches(fileKind));
            }
        }
    }
}
