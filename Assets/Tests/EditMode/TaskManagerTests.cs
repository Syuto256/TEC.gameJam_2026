using NUnit.Framework;

public class TaskManagerTests
{
    [Test]
    public void ZeroAiCooldown_AllowsMultipleConcurrentRequests()
    {
        var manager = new TaskManager(new TaskManagerSettings { AiCooldownSec = 0f });
        var first = manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
        var second = manager.CreateTask(TaskKind.Tracing, TaskSurface.Pad, 1, 10f);

        Assert.That(manager.TryRequestAi(first.Id), Is.True);
        Assert.That(manager.TryRequestAi(second.Id), Is.True);
        Assert.That(first.State, Is.EqualTo(TaskState.AiProcessing));
        Assert.That(second.State, Is.EqualTo(TaskState.AiProcessing));
    }

    [Test]
    public void PlayerStart_FreezesTaskLifetimeAndCapturesRemainingRatio()
    {
        var manager = new TaskManager(new TaskManagerSettings());
        var task = manager.CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);

        manager.Tick(3f);
        Assert.That(manager.TryStartPlayer(task.Id), Is.True);
        manager.Tick(5f);

        Assert.That(task.RemainingLifetimeSec, Is.EqualTo(7f).Within(0.001f));
        Assert.That(task.CapturedTimeRatio, Is.EqualTo(0.7f).Within(0.001f));
    }

    [Test]
    public void Expiration_ResolvesOnlyOnce()
    {
        var manager = new TaskManager(new TaskManagerSettings());
        var task = manager.CreateTask(TaskKind.RapidClick, TaskSurface.Pc, 1, 1f);
        var resolutionCount = 0;
        manager.TaskResolved += _ => resolutionCount++;

        manager.Tick(1f);
        manager.Tick(1f);

        Assert.That(task.Resolution, Is.EqualTo(TaskResolution.Expired));
        Assert.That(resolutionCount, Is.EqualTo(1));
    }

    [Test]
    public void Session_GameOverTakesPriorityOverLaterTimeClear()
    {
        var session = new GameSession(new GameSessionSettings { DurationSec = 1f, MaxHp = 5, ExpiredDamage = 8 });
        var task = new TaskManager(new TaskManagerSettings()).CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 1f);

        session.Apply(new TaskResolutionResult(task, TaskResolution.Expired, 0f));
        session.Tick(1f);

        Assert.That(session.EndState, Is.EqualTo(GameEndState.GameOver));
    }

    [Test]
    public void SessionResult_PreservesAiUsageAndResolutionCounts()
    {
        var session = new GameSession(new GameSessionSettings());
        var task = new TaskManager(new TaskManagerSettings()).CreateTask(TaskKind.Tracing, TaskSurface.Pad, 2, 10f);

        session.Apply(new TaskResolutionResult(task, TaskResolution.AiSuccess, 1f));
        var result = session.CreateResult();

        Assert.That(result.AiUsedCount, Is.EqualTo(1));
        Assert.That(result.ResolutionCounts[TaskResolution.AiSuccess], Is.EqualTo(1));
    }
}
