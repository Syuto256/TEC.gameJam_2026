using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overwork.MiniGames.Typing
{
    /// <summary>Keyboard.onTextInput でローマ字入力を受ける共有タイピングミニゲーム。</summary>
    /// <remarks>
    /// 表示の並びは Suzuki の試作（`Assets/Personal/Suzuki/Suzuki.unity`）から取り込んでいる。
    /// お題・ローマ字・入力済み・残りを縦に並べ、ミスを左下、残り時間を右下に置く。
    /// 配置・配色・文字サイズは `Assets/Prefabs/MiniGames/TypingMiniGame.prefab` で調整する。
    /// </remarks>
    public sealed class TypingMiniGame : MiniGameBase
    {
        [Header("Data")]
        [Tooltip("出題に使う問題集。この Prefab が自分で持つ。")]
        [SerializeField] private TypingQuestionDatabase database;

        [Header("View")]
        [Tooltip("お題（漢字表記）。")]
        [SerializeField] private TMP_Text questionText;

        [Tooltip("打つべきローマ字の全体。任意。")]
        [SerializeField] private TMP_Text targetRomanizationText;

        [Tooltip("すでに正しく打てた部分。")]
        [SerializeField] private TMP_Text acceptedInputText;

        [Tooltip("これから打つ部分。")]
        [SerializeField] private TMP_Text remainingInputText;

        [Tooltip("ミス数。任意。")]
        [SerializeField] private TMP_Text missText;

        [Header("Text format")]
        [SerializeField] private string questionFormat = "お題: {0}";
        [SerializeField] private string targetRomanizationFormat = "ローマ字: {0}";
        [SerializeField] private string acceptedInputFormat = "入力済み: {0}";
        [SerializeField] private string remainingInputFormat = "残り: {0}";
        [SerializeField] private string missFormat = "ミス: {0} / {1}";

        [Header("Tuning")]
        [Tooltip("何回打ち間違えたら失敗にするか。")]
        [Min(1)] [SerializeField] private int allowedMisses = 2;

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
                    (nameof(acceptedInputText), acceptedInputText),
                    (nameof(remainingInputText), remainingInputText)))
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

            questionText.text = string.Format(questionFormat, question.displayText);
            if (targetRomanizationText != null)
            {
                targetRomanizationText.text = string.Format(
                    targetRomanizationFormat, evaluator.AcceptedInput + evaluator.RemainingInput);
            }

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
            if (evaluator == null)
            {
                return;
            }

            acceptedInputText.text = string.Format(acceptedInputFormat, evaluator.AcceptedInput);
            remainingInputText.text = string.Format(remainingInputFormat, evaluator.RemainingInput);

            if (missText != null)
            {
                missText.text = string.Format(missFormat, missCount, allowedMisses);
            }
        }
    }
}
