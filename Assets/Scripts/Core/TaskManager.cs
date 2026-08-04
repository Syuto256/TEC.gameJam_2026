using System;
using System.Collections.Generic;

public interface ITaskRandom
{
    float NextFloat();
}

public sealed class SystemTaskRandom : ITaskRandom
{
    private readonly Random random = new Random();
    public float NextFloat() => (float)random.NextDouble();
}

public sealed class TaskManagerSettings
{
    public float AiSuccessRate { get; set; } = 0.9f;
    public float AiProcessDurationSec { get; set; } = 0.4f;
    public float AiCooldownSec { get; set; }
}

/// <summary>タスクの受付、担当開始、期限・AI処理を扱う UI 非依存の実行時モデル。</summary>
public sealed class TaskManager
{
    private readonly Dictionary<int, TaskInstance> tasks = new Dictionary<int, TaskInstance>();
    private readonly ITaskRandom random;
    private readonly TaskManagerSettings settings;
    private int nextTaskId = 1;
    private float elapsedSec;
    private float nextAiRequestSec;

    public TaskManager(TaskManagerSettings settings, ITaskRandom random = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.random = random ?? new SystemTaskRandom();
    }

    public event Action<TaskResolutionResult> TaskResolved;
    public float ElapsedSec => elapsedSec;
    public IEnumerable<TaskInstance> Tasks => tasks.Values;

    public TaskInstance CreateTask(TaskKind kind, TaskSurface surface, int level, float lifetimeSec)
    {
        var task = new TaskInstance(nextTaskId++, kind, surface, level, lifetimeSec);
        tasks.Add(task.Id, task);
        return task;
    }

    public bool TryStartPlayer(int taskId)
    {
        if (!TryGetAvailableTask(taskId, out var task)) return false;
        task.CapturedTimeRatio = CalculateRemainingRatio(task);
        task.State = TaskState.PlayerPlaying;
        return true;
    }

    public bool TryRequestAi(int taskId)
    {
        if (elapsedSec < nextAiRequestSec || !TryGetAvailableTask(taskId, out var task)) return false;
        task.CapturedTimeRatio = CalculateRemainingRatio(task);
        task.AiRemainingProcessSec = Math.Max(0f, settings.AiProcessDurationSec);
        task.State = TaskState.AiProcessing;
        nextAiRequestSec = elapsedSec + Math.Max(0f, settings.AiCooldownSec);
        return true;
    }

    public bool CompletePlayer(int taskId, bool succeeded)
    {
        if (!tasks.TryGetValue(taskId, out var task) || task.State != TaskState.PlayerPlaying) return false;
        Resolve(task, succeeded ? TaskResolution.PlayerSuccess : TaskResolution.PlayerFailure);
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        elapsedSec += deltaTime;
        foreach (var task in tasks.Values)
        {
            if (task.State == TaskState.Available)
            {
                task.RemainingLifetimeSec = Math.Max(0f, task.RemainingLifetimeSec - deltaTime);
                if (task.RemainingLifetimeSec <= 0f) Resolve(task, TaskResolution.Expired);
            }
            else if (task.State == TaskState.AiProcessing)
            {
                task.AiRemainingProcessSec -= deltaTime;
                if (task.AiRemainingProcessSec <= 0f)
                {
                    Resolve(task, random.NextFloat() < Clamp01(settings.AiSuccessRate)
                        ? TaskResolution.AiSuccess : TaskResolution.AiFailure);
                }
            }
        }
    }

    public bool TryGetTask(int taskId, out TaskInstance task) => tasks.TryGetValue(taskId, out task);

    private bool TryGetAvailableTask(int taskId, out TaskInstance task)
    {
        return tasks.TryGetValue(taskId, out task) && task.State == TaskState.Available;
    }

    private void Resolve(TaskInstance task, TaskResolution resolution)
    {
        if (task.IsTerminal) return;
        task.State = TaskState.Resolved;
        task.Resolution = resolution;
        TaskResolved?.Invoke(new TaskResolutionResult(task, resolution, task.CapturedTimeRatio));
    }

    private static float CalculateRemainingRatio(TaskInstance task)
    {
        return task.InitialLifetimeSec <= 0f ? 0f : Clamp01(task.RemainingLifetimeSec / task.InitialLifetimeSec);
    }

    private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
}
