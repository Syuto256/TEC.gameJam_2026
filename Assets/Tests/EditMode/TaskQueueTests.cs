using System.Collections.Generic;
using NUnit.Framework;

/// <summary>表示枠があふれたタスクの待機列のふるまい。</summary>
public class TaskQueueTests
{
    private static TaskManager CreateManager(int maxVisible, bool queuedLifetimeTicks, out List<int> shownIds)
    {
        var manager = new TaskManager(new TaskManagerSettings
        {
            MaxVisibleTasksPerSurface = maxVisible,
            QueuedLifetimeTicks = queuedLifetimeTicks
        });

        var ids = new List<int>();
        manager.TaskShown += task => ids.Add(task.Id);
        shownIds = ids;
        return manager;
    }

    [Test]
    public void TasksBeyondTheVisibleLimit_AreQueuedAndNotShown()
    {
        var manager = CreateManager(2, false, out var shownIds);

        var first = manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
        var second = manager.CreateTask(TaskKind.Tracing, TaskSurface.Pc, 1, 10f);
        var third = manager.CreateTask(TaskKind.Qte, TaskSurface.Pc, 1, 10f);

        Assert.That(first.State, Is.EqualTo(TaskState.Available));
        Assert.That(second.State, Is.EqualTo(TaskState.Available));
        Assert.That(third.State, Is.EqualTo(TaskState.Queued));
        Assert.That(manager.QueuedCount, Is.EqualTo(1));
        Assert.That(shownIds, Is.EqualTo(new[] { first.Id, second.Id }));
    }

    [Test]
    public void QueueIsPerSurface()
    {
        var manager = CreateManager(1, false, out _);

        manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
        var padTask = manager.CreateTask(TaskKind.Tracing, TaskSurface.Pad, 1, 10f);

        // 面ごとに枠を数えるため、別の面はふさがらない。
        Assert.That(padTask.State, Is.EqualTo(TaskState.Available));
        Assert.That(manager.QueuedCount, Is.EqualTo(0));
    }

    [Test]
    public void ResolvingAVisibleTask_PromotesTheOldestQueuedTask()
    {
        var manager = CreateManager(1, false, out var shownIds);

        var visible = manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
        var waiting = manager.CreateTask(TaskKind.Tracing, TaskSurface.Pc, 1, 10f);
        var later = manager.CreateTask(TaskKind.Qte, TaskSurface.Pc, 1, 10f);

        Assert.That(manager.TryStartPlayer(visible.Id), Is.True);
        Assert.That(manager.CompletePlayer(visible.Id, true), Is.True);
        manager.Tick(0.1f);

        // 空いた 1 枠には、後から来たものではなく古いほうが入る。
        Assert.That(waiting.State, Is.EqualTo(TaskState.Available));
        Assert.That(later.State, Is.EqualTo(TaskState.Queued));
        Assert.That(shownIds, Is.EqualTo(new[] { visible.Id, waiting.Id }));
    }

    [Test]
    public void QueuedTask_DoesNotLoseLifetimeByDefault()
    {
        var manager = CreateManager(1, false, out _);

        manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
        var waiting = manager.CreateTask(TaskKind.Tracing, TaskSurface.Pc, 1, 10f);

        manager.Tick(5f);

        // 画面に出ていない＝クリックできないため、既定では寿命を進めない。
        Assert.That(waiting.RemainingLifetimeSec, Is.EqualTo(10f).Within(0.001f));
        Assert.That(waiting.State, Is.EqualTo(TaskState.Queued));
    }

    [Test]
    public void QueuedTask_LosesLifetimeWhenTheSettingIsOn()
    {
        var manager = CreateManager(1, true, out _);

        manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
        var waiting = manager.CreateTask(TaskKind.Tracing, TaskSurface.Pc, 1, 10f);

        manager.Tick(5f);

        Assert.That(waiting.RemainingLifetimeSec, Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void QueuedTask_CanExpireUnseenWhenTheSettingIsOn()
    {
        var manager = CreateManager(1, true, out _);
        var resolutions = new List<TaskResolution>();

        manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 30f);
        var waiting = manager.CreateTask(TaskKind.Tracing, TaskSurface.Pc, 1, 10f);
        manager.TaskResolved += result => resolutions.Add(result.Resolution);

        manager.Tick(10f);

        Assert.That(waiting.State, Is.EqualTo(TaskState.Resolved));
        Assert.That(resolutions, Is.EqualTo(new[] { TaskResolution.Expired }));
        Assert.That(manager.QueuedCount, Is.EqualTo(0));
    }

    [Test]
    public void FullQueue_StopsAcceptingNewTasks()
    {
        var manager = new TaskManager(new TaskManagerSettings
        {
            MaxVisibleTasksPerSurface = 1,
            MaxQueuedTasksPerSurface = 1
        });

        manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
        Assert.That(manager.CanAcceptTask(TaskSurface.Pc), Is.True);

        manager.CreateTask(TaskKind.Tracing, TaskSurface.Pc, 1, 10f);
        Assert.That(manager.CanAcceptTask(TaskSurface.Pc), Is.False);
    }

    [Test]
    public void ZeroLimits_MeanUnlimited()
    {
        var manager = CreateManager(0, false, out var shownIds);

        for (var i = 0; i < 5; i++)
        {
            manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
        }

        Assert.That(manager.QueuedCount, Is.EqualTo(0));
        Assert.That(shownIds.Count, Is.EqualTo(5));
        Assert.That(manager.CanAcceptTask(TaskSurface.Pc), Is.True);
    }
}
