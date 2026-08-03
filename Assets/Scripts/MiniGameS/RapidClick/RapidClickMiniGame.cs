using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Overwork.MiniGames.RapidClick
{
    /// <summary>規定回数まで連打するミニゲーム。</summary>
    /// <remarks>
    /// 画面の配置・配色・文字サイズは <c>Assets/Prefabs/MiniGames/RapidClickMiniGame.prefab</c> で調整する。
    /// クリックを受けるにはルートに Raycast Target が有効な Graphic（Image など）が必要である。
    /// </remarks>
    public sealed class RapidClickMiniGame : MiniGameBase, IPointerClickHandler
    {
        [Header("View")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Tuning")]
        [Tooltip("レベル 1 での必要クリック数。")]
        [Min(1)] [SerializeField] private int baseClicks = 12;

        [Tooltip("レベルが 1 上がるごとに増えるクリック数。")]
        [Min(0)] [SerializeField] private int clicksPerLevel = 4;

        private int requiredClicks;
        private int clicks;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this, (nameof(statusText), statusText)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            requiredClicks = baseClicks + (Mathf.Clamp(difficulty, 1, 4) - 1) * clicksPerLevel;
            clicks = 0;
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsPlaying)
            {
                return;
            }

            clicks++;
            if (clicks >= requiredClicks)
            {
                FinishGame(true, "COMPLETE");
                return;
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

            statusText.text = "CLICK!\n" + clicks + " / " + requiredClicks
                + "\nTIME " + Mathf.CeilToInt(TimeRemaining).ToString("00");
        }
    }
}
