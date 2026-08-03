using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 日本語の読みをローマ字で入力するミニゲーム本体。
/// 共通の MiniGameBase は変更せず、この個人領域内で継承して利用する。
/// </summary>
public sealed class TypingMiniGame : MiniGameBase
{
    private const int MaxMissCount = 2;
    private const float InputLockDuration = 0.2f;

    [Header("問題データ")]
    [SerializeField] private TypingWordDatabase wordDatabase;

    [Header("UI")]
    [SerializeField] private TypingMiniGameView view;

    private TypingInputEvaluator inputEvaluator;
    private TypingWordEntry currentEntry;
    private Keyboard subscribedKeyboard;
    private int missCount;
    private float inputLockRemaining;

    private void Awake()
    {
        OnCompleted += HandleCompleted;
    }

    private void OnEnable()
    {
        SubscribeKeyboard();
    }

    private void OnDisable()
    {
        UnsubscribeKeyboard();
    }

    public override void Initialize(int difficulty, float timeLimit)
    {
        base.Initialize(difficulty, timeLimit);

        missCount = 0;
        inputLockRemaining = 0f;
        inputEvaluator = null;
        currentEntry = null;

        var typingDifficulty = ToTypingDifficulty(difficulty);
        if (wordDatabase == null || !wordDatabase.TryGetRandomEntry(typingDifficulty, out currentEntry))
        {
            Debug.LogError($"[{nameof(TypingMiniGame)}] {typingDifficulty} の有効なお題がありません。", this);
            FinishGame(false, "NO WORD CONFIGURED");
            return;
        }

        try
        {
            var candidates = RomanizationGenerator.GenerateCandidates(currentEntry.Reading);
            var canonicalCandidate = RomanizationGenerator.GenerateCanonical(currentEntry.Reading);
            inputEvaluator = new TypingInputEvaluator(candidates, canonicalCandidate);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"[{nameof(TypingMiniGame)}] 読み \"{currentEntry.Reading}\" をローマ字へ変換できません。\n{exception.Message}", this);
            FinishGame(false, "INVALID READING");
            return;
        }

        view?.ShowQuestion(currentEntry.DisplayText, inputEvaluator.GetDisplayCandidate());
        UpdateView();
    }

    /// <summary>
    /// UI や自動テストから 1 文字を明示的に渡すための入口。
    /// 無効時間中の入力は結果を返さず、完全に無視する。
    /// </summary>
    public void ProcessInput(char input)
    {
        if (!IsPlaying || inputEvaluator == null || inputLockRemaining > 0f || char.IsControl(input))
        {
            return;
        }

        if (inputEvaluator.TryInput(input))
        {
            UpdateView();
            if (inputEvaluator.IsCompleted)
            {
                FinishGame(true, "COMPLETE");
            }

            return;
        }

        missCount++;
        inputLockRemaining = InputLockDuration;
        UpdateView();

        if (missCount >= MaxMissCount)
        {
            FinishGame(false, "MISSED");
        }
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (inputLockRemaining > 0f)
        {
            inputLockRemaining = Mathf.Max(0f, inputLockRemaining - deltaTime);
            UpdateView();
            return;
        }

        SubscribeKeyboard();
        UpdateView();
    }

    protected override void OnDestroy()
    {
        UnsubscribeKeyboard();
        OnCompleted -= HandleCompleted;
        base.OnDestroy();
    }

    private void SubscribeKeyboard()
    {
        if (subscribedKeyboard == Keyboard.current)
        {
            return;
        }

        UnsubscribeKeyboard();
        if (Keyboard.current != null)
        {
            subscribedKeyboard = Keyboard.current;
            subscribedKeyboard.onTextInput += HandleTextInput;
        }
    }

    private void UnsubscribeKeyboard()
    {
        if (subscribedKeyboard != null)
        {
            subscribedKeyboard.onTextInput -= HandleTextInput;
            subscribedKeyboard = null;
        }
    }

    private void HandleTextInput(char input)
    {
        ProcessInput(input);
    }

    private void HandleCompleted(bool success, string reason)
    {
        view?.ShowResult(success ? "結果: 成功" : $"結果: 失敗（{reason}）");
        UpdateView();
    }

    private void UpdateView()
    {
        if (inputEvaluator == null)
        {
            return;
        }

        view?.ShowProgress(
            inputEvaluator.AcceptedInput,
            inputEvaluator.GetRemainingInput(),
            missCount,
            MaxMissCount,
            TimeRemaining);
    }

    private static TypingDifficulty ToTypingDifficulty(int difficulty)
    {
        if (Enum.IsDefined(typeof(TypingDifficulty), difficulty))
        {
            return (TypingDifficulty)difficulty;
        }

        Debug.LogWarning($"[{nameof(TypingMiniGame)}] 未定義の難易度 {difficulty} を Easy として扱います。");
        return TypingDifficulty.Easy;
    }
}
