using System;
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
        [Header("【使用データ】")]
        [Tooltip("出題に使う問題集。この Prefab が自分で持つ。")]
        [SerializeField] private TypingQuestionDatabase database;

        [Header("【表示先】")]
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

        [Header("【文言の書式】")]
        [SerializeField] private string questionFormat = "お題: {0}";
        [SerializeField] private string targetRomanizationFormat = "ローマ字: {0}";
        [SerializeField] private string acceptedInputFormat = "入力済み: {0}";
        [SerializeField] private string remainingInputFormat = "残り: {0}";
        [SerializeField] private string missFormat = "ミス: {0} / {1}";

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
        private Color remainingInputColor;

        public string CurrentQuestionText => question == null ? string.Empty : question.displayText;

        /// <summary>打ち間違えた直後で、入力を受け付けない状態か。</summary>
        public bool IsInputLocked => lockoutRemaining > 0f;

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
            remainingInputColor = remainingInputText.color;

            questionText.text = string.Format(questionFormat, question.displayText);
            RefreshHint();

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
            if (missCount >= allowedMisses)
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
                ApplyLockoutColor();
            }
        }

        private void ApplyLockoutColor()
        {
            if (remainingInputText != null)
            {
                remainingInputText.color = IsInputLocked ? lockedOutColor : remainingInputColor;
            }
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

        /// <summary>「ローマ字」の行を整える。お題と同じ綴りになるときは行ごと隠す。</summary>
        /// <remarks>
        /// 英単語のお題では、打つ綴りがお題そのものになる。
        /// 「お題: pull」の下に「ローマ字: pull」と並べても案内にならないため、そのときは出さない。
        /// </remarks>
        private void RefreshHint()
        {
            if (targetRomanizationText == null)
            {
                return;
            }

            var hint = evaluator.AcceptedInput + evaluator.RemainingInput;
            var duplicated = string.Equals(hint, question.displayText.Trim(), StringComparison.OrdinalIgnoreCase);
            targetRomanizationText.gameObject.SetActive(!duplicated);
            if (!duplicated)
            {
                targetRomanizationText.text = string.Format(targetRomanizationFormat, hint);
            }
        }

        private void RefreshUi()
        {
            if (evaluator == null)
            {
                return;
            }

            acceptedInputText.text = string.Format(acceptedInputFormat, evaluator.AcceptedInput);
            remainingInputText.text = string.Format(remainingInputFormat, evaluator.RemainingInput);
            ApplyLockoutColor();

            if (missText != null)
            {
                missText.text = string.Format(missFormat, missCount, allowedMisses);
            }
        }
    }
}
