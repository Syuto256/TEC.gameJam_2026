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
        [System.Serializable]
        public sealed class SortingLevelLayout
        {
            [Range(1, 4)] public int level = 1;
            [Tooltip("このレベルで有効にする、箱とカードを含む Prefab 上のレイアウトルート。")]
            public GameObject layoutRoot;
            [Tooltip("このレベルで仕分け先として使う箱。")]
            public SortingDropBox[] dropBoxes;
            [Tooltip("このレベルで仕分けるカード。")]
            public SortingDraggable[] cards;
            [Tooltip("このレベルで失敗になるまでの誤配置数。")]
            [Min(1)] public int allowedMisses = 2;
        }

        [Header("【表示先】")]
        [Tooltip("ミス数。画面左上に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text missText;

        [Tooltip("ミス数の書式。{0} が現在のミス、{1} が上限。")]
        [SerializeField] private string missFormat = "ミス: {0} / {1}";

        [Header("【レベル別の配置】")]
        [Tooltip("レベル別の箱・カード・配置・許容ミス。各 Layout は Prefab 上に実体として置く。")]
        [SerializeField] private SortingLevelLayout[] levelLayouts;

        private SortingLevelLayout activeLayout;
        private int remaining;
        private int misses;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this, (nameof(missText), missText)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            activeLayout = FindLayout(difficulty);
            if (!TryActivateLayout(activeLayout))
            {
                Debug.LogError("SortingMiniGame (" + name + "): Lv." + difficulty
                    + " の Layout と箱・カードを Prefab で設定してください。", this);
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            foreach (var box in activeLayout.dropBoxes)
            {
                if (box != null)
                {
                    box.Bind(this);
                }
            }

            remaining = activeLayout.cards.Length;
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

            PlayInputFeedback(matched);

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
                if (misses >= activeLayout.allowedMisses)
                {
                    FinishGame(false, "MISSED");
                    return;
                }
            }

            Refresh();
        }

        protected override void OnUpdate(float deltaTime)
        {
        }

        private void Refresh()
        {
            if (missText != null)
            {
                missText.text = string.Format(missFormat, misses, activeLayout.allowedMisses);
            }
        }

        private SortingLevelLayout FindLayout(int difficulty)
        {
            if (levelLayouts == null)
            {
                return null;
            }

            var level = Mathf.Clamp(difficulty, 1, 4);
            foreach (var layout in levelLayouts)
            {
                if (layout != null && layout.level == level)
                {
                    return layout;
                }
            }

            return null;
        }

        private bool TryActivateLayout(SortingLevelLayout selectedLayout)
        {
            if (levelLayouts != null)
            {
                foreach (var layout in levelLayouts)
                {
                    if (layout?.layoutRoot != null)
                    {
                        layout.layoutRoot.SetActive(layout == selectedLayout);
                    }
                }
            }

            if (selectedLayout?.layoutRoot == null || selectedLayout.dropBoxes == null || selectedLayout.dropBoxes.Length == 0
                || selectedLayout.cards == null || selectedLayout.cards.Length == 0)
            {
                return false;
            }

            foreach (var box in selectedLayout.dropBoxes)
            {
                if (box == null)
                {
                    return false;
                }
            }

            foreach (var card in selectedLayout.cards)
            {
                if (card == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
