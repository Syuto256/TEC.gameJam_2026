using NUnit.Framework;

public class GameSessionComboTests
{
    /// <summary>時間ボーナスを 0 に固定し、コンボ倍率だけがスコアに出る設定を作る。</summary>
    private static GameSessionSettings CreateSettings(float addPerCombo = 0.1f, float maxMultiplier = 2f)
    {
        return new GameSessionSettings
        {
            BaseScoreLevel1 = 100,
            MaxTimeBonusAdd = 0f,
            ComboScoreAddPerCombo = addPerCombo,
            MaxComboMultiplier = maxMultiplier
        };
    }

    private static TaskInstance CreateTask()
    {
        return new TaskManager(new TaskManagerSettings()).CreateTask(TaskKind.Typing, TaskSurface.Pc, 1, 10f);
    }

    private static int ApplyPlayerResult(GameSession session, TaskResolution resolution)
    {
        return session.Apply(new TaskResolutionResult(CreateTask(), resolution, 0f));
    }

    [Test]
    public void ConsecutivePlayerSuccess_RaisesComboAndScore()
    {
        var session = new GameSession(CreateSettings());

        Assert.That(ApplyPlayerResult(session, TaskResolution.PlayerSuccess), Is.EqualTo(100));
        Assert.That(ApplyPlayerResult(session, TaskResolution.PlayerSuccess), Is.EqualTo(110));
        Assert.That(ApplyPlayerResult(session, TaskResolution.PlayerSuccess), Is.EqualTo(120));
        Assert.That(session.ComboCount, Is.EqualTo(3));
        Assert.That(session.Score, Is.EqualTo(330));
    }

    [Test]
    public void FailureAndExpiry_ResetTheCombo()
    {
        var session = new GameSession(CreateSettings());

        ApplyPlayerResult(session, TaskResolution.PlayerSuccess);
        ApplyPlayerResult(session, TaskResolution.PlayerSuccess);
        Assert.That(session.ComboCount, Is.EqualTo(2));

        ApplyPlayerResult(session, TaskResolution.PlayerFailure);
        Assert.That(session.ComboCount, Is.EqualTo(0));

        Assert.That(ApplyPlayerResult(session, TaskResolution.PlayerSuccess), Is.EqualTo(100));

        ApplyPlayerResult(session, TaskResolution.Expired);
        Assert.That(session.ComboCount, Is.EqualTo(0));
    }

    [Test]
    public void ComboMultiplier_StopsAtTheConfiguredCap()
    {
        var session = new GameSession(CreateSettings(maxMultiplier: 1.2f));

        ApplyPlayerResult(session, TaskResolution.PlayerSuccess);
        ApplyPlayerResult(session, TaskResolution.PlayerSuccess);

        Assert.That(ApplyPlayerResult(session, TaskResolution.PlayerSuccess), Is.EqualTo(120));
        Assert.That(ApplyPlayerResult(session, TaskResolution.PlayerSuccess), Is.EqualTo(120));
    }

    /// <summary>コンボ加算を 0 にすると、コンボが伸びてもスコアが変わらないこと。</summary>
    [Test]
    public void ZeroComboAdd_KeepsScoreFlat()
    {
        var session = new GameSession(CreateSettings(addPerCombo: 0f));

        Assert.That(ApplyPlayerResult(session, TaskResolution.PlayerSuccess), Is.EqualTo(100));
        Assert.That(ApplyPlayerResult(session, TaskResolution.PlayerSuccess), Is.EqualTo(100));
    }
}
