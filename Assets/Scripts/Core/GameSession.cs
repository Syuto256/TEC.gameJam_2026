using System;
using System.Collections.Generic;

public sealed class GameSessionSettings
{
    public GameDifficulty Difficulty { get; set; }
    public bool IsEndless { get; set; }
    public float DurationSec { get; set; } = 180f;
    public int MaxHp { get; set; } = 100;
    public int PlayerFailureDamage { get; set; } = 5;
    public int AiFailureDamage { get; set; } = 5;
    public int ExpiredDamage { get; set; } = 8;
    public float AiScoreMultiplier { get; set; } = 0.6f;
    public int BaseScoreLevel1 { get; set; } = 100;
    public int BaseScoreLevel2 { get; set; } = 150;
    public int BaseScoreLevel3 { get; set; } = 220;
    public int BaseScoreLevel4 { get; set; } = 300;
    public float MaxTimeBonusAdd { get; set; } = 0.5f;

    /// <summary>コンボ 1 段につき増えるスコア倍率。0.1 なら 1 コンボごとに +10%。</summary>
    public float ComboScoreAddPerCombo { get; set; } = 0.1f;

    /// <summary>コンボ倍率の上限。2.0 なら何コンボ繋いでも最大 2 倍で頭打ちになる。</summary>
    public float MaxComboMultiplier { get; set; } = 2.0f;
}

/// <summary>HP、スコア、終了判定を保持するゲームセッション。</summary>
public sealed class GameSession
{
    private readonly GameSessionSettings settings;
    private readonly Dictionary<TaskResolution, int> resolutionCounts = new Dictionary<TaskResolution, int>();

    public GameSession(GameSessionSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        RemainingTimeSec = Math.Max(0f, settings.DurationSec);
        Hp = Math.Max(0, settings.MaxHp);
        EndState = Hp <= 0 ? GameEndState.GameOver : GameEndState.Playing;
    }

    public GameDifficulty Difficulty => settings.Difficulty;
    public bool IsEndless => settings.IsEndless;
    public int MaxHp => Math.Max(0, settings.MaxHp);
    public float RemainingTimeSec { get; private set; }
    public int Hp { get; private set; }
    public int Score { get; private set; }
    public GameEndState EndState { get; private set; }
    public int AiUsedCount { get; private set; }
    public IReadOnlyDictionary<TaskResolution, int> ResolutionCounts => resolutionCounts;

    /// <summary>自力成功が途切れずに続いている件数。失敗と時間切れで 0 に戻る。</summary>
    public int ComboCount { get; private set; }

    public void Tick(float deltaTime)
    {
        if (EndState != GameEndState.Playing || deltaTime <= 0f || settings.IsEndless) return;
        RemainingTimeSec = Math.Max(0f, RemainingTimeSec - deltaTime);
        if (RemainingTimeSec <= 0f) EndState = GameEndState.Clear;
    }

    /// <summary>解決結果を反映し、このとき加算されたスコアを返す。演出はこの戻り値を使う。</summary>
    public int Apply(TaskResolutionResult result)
    {
        if (EndState != GameEndState.Playing) return 0;

        resolutionCounts[result.Resolution] = GetResolutionCount(result.Resolution) + 1;
        var addedScore = 0;

        switch (result.Resolution)
        {
            case TaskResolution.PlayerSuccess:
                ComboCount++;
                addedScore = CalculateScore(result.Task.Level, result.CapturedTimeRatio, 1f, ComboCount);
                Score += addedScore;
                break;

            // TODO(2026-08-04): AI 成功は現在コンボを伸ばさないが、コンボ倍率は受け取る。
            // 自力と AI の評価差（仕様書 22.5）が決まったら、どちらに寄せるか確定する。
            case TaskResolution.AiSuccess:
                AiUsedCount++;
                addedScore = CalculateScore(
                    result.Task.Level, result.CapturedTimeRatio, settings.AiScoreMultiplier, ComboCount);
                Score += addedScore;
                break;

            case TaskResolution.PlayerFailure:
            case TaskResolution.AiFailure:
            case TaskResolution.Expired:
                ComboCount = 0;
                ApplyDamage(result.Resolution == TaskResolution.PlayerFailure ? settings.PlayerFailureDamage :
                            result.Resolution == TaskResolution.AiFailure ? settings.AiFailureDamage : settings.ExpiredDamage);
                break;
        }

        return addedScore;
    }

    private int CalculateScore(int level, float timeRatio, float multiplier, int combo)
    {
        var baseScore = level == 1 ? settings.BaseScoreLevel1 : level == 2 ? settings.BaseScoreLevel2
            : level == 3 ? settings.BaseScoreLevel3 : settings.BaseScoreLevel4;
        var ratio = Math.Max(0f, Math.Min(1f, timeRatio));

        // 1 コンボ目は等倍。2 コンボ目から 1 段ずつ増え、上限で頭打ちにする。
        var comboBonus = 1f + Math.Max(0, combo - 1) * settings.ComboScoreAddPerCombo;
        comboBonus = Math.Min(comboBonus, Math.Max(1f, settings.MaxComboMultiplier));

        return Math.Max(0, (int)Math.Round(
            baseScore * (1f + settings.MaxTimeBonusAdd * ratio) * Math.Max(0f, multiplier) * comboBonus));
    }

    public GameSessionResult CreateResult()
    {
        return new GameSessionResult(Difficulty, Score, Hp, EndState, AiUsedCount, resolutionCounts);
    }

    private int GetResolutionCount(TaskResolution resolution)
    {
        return resolutionCounts.TryGetValue(resolution, out var count) ? count : 0;
    }

    private void ApplyDamage(int amount)
    {
        Hp = Math.Max(0, Hp - Math.Max(0, amount));
        if (Hp <= 0) EndState = GameEndState.GameOver;
    }
}

/// <summary>Clear/GameOver 画面へ渡す、終了時点のセッション集計。</summary>
public sealed class GameSessionResult
{
    public GameSessionResult(
        GameDifficulty difficulty,
        int finalScore,
        int finalHp,
        GameEndState endState,
        int aiUsedCount,
        IReadOnlyDictionary<TaskResolution, int> resolutionCounts)
    {
        Difficulty = difficulty;
        FinalScore = finalScore;
        FinalHp = finalHp;
        EndState = endState;
        AiUsedCount = aiUsedCount;
        ResolutionCounts = new Dictionary<TaskResolution, int>(resolutionCounts);
    }

    public GameDifficulty Difficulty { get; }
    public int FinalScore { get; }
    public int FinalHp { get; }
    public GameEndState EndState { get; }
    public int AiUsedCount { get; }
    public IReadOnlyDictionary<TaskResolution, int> ResolutionCounts { get; }
}
