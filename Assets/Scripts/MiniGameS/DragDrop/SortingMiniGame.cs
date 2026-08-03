using TMPro;
using UnityEngine;

namespace Overwork.MiniGames.DragDrop
{
    /// <summary>カードを正しい箱へ仕分けるミニゲーム。</summary>
    /// <remarks>
    /// 箱とカードは <c>Assets/Prefabs/MiniGames/SortingMiniGame.prefab</c> に実体として置く。
    /// 増減させる場合は Prefab に置いてから、このコンポーネントの配列へ追加する。
    /// 正解の対応は各カードと各箱の categoryId が一致するかどうかで決まる。
    /// </remarks>
    public sealed class SortingMiniGame : MiniGameBase
    {
        [Header("View")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Content")]
        [Tooltip("仕分け先の箱。Prefab 上に置いたものを並べる。")]
        [SerializeField] private SortingDropBox[] dropBoxes;

        [Tooltip("仕分けるカード。Prefab 上に置いたものを並べる。")]
        [SerializeField] private SortingDraggable[] cards;

        [Header("Tuning")]
        [Tooltip("何回入れ間違えたら失敗にするか。")]
        [Min(1)] [SerializeField] private int allowedMisses = 2;

        private int remaining;
        private int misses;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this, (nameof(statusText), statusText)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            if (dropBoxes == null || dropBoxes.Length == 0 || cards == null || cards.Length == 0)
            {
                Debug.LogError("SortingMiniGame (" + name + "): dropBoxes と cards を Prefab で設定してください。", this);
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            foreach (var box in dropBoxes)
            {
                if (box != null)
                {
                    box.Bind(this);
                }
            }

            remaining = cards.Length;
            misses = 0;
            Refresh();
        }

        /// <summary>カードが箱に落とされたときに <see cref="SortingDropBox"/> から呼ばれる。</summary>
        public void Drop(SortingDraggable card, bool matched)
        {
            if (!IsPlaying || card == null)
            {
                return;
            }

            if (matched)
            {
                Destroy(card.gameObject);
                remaining--;
                if (remaining <= 0)
                {
                    FinishGame(true, "COMPLETE");
                    return;
                }
            }
            else
            {
                misses++;
                if (misses >= allowedMisses)
                {
                    FinishGame(false, "MISSED");
                    return;
                }
            }

            Refresh();
        }

        protected override void OnUpdate(float deltaTime)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = "SORT THE MAIL\nMISS " + misses + " / " + allowedMisses
                + "   TIME " + Mathf.CeilToInt(TimeRemaining).ToString("00");
        }
    }
}
