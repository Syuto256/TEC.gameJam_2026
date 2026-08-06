using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Overwork.MiniGames.RapidClick
{
    public sealed class RapidClickMiniGame : MiniGameBase, IPointerClickHandler
    {
        [Header("【表示先】")]
        [Tooltip("連打の進捗の表示先。押すたびに揺れる。")]
        [SerializeField] private TMP_Text indexText;

        [Tooltip("連打の進捗の書式。{0} が現在の回数、{1} が必要回数。")]
        [SerializeField] private string indexFormat = "{0} / {1}";

        [Tooltip("残り時間の表示先。")]
        [SerializeField] private TMP_Text remainingText;

        [Tooltip("残り時間の書式。{0} に残り秒数が入る。")]
        [SerializeField] private string remainingFormat = "残り{0}秒";

        [Tooltip("連打時に文字を揺らすエフェクト（未設定でも動作に影響しない）")]
        [SerializeField] private RapidMashTextEffect textEffect;

        [Header("【難度の調整】")]
        [Tooltip("レベル 1 での必要クリック数。")]
        [Min(1)] [SerializeField] private int baseClicks = 12;

        [Tooltip("レベルが 1 上がるごとに増えるクリック数。")]
        [Min(0)] [SerializeField] private int clicksPerLevel = 4;

        [Tooltip("スペースキーでも 1 回として数える。")]
        [SerializeField] private bool acceptSpaceKey = true;

        private int requiredClicks;
        private int clicks;
        private int shownRemainingSec = -1;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this,
                    (nameof(indexText), indexText),
                    (nameof(remainingText), remainingText)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            // 揺らす対象は必ず IndexText にする。
            // Prefab 側の割り当てがどこを向いていても、ここで揃えておく。
            if (textEffect == null)
            {
                textEffect = indexText.GetComponent<RapidMashTextEffect>();
            }

            if (textEffect != null)
            {
                textEffect.SetTarget(indexText.rectTransform);
            }

            requiredClicks = baseClicks + (Mathf.Clamp(difficulty, 1, 4) - 1) * clicksPerLevel;
            clicks = 0;
            shownRemainingSec = -1;
            Refresh();
            RefreshRemaining();
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

            RefreshRemaining();
        }

        private void RegisterInput()
        {
            if (!IsPlaying)
            {
                return;
            }

            clicks++;
            PlayInputFeedback(true);

            if (textEffect != null)
            {
                textEffect.OnMash();
            }

            if (clicks >= requiredClicks)
            {
                Refresh();
                FinishGame(true, "COMPLETE");
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (indexText != null)
            {
                indexText.text = string.Format(indexFormat, Mathf.Min(clicks, requiredClicks), requiredClicks);
            }
        }

        /// <summary>残り秒数を表示する。秒が変わった時だけ書き換える。</summary>
        private void RefreshRemaining()
        {
            if (remainingText == null)
            {
                return;
            }

            var seconds = Mathf.Max(0, Mathf.CeilToInt(TimeRemaining));
            if (seconds == shownRemainingSec)
            {
                return;
            }

            shownRemainingSec = seconds;
            remainingText.text = string.Format(remainingFormat, seconds);
        }
    }
}
