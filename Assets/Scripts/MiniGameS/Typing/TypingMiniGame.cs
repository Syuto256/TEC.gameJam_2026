using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overwork.MiniGames.Typing
{
    /// <summary>Keyboard.onTextInput でローマ字入力を受ける共有タイピングミニゲーム。</summary>
    /// <remarks>
    /// 表示の並びは Suzuki の試作（`Assets/Personal/Suzuki/Suzuki.unity`）から取り込んでいる。
    /// 読み・お題・入力済みと残りを表示し、ミスを左下、残り時間を右下に置く。
    /// 配置・配色・文字サイズは `Assets/Prefabs/MiniGames/TypingMiniGame.prefab` で調整する。
    /// </remarks>
    public sealed class TypingMiniGame : MiniGameBase
    {
        [Header("【使用データ】")]
        [Tooltip("出題に使う問題集。この Prefab が自分で持つ。")]
        [SerializeField] private TypingQuestionDatabase database;

        [Header("【表示先】")]
        [Tooltip("お題（漢字表記）。")]
        [SerializeField] private TMP_Text questionText;

        [Tooltip("お題の読み。空の問題では行ごと隠す。")]
        [SerializeField] private TMP_Text readingText;

        [Tooltip("すでに打てた部分と、これから打つ部分を1行で表示する。")]
        [SerializeField] private TMP_Text spellingText;

        [Tooltip("ミス数。任意。")]
        [SerializeField] private TMP_Text missText;

        [Header("【文言の書式】")]
        [SerializeField] private string questionFormat = "{0}";
        [SerializeField] private string missFormat = "ミス: {0} / {1}";

        [Header("【綴りの色】")]
        [Tooltip("すでに正しく打てた部分の色。")]
        [SerializeField] private Color acceptedInputColor = new Color(0.55f, 1f, 0.70f, 1f);

        [Tooltip("これから打つ部分の色。")]
        [SerializeField] private Color remainingInputColor = new Color(0.72f, 0.80f, 0.90f, 1f);

        [Header("【難度の調整】")]
        [Tooltip("何回打ち間違えたら失敗にするか。")]
        [Min(1)] [SerializeField] private int allowedMisses = 2;

        [Tooltip("打ち間違えた直後、この秒数だけ入力を受け付けない。\n" +
                 "速く打つと 1 回のつまずきで何回もミスが増えてしまうため、間を置く。\n" +
                 "0 にすると無効になり、打った分だけミスが増える。")]
        [Min(0f)] [SerializeField] private float missLockoutSeconds = 0.2f;

        [Tooltip("入力を受け付けない間、これから打つ部分をこの色にする。\n" +
                 "何も変わらないと、キーが効かなくなったように見えるためである。")]
        [SerializeField] private Color lockedOutColor = new Color(1f, 0.55f, 0.62f, 1f);

        private TypingQuestion question;
        private TypingInputEvaluator evaluator;
        private int missCount;
        private bool subscribed;
        private Keyboard subscribedKeyboard;
        private float lockoutRemaining;

        public string CurrentQuestionText => question == null ? string.Empty : question.displayText;

        /// <summary>打ち間違えた直後で、入力を受け付けない状態か。</summary>
        public bool IsInputLocked => lockoutRemaining > 0f;

        public override void Initialize(int difficulty, float timeLimit)
        {
            if (!SceneUiValidation.Require(this,
                    (nameof(database), database),
                    (nameof(questionText), questionText),
                    (nameof(readingText), readingText),
                    (nameof(spellingText), spellingText)))
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

            // 打てる綴りは毎回この場で組み立てる。読みからの自動生成、手書きのユニーク入力、
            // 英単語のお題そのもの、どれが使われるかは問題データが決める。
            System.Collections.Generic.IReadOnlyList<string> candidates;
            string error;
            if (!TypingCandidateBuilder.TryBuild(question, out candidates, out error))
            {
                Debug.LogError(
                    nameof(TypingMiniGame) + " (" + name + "): 「" + question.displayText + "」の打てる綴りを作れません -> "
                    + error, this);
                base.Initialize(difficulty, timeLimit);
                FinishGame(false, "BAD READING");
                return;
            }

            evaluator = new TypingInputEvaluator(candidates);
            base.Initialize(difficulty, timeLimit);

            missCount = 0;
            lockoutRemaining = 0f;
            questionText.text = string.Format(questionFormat, question.displayText);
            RefreshUi();
            Subscribe();
        }

        public bool ProcessInput(char input)
        {
            if (!IsPlaying || evaluator == null)
            {
                return false;
            }

            // 打てない文字は、そもそも打たれなかったことにする。
            // 日本語入力が有効なままだとかなが飛んでくるが、それをミスに数えると打ち始めた瞬間に失敗する。
            // BackSpace などの退避キーも同じくここで落ちる。
            if (!TypingInputEvaluator.IsTypableCharacter(input))
            {
                return false;
            }

            // 打ち間違えた直後の入力は、ミスにも進捗にも数えず完全に捨てる。
            // 速く打つ人ほど 1 回のつまずきで指が数文字ぶん先に進んでしまい、
            // 間を置かないと 1 度の打ち間違いで即 2 ミス失敗になるためである。
            if (IsInputLocked)
            {
                return false;
            }

            if (evaluator.TryInput(input))
            {
                PlayInputFeedback(true);
                RefreshUi();
                if (evaluator.IsCompleted)
                {
                    FinishGame(true, "COMPLETE");
                }

                return true;
            }

            PlayInputFeedback(false);
            missCount++;
            lockoutRemaining = missLockoutSeconds;
            RefreshUi();

            // ★ 修正: 制限時間が 90 秒以上（チュートリアル時）はミス失敗を行わない！
            var isTutorial = TimeLimit >= 90f;
            if (!isTutorial && missCount >= allowedMisses)
            {
                FinishGame(false, "MISSED");
            }

            return false;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (lockoutRemaining <= 0f)
            {
                return;
            }

            lockoutRemaining -= deltaTime;
            if (lockoutRemaining <= 0f)
            {
                lockoutRemaining = 0f;
                RefreshSpellingText();
            }
        }

        private void RefreshSpellingText()
        {
            if (spellingText == null || evaluator == null)
            {
                return;
            }

            var acceptedHex = ColorUtility.ToHtmlStringRGBA(acceptedInputColor);
            var remainingHex = ColorUtility.ToHtmlStringRGBA(IsInputLocked ? lockedOutColor : remainingInputColor);
            spellingText.text = "<color=#" + acceptedHex + ">" + evaluator.AcceptedInput
                + "</color><color=#" + remainingHex + ">" + evaluator.RemainingInput + "</color>";
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

            if (readingText != null)
            {
                var reading = question == null ? string.Empty : question.reading?.Trim();
                readingText.gameObject.SetActive(!string.IsNullOrEmpty(reading));
                readingText.text = reading ?? string.Empty;
            }

            RefreshSpellingText();

            if (missText != null)
            {
                missText.text = string.Format(missFormat, missCount, allowedMisses);
            }
        }
    }
}
