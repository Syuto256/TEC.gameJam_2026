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

    public void Tick(float deltaTime)
    {
        if (EndState != GameEndState.Playing || deltaTime <= 0f || settings.IsEndless) return;
        RemainingTimeSec = Math.Max(0f, RemainingTimeSec - deltaTime);
        if (RemainingTimeSec <= 0f) EndState = GameEndState.Clear;
    }

    public void Apply(TaskResolutionResult result)
    {
        if (EndState != GameEndState.Playing) return;
        resolutionCounts[result.Resolution] = GetResolutionCount(result.Resolution) + 1;
        switch (result.Resolution)
        {
            case TaskResolution.PlayerSuccess:
                Score += CalculateScore(result.Task.Level, result.CapturedTimeRatio, 1f);
                break;
            case TaskResolution.AiSuccess:
                AiUsedCount++;
                Score += CalculateScore(result.Task.Level, result.CapturedTimeRatio, settings.AiScoreMultiplier);
                break;
            case TaskResolution.PlayerFailure:
                ApplyDamage(settings.PlayerFailureDamage);
                break;
            case TaskResolution.AiFailure:
                AiUsedCount++;
                ApplyDamage(settings.AiFailureDamage);
                break;
            case TaskResolution.Expired:
                ApplyDamage(settings.ExpiredDamage);
                break;
        }
    }

    public GameSessionResult CreateResult()
    {
        return new GameSessionResult(Difficulty, Score, Hp, EndState, AiUsedCount, resolutionCounts);
    }

    private int GetResolutionCount(TaskResolution resolution)
    {
        return resolutionCounts.TryGetValue(resolution, out var count) ? count : 0;
    }

    private int CalculateScore(int level, float timeRatio, float multiplier)
    {
        var baseScore = level == 1 ? settings.BaseScoreLevel1 : level == 2 ? settings.BaseScoreLevel2
            : level == 3 ? settings.BaseScoreLevel3 : settings.BaseScoreLevel4;
        var ratio = Math.Max(0f, Math.Min(1f, timeRatio));
        return Math.Max(0, (int)Math.Round(baseScore * (1f + settings.MaxTimeBonusAdd * ratio) * Math.Max(0f, multiplier)));
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
