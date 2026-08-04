using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Overwork.MiniGames.RapidClick
{
    /// <summary>規定回数まで連打するミニゲーム。</summary>
    /// <remarks>
    /// クリックに加えてスペースキーでも数える規則は Hiroto の試作
    /// （`Assets/Personal/Hiroto/rendagame/`）から取り込んでいる。
    /// 配置・配色・文字サイズは `Assets/Prefabs/MiniGames/RapidClickMiniGame.prefab` で調整する。
    /// クリックを受けるにはルートに Raycast Target が有効な Graphic（Image など）が必要である。
    /// </remarks>
    public sealed class RapidClickMiniGame : MiniGameBase, IPointerClickHandler
    {
        [Header("【表示先】")]
        [Tooltip("連打の進捗。")]
        [SerializeField] private TMP_Text progressText;

        [Tooltip("進捗の書式。{0} が現在の回数、{1} が必要回数。")]
        [SerializeField] private string progressFormat = "連打! {0} / {1}";

        [Header("【難度の調整】")]
        [Tooltip("レベル 1 での必要クリック数。")]
        [Min(1)] [SerializeField] private int baseClicks = 12;

        [Tooltip("レベルが 1 上がるごとに増えるクリック数。")]
        [Min(0)] [SerializeField] private int clicksPerLevel = 4;

        [Tooltip("スペースキーでも 1 回として数える。")]
        [SerializeField] private bool acceptSpaceKey = true;

        private int requiredClicks;
        private int clicks;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this, (nameof(progressText), progressText)))
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
            RegisterInput();
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (acceptSpaceKey && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                RegisterInput();
            }
        }

        private void RegisterInput()
        {
            if (!IsPlaying)
            {
                return;
            }

            clicks++;
            PlayInputFeedback(true);
            if (clicks >= requiredClicks)
            {
                FinishGame(true, "COMPLETE");
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (progressText != null)
            {
                progressText.text = string.Format(progressFormat, clicks, requiredClicks);
            }
        }
    }
}
