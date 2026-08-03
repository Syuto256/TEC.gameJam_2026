using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overwork.MiniGames.Typing
{
    /// <summary>Keyboard.onTextInput でローマ字入力を受ける共有タイピングミニゲーム。</summary>
    /// <remarks>
    /// 画面の配置・配色・文字サイズは <c>Assets/Prefabs/MiniGames/TypingMiniGame.prefab</c> で調整する。
    /// このクラスは割り当てられた表示先へ文字を書き込むだけで、座標もサイズも持たない。
    /// </remarks>
    public sealed class TypingMiniGame : MiniGameBase
    {
        [Header("Data")]
        [Tooltip("出題に使う問題集。この Prefab が自分で持つ。")]
        [SerializeField] private TypingQuestionDatabase database;

        [Header("View")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI inputText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Tuning")]
        [Tooltip("何回打ち間違えたら失敗にするか。")]
        [Min(1)] [SerializeField] private int allowedMisses = 2;

        [Tooltip("まだ打っていない部分の文字色。")]
        [SerializeField] private Color remainingInputColor = new Color(0.54f, 0.65f, 0.78f, 1f);

        private TypingQuestion question;
        private TypingInputEvaluator evaluator;
        private int missCount;
        private bool subscribed;
        private Keyboard subscribedKeyboard;

        public string CurrentQuestionText => question == null ? string.Empty : question.displayText;

        public override void Initialize(int difficulty, float timeLimit)
        {
            if (!SceneUiValidation.Require(this,
                    (nameof(database), database),
                    (nameof(questionText), questionText),
                    (nameof(inputText), inputText),
                    (nameof(statusText), statusText)))
            {
                base.Initialize(difficulty, timeLimit);
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            if (!database.TryGetRandomQuestion(difficulty, out question))
            {
                base.Initialize(difficulty, timeLimit);
                FinishGame(false, "NO QUESTION CONFIGURED");
                return;
            }

            evaluator = new TypingInputEvaluator(question.acceptedRomanizations);
            base.Initialize(difficulty, timeLimit);
            questionText.text = question.displayText;
            RefreshUi();
            Subscribe();
        }

        public bool ProcessInput(char input)
        {
            if (!IsPlaying || evaluator == null)
            {
                return false;
            }

            if (evaluator.TryInput(input))
            {
                RefreshUi();
                if (evaluator.IsCompleted)
                {
                    FinishGame(true, "COMPLETE");
                }

                return true;
            }

            missCount++;
            RefreshUi();
            if (missCount >= allowedMisses)
            {
                FinishGame(false, "MISSED");
            }

            return false;
        }

        protected override void OnUpdate(float deltaTime)
        {
            RefreshUi();
        }

        protected override void OnDestroy()
        {
            Unsubscribe();
            base.OnDestroy();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            var keyboard = Keyboard.current;
            if (subscribed || !IsPlaying || keyboard == null)
            {
                return;
            }

            keyboard.onTextInput += HandleTextInput;
            subscribedKeyboard = keyboard;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (subscribedKeyboard != null)
            {
                subscribedKeyboard.onTextInput -= HandleTextInput;
            }

            subscribedKeyboard = null;
            subscribed = false;
        }

        private void HandleTextInput(char input)
        {
            ProcessInput(input);
        }

        private void RefreshUi()
        {
            if (evaluator == null || inputText == null || statusText == null)
            {
                return;
            }

            inputText.text = evaluator.AcceptedInput
                + "<color=#" + ColorUtility.ToHtmlStringRGB(remainingInputColor) + ">"
                + evaluator.RemainingInput + "</color>";
            statusText.text = "MISS " + missCount + " / " + allowedMisses
                + "    TIME " + Mathf.CeilToInt(TimeRemaining).ToString("00");
        }
    }
}
