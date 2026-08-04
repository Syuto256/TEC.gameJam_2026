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

    /// <summary>1 つの面に同時に表示できるタスクの数。0 で無制限。</summary>
    public int MaxVisibleTasksPerSurface { get; set; }

    /// <summary>1 つの面の待機列に積める数。0 で無制限。</summary>
    public int MaxQueuedTasksPerSurface { get; set; }

    /// <summary>待機中のタスクも寿命を減らすか。false なら表示された時点から減り始める。</summary>
    public bool QueuedLifetimeTicks { get; set; }
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
    private int queuedCount;

    public TaskManager(TaskManagerSettings settings, ITaskRandom random = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.random = random ?? new SystemTaskRandom();
    }

    public event Action<TaskResolutionResult> TaskResolved;

    /// <summary>タスクが画面に出るべきタイミング。発生時、または待機列から繰り上がったときに飛ぶ。</summary>
    /// <remarks>吹き出しの生成はこの通知だけを見て行うこと。<see cref="CreateTask"/> の戻り値では待機分を取りこぼす。</remarks>
    public event Action<TaskInstance> TaskShown;

    public float ElapsedSec => elapsedSec;
    public IEnumerable<TaskInstance> Tasks => tasks.Values;

    /// <summary>まだ画面に出ていないタスクの総数。右下の「溜まっている件数」に使う。</summary>
    public int QueuedCount => queuedCount;

    public TaskInstance CreateTask(TaskKind kind, TaskSurface surface, int level, float lifetimeSec)
    {
        // 空きの判定は登録する前に行う。後にすると、これから足す 1 件が自分の枠を埋めてしまう。
        var hasFreeSlot = HasFreeVisibleSlot(surface);

        var task = new TaskInstance(nextTaskId++, kind, surface, level, lifetimeSec);
        tasks.Add(task.Id, task);

        // 表示枠が空いていなければ待機列へ回す。出ていないタスクはクリックできないため、
        // 既定では寿命も進めない（Tick を参照）。
        if (hasFreeSlot)
        {
            TaskShown?.Invoke(task);
        }
        else
        {
            task.State = TaskState.Queued;
            queuedCount++;
        }

        return task;
    }

    /// <summary>その面が新しいタスクを受け付けられるか。待機列の上限だけを見る。</summary>
    public bool CanAcceptTask(TaskSurface surface)
    {
        return settings.MaxQueuedTasksPerSurface <= 0
            || CountQueued(surface) < settings.MaxQueuedTasksPerSurface;
    }

    /// <summary>その面で画面に出ているタスクの数。担当中・AI 処理中も枠を占有している。</summary>
    public int CountVisible(TaskSurface surface)
    {
        var count = 0;
        foreach (var task in tasks.Values)
        {
            if (task.Surface == surface && !task.IsTerminal && task.State != TaskState.Queued)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>その面で待機しているタスクの数。</summary>
    public int CountQueued(TaskSurface surface)
    {
        var count = 0;
        foreach (var task in tasks.Values)
        {
            if (task.Surface == surface && task.State == TaskState.Queued)
            {
                count++;
            }
        }

        return count;
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

        // 空いた表示枠を待機列から埋める。決着のたびに呼ぶのではなく毎フレーム均すことで、
        // 決着経路が増えても繰り上げの呼び忘れが起きない。
        PromoteQueuedTasks();

        foreach (var task in tasks.Values)
        {
            if (task.State == TaskState.Available || (task.State == TaskState.Queued && settings.QueuedLifetimeTicks))
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

    /// <summary>空いた表示枠を、待機列の古いものから埋める。</summary>
    /// <remarks>
    /// 待機が無ければ何もしない。件数は多くても数十なので、素直に走査している。
    /// </remarks>
    private void PromoteQueuedTasks()
    {
        while (queuedCount > 0 && TryFindPromotable(out var task))
        {
            task.State = TaskState.Available;
            queuedCount--;
            TaskShown?.Invoke(task);
        }
    }

    /// <summary>繰り上げる 1 件を選ぶ。表示枠が空いている面のうち、最も古いものを返す。</summary>
    private bool TryFindPromotable(out TaskInstance promotable)
    {
        promotable = null;
        foreach (var task in tasks.Values)
        {
            if (task.State != TaskState.Queued) continue;
            if (promotable != null && task.Id >= promotable.Id) continue;
            if (!HasFreeVisibleSlot(task.Surface)) continue;
            promotable = task;
        }

        return promotable != null;
    }

    private bool HasFreeVisibleSlot(TaskSurface surface)
    {
        return settings.MaxVisibleTasksPerSurface <= 0
            || CountVisible(surface) < settings.MaxVisibleTasksPerSurface;
    }

    private void Resolve(TaskInstance task, TaskResolution resolution)
    {
        if (task.IsTerminal) return;
        if (task.State == TaskState.Queued) queuedCount--;
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
