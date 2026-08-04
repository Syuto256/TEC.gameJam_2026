using System;

public enum GameDifficulty { Easy, Normal, Hard, VeryHard, Endless }

/// <summary>タスクの種別。カタログと出現表がこの値で行を引く。</summary>
/// <remarks>
/// アセットは選択肢を整数で保存する。**並びを変えたり途中に挿入したりしないこと。**
/// 追加するときは必ず末尾に足す。途中に挿入すると、既存の登録が黙って別の種別を指すようになる。
/// </remarks>
public enum TaskKind
{
    Typing,
    Tracing,
    RapidClick,
    DragDrop,

    // ここから 2026-08-04 追加
    Qte,
    TimingStop
}
public enum TaskSurface { Pc, Pad }
public enum TaskState { Available, PlayerPlaying, AiProcessing, Resolved }
public enum TaskResolution { PlayerSuccess, PlayerFailure, AiSuccess, AiFailure, Expired }
public enum GameEndState { Playing, Clear, GameOver }

/// <summary>メインゲーム中に生成される一件のお題の実行時状態。</summary>
public sealed class TaskInstance
{
    internal TaskInstance(int id, TaskKind kind, TaskSurface surface, int level, float lifetimeSec)
    {
        Id = id;
        Kind = kind;
        Surface = surface;
        Level = Math.Max(1, Math.Min(4, level));
        InitialLifetimeSec = Math.Max(0f, lifetimeSec);
        RemainingLifetimeSec = InitialLifetimeSec;
        State = TaskState.Available;
    }

    public int Id { get; }
    public TaskKind Kind { get; }
    public TaskSurface Surface { get; }
    public int Level { get; }
    public float InitialLifetimeSec { get; }
    public float RemainingLifetimeSec { get; internal set; }
    public float CapturedTimeRatio { get; internal set; }
    public float AiRemainingProcessSec { get; internal set; }
    public TaskState State { get; internal set; }
    public TaskResolution? Resolution { get; internal set; }
    public bool IsTerminal => State == TaskState.Resolved;
}

/// <summary>タスクを解決した一回分の通知データ。</summary>
public readonly struct TaskResolutionResult
{
    public TaskResolutionResult(TaskInstance task, TaskResolution resolution, float capturedTimeRatio)
    {
        Task = task;
        Resolution = resolution;
        CapturedTimeRatio = capturedTimeRatio;
    }

    public TaskInstance Task { get; }
    public TaskResolution Resolution { get; }
    public float CapturedTimeRatio { get; }
}
