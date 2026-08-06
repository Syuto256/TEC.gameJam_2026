using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Overwork.MiniGames.RapidClick
{
    public sealed class RapidClickMiniGame : MiniGameBase, IPointerClickHandler
    {
        [Header("【表示先】")]
        [Tooltip("連打の進捗。")]
        [SerializeField] private TMP_Text progressText;

        // ★追加: 文字揺れエフェクトの参照
        [Tooltip("連打時に文字を揺らすエフェクト（未設定でも動作に影響しない）")]
        [SerializeField] private RapidMashTextEffect textEffect;

        [Tooltip("進捗の書式。{0} が現在の回数、{1} が必要回数。")]
        [SerializeField] private string progressFormat = "連打! {0} / {1}";

        [Header("【プレビュー】")]
        [Tooltip("押すたびに切り替える画像。仮素材を含め、同じ名前で本番素材へ差し替えられる。")]
        [SerializeField] private Image previewImage;

        [Tooltip("押すたびに切り替える画像の一覧。")]
        [SerializeField] private Sprite[] previewSprites = new Sprite[0];

        [Tooltip("プレビューのファイル名。画像の順番に対応させる。")]
        [SerializeField] private string[] previewFileNames = new string[0];

        [Tooltip("プレビューの解像度。画像の順番に対応させる。")]
        [SerializeField] private string[] previewResolutions = new string[0];

        [Tooltip("プレビューのファイル名の表示先。")]
        [SerializeField] private TMP_Text fileNameText;

        [Tooltip("プレビューの解像度の表示先。")]
        [SerializeField] private TMP_Text resolutionText;

        [Tooltip("プレビューの何枚目かの表示先。")]
        [SerializeField] private TMP_Text indexText;

        [Tooltip("残りクリック回数を大きく表示する先。")]
        [SerializeField] private TMP_Text remainingText;

        [Tooltip("プレビューの何枚目かの書式。{0} が番号、{1} が総数。")]
        [SerializeField] private string indexFormat = "{0} / {1}";

        [Tooltip("何回押すごとにプレビューを切り替えるか。1 なら毎回切り替える。")]
        [Min(1)] [SerializeField] private int switchEveryNClicks = 1;

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
            if (!SceneUiValidation.Require(this,
                    (nameof(progressText), progressText),
                    (nameof(previewImage), previewImage),
                    (nameof(fileNameText), fileNameText),
                    (nameof(resolutionText), resolutionText),
                    (nameof(indexText), indexText),
                    (nameof(remainingText), remainingText)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            if (previewSprites == null || previewSprites.Length == 0)
            {
                Debug.LogError(nameof(RapidClickMiniGame) + " (" + name + "): previewSprites が空です。", this);
                FinishGame(false, "NO PREVIEW CONFIGURED");
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

            // ★追加: 連打時にテキスト揺れ演出を実行
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
            if (progressText != null)
            {
                progressText.text = string.Format(progressFormat, clicks, requiredClicks);
            }

            if (remainingText != null)
            {
                remainingText.text = Mathf.Max(0, requiredClicks - clicks).ToString();
            }

            if (previewSprites == null || previewSprites.Length == 0)
            {
                return;
            }

            var switchCount = Mathf.Max(1, switchEveryNClicks);
            var previewIndex = Mathf.Clamp(clicks / switchCount, 0, int.MaxValue) % previewSprites.Length;
            if (previewImage != null)
            {
                previewImage.sprite = previewSprites[previewIndex];
            }

            if (fileNameText != null)
            {
                fileNameText.text = ValueAt(previewFileNames, previewIndex, "preview_" + (previewIndex + 1).ToString("00") + ".png");
            }

            if (resolutionText != null)
            {
                resolutionText.text = ValueAt(previewResolutions, previewIndex, "1024 × 1024");
            }

            if (indexText != null)
            {
                indexText.text = string.Format(indexFormat, previewIndex + 1, previewSprites.Length);
            }
        }

        private static string ValueAt(string[] values, int index, string fallback)
        {
            return values != null && index >= 0 && index < values.Length && !string.IsNullOrEmpty(values[index])
                ? values[index]
                : fallback;
        }
    }
}
