using NUnit.Framework;

public class GameTuningSettingsTests
{
    [Test]
    public void TaskLevelMilestones_ApplyTheLatestReachedLevel()
    {
        var profile = new GameTuningSettings.DifficultyProfile
        {
            startingTaskLevel = 1,
            maxTaskLevel = 4,
            taskLevelMilestones =
            {
                new GameTuningSettings.DifficultyProfile.TaskLevelMilestone { elapsedSec = 0f, level = 1 },
                new GameTuningSettings.DifficultyProfile.TaskLevelMilestone { elapsedSec = 60f, level = 2 },
                new GameTuningSettings.DifficultyProfile.TaskLevelMilestone { elapsedSec = 120f, level = 3 },
                new GameTuningSettings.DifficultyProfile.TaskLevelMilestone { elapsedSec = 150f, level = 4 }
            }
        };

        Assert.That(profile.GetTaskLevel(59.9f), Is.EqualTo(1));
        Assert.That(profile.GetTaskLevel(60f), Is.EqualTo(2));
        Assert.That(profile.GetTaskLevel(120f), Is.EqualTo(3));
        Assert.That(profile.GetTaskLevel(150f), Is.EqualTo(4));
    }

    [Test]
    public void EmptyTaskLevelMilestones_UseTheLegacyInterval()
    {
        var profile = new GameTuningSettings.DifficultyProfile
        {
            startingTaskLevel = 1,
            maxTaskLevel = 4,
            taskLevelIncreaseIntervalSec = 30f
        };

        Assert.That(profile.GetTaskLevel(0f), Is.EqualTo(1));
        Assert.That(profile.GetTaskLevel(30f), Is.EqualTo(2));
        Assert.That(profile.GetTaskLevel(95f), Is.EqualTo(4));
    }

    [Test]
    public void SpawnIntervalMilestones_ApplyTheLatestReachedInterval()
    {
        var profile = new GameTuningSettings.DifficultyProfile
        {
            spawnIntervalSec = 5f,
            spawnIntervalMilestones =
            {
                new GameTuningSettings.DifficultyProfile.SpawnIntervalMilestone { elapsedSec = 60f, intervalSec = 4f },
                new GameTuningSettings.DifficultyProfile.SpawnIntervalMilestone { elapsedSec = 120f, intervalSec = 3f }
            }
        };

        Assert.That(profile.GetSpawnInterval(0f), Is.EqualTo(5f));
        Assert.That(profile.GetSpawnInterval(60f), Is.EqualTo(4f));
        Assert.That(profile.GetSpawnInterval(150f), Is.EqualTo(3f));
    }

    [Test]
    public void EmptySpawnIntervalMilestones_UseTheBaseInterval()
    {
        var profile = new GameTuningSettings.DifficultyProfile { spawnIntervalSec = 4.5f };

        Assert.That(profile.GetSpawnInterval(100f), Is.EqualTo(4.5f));
    }
}
