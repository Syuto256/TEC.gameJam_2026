using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Overwork.MiniGames.Typing
{
    /// <summary>Keyboard.onTextInput でローマ字入力を受ける共有タイピングミニゲーム。</summary>
    public sealed class TypingMiniGame : MiniGameBase
    {
        private TypingQuestionDatabase database;
        private TypingQuestion question;
        private TypingInputEvaluator evaluator;
        private TextMeshProUGUI questionText;
        private TextMeshProUGUI inputText;
        private TextMeshProUGUI statusText;
        private int missCount;
        private bool subscribed;
        private Keyboard subscribedKeyboard;

        public string CurrentQuestionText => question == null ? string.Empty : question.displayText;

        public void Configure(TypingQuestionDatabase questionDatabase)
        {
            database = questionDatabase;
        }

        public override void Initialize(int difficulty, float timeLimit)
        {
            if (database == null || !database.TryGetRandomQuestion(difficulty, out question))
            {
                base.Initialize(difficulty, timeLimit);
                FinishGame(false, "NO QUESTION CONFIGURED");
                return;
            }

            evaluator = new TypingInputEvaluator(question.acceptedRomanizations);
            BuildUi();
            base.Initialize(difficulty, timeLimit);
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
            if (missCount >= 2)
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

        private void BuildUi()
        {
            var panel = gameObject.AddComponent<Image>();
            panel.color = new Color(0.07f, 0.12f, 0.2f, 0.98f);
            CreateText("Title", "TYPE THE ROMANIZATION", new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.92f), 34f, TextAlignmentOptions.Center);
            questionText = CreateText("Question", question.displayText, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.76f), 64f, TextAlignmentOptions.Center);
            inputText = CreateText("Input", string.Empty, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.5f), 38f, TextAlignmentOptions.Center);
            statusText = CreateText("Status", string.Empty, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.32f), 25f, TextAlignmentOptions.Center);
        }

        private void RefreshUi()
        {
            if (evaluator == null || inputText == null || statusText == null)
            {
                return;
            }

            inputText.text = evaluator.AcceptedInput + "<color=#8AA6C6>" + evaluator.RemainingInput + "</color>";
            statusText.text = "MISS " + missCount + " / 2    TIME " + Mathf.CeilToInt(TimeRemaining).ToString("00");
        }

        private TextMeshProUGUI CreateText(string name, string value, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.color = Color.white;
            return text;
        }
    }
}
