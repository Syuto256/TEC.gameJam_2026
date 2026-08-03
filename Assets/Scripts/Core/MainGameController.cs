using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Game シーンでセッション、タスク生成、AI 処理、HUD、終了遷移を接続する。</summary>
public sealed class MainGameController : MonoBehaviour
{
    private static readonly TaskKind[] SpawnKinds = { TaskKind.Typing, TaskKind.Tracing, TaskKind.RapidClick, TaskKind.DragDrop };

    private readonly Dictionary<int, TaskBubbleView> taskViews = new Dictionary<int, TaskBubbleView>();
    private GameTuningSettings tuningSettings;
    private GameTuningSettings.DifficultyProfile difficultyProfile;
    private TaskManager taskManager;
    private GameSession session;
    private TextMeshProUGUI hudText;
    private Transform pcTaskSpawnArea;
    private Transform padTaskSpawnArea;
    private GameObject miniGameHost;
    private TextMeshProUGUI miniGameHostText;
    private GameObject pausePanel;
    private IPlayerMiniGameLauncher[] miniGameLaunchers;
    private int activePlayerTaskId = -1;
    private float spawnElapsedSec;
    private int nextTaskKindIndex;
    private bool initialized;
    private bool ending;
    private bool paused;

    public GameSession Session => session;
    public TaskManager TaskManager => taskManager;
    public bool IsPaused => paused;

    public void Initialize(
        GameTuningSettings settings,
        TextMeshProUGUI hud,
        Transform pcSpawnArea,
        Transform padSpawnArea,
        GameObject host,
        GameObject pause,
        IPlayerMiniGameLauncher[] launchers)
    {
        if (initialized)
        {
            return;
        }

        if (settings == null)
        {
            Debug.LogError("MainGameController requires GameTuningSettings.");
            return;
        }

        tuningSettings = settings;
        hudText = hud;
        pcTaskSpawnArea = pcSpawnArea;
        padTaskSpawnArea = padSpawnArea;
        miniGameHost = host;
        miniGameHostText = host.GetComponentInChildren<TextMeshProUGUI>(true);
        pausePanel = pause;
        miniGameLaunchers = launchers ?? System.Array.Empty<IPlayerMiniGameLauncher>();

        var flow = GameFlowController.EnsureInstance();
        difficultyProfile = tuningSettings.GetDifficultyProfile(flow.SelectedDifficulty);
        taskManager = new TaskManager(new TaskManagerSettings
        {
            AiSuccessRate = tuningSettings.ai.successRate,
            AiProcessDurationSec = tuningSettings.ai.processDurationSec,
            AiCooldownSec = tuningSettings.ai.cooldownSec
        });
        taskManager.TaskResolved += OnTaskResolved;
        session = new GameSession(new GameSessionSettings
        {
            Difficulty = flow.SelectedDifficulty,
            IsEndless = difficultyProfile.isEndless || flow.SelectedDifficulty == GameDifficulty.Endless,
            DurationSec = difficultyProfile.durationSec,
            MaxHp = difficultyProfile.maxHp,
            PlayerFailureDamage = tuningSettings.damage.playerFail,
            AiFailureDamage = tuningSettings.damage.aiFail,
            ExpiredDamage = tuningSettings.damage.expired,
            AiScoreMultiplier = tuningSettings.ai.scoreMultiplier,
            BaseScoreLevel1 = tuningSettings.score.baseScoreDiff1,
            BaseScoreLevel2 = tuningSettings.score.baseScoreDiff2,
            BaseScoreLevel3 = tuningSettings.score.baseScoreDiff3,
            BaseScoreLevel4 = tuningSettings.score.baseScoreDiff4,
            MaxTimeBonusAdd = tuningSettings.score.maxTimeBonusAdd
        });

        initialized = true;
        RefreshHud();
    }

    private void Update()
    {
        if (!initialized || ending || paused)
        {
            return;
        }

        var deltaTime = Time.deltaTime;
        taskManager.Tick(deltaTime);
        session.Tick(deltaTime);
        if (session.EndState != GameEndState.Playing)
        {
            FinishSession();
            return;
        }

        spawnElapsedSec += deltaTime;
        var spawnInterval = Mathf.Max(0.1f, difficultyProfile.spawnIntervalSec);
        if (spawnElapsedSec >= spawnInterval)
        {
            spawnElapsedSec -= spawnInterval;
            TrySpawnTask();
        }

        RefreshTaskViews();
        RefreshHud();
    }

    public bool TryAssignPlayer(int taskId)
    {
        if (!initialized || ending || paused || !taskManager.TryGetTask(taskId, out var task) || task.State != TaskState.Available)
        {
            return false;
        }

        var launcher = FindLauncher(task.Kind);
        if (launcher == null || !launcher.IsReady)
        {
            Debug.LogError("Mini-game launcher or its data is missing for " + task.Kind + ".");
            return false;
        }

        if (!taskManager.TryStartPlayer(taskId))
        {
            return false;
        }

        if (taskViews.TryGetValue(taskId, out var view))
        {
            view.Refresh();
        }

        miniGameHost.SetActive(true);
        if (task.Kind == TaskKind.Typing || task.Kind == TaskKind.Tracing || task.Kind == TaskKind.RapidClick || task.Kind == TaskKind.DragDrop)
        {
            activePlayerTaskId = taskId;
            var timeLimit = task.Kind == TaskKind.Typing ? tuningSettings.miniGameTimes.typing :
                task.Kind == TaskKind.Tracing ? tuningSettings.miniGameTimes.tracing :
                task.Kind == TaskKind.RapidClick ? tuningSettings.miniGameTimes.rapidClick : tuningSettings.miniGameTimes.dragDrop;
            if (!launcher.TryStart(miniGameHost, task.Level, timeLimit, (success, reason) => CompletePlayerMiniGame(taskId, success)))
            {
                activePlayerTaskId = -1;
                taskManager.CompletePlayer(taskId, false);
                return false;
            }
        }
        else
        {
            miniGameHostText.gameObject.SetActive(true);
            miniGameHostText.text = "SELF TASK RESERVED\n" + task.Kind + "  Lv." + task.Level + "\n" +
                "The mini-game will connect here in M5.";
        }
        return true;
    }

    public bool TryAssignAi(int taskId)
    {
        if (!initialized || ending || paused || !taskManager.TryRequestAi(taskId))
        {
            return false;
        }

        if (taskViews.TryGetValue(taskId, out var view))
        {
            view.Refresh();
        }

        return true;
    }

    public void TogglePause()
    {
        SetPaused(!paused);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void ReturnToDifficultySelect()
    {
        SetPaused(false);
        GameFlowController.EnsureInstance().OpenDifficultySelect();
    }

    private void TrySpawnTask()
    {
        var pcCount = CountActiveTasks(TaskSurface.Pc);
        var padCount = CountActiveTasks(TaskSurface.Pad);
        var maxTasks = Mathf.Max(1, difficultyProfile.maxTasksPerSurface);
        if (pcCount >= maxTasks && padCount >= maxTasks)
        {
            return;
        }

        var surface = pcCount <= padCount && pcCount < maxTasks ? TaskSurface.Pc : TaskSurface.Pad;
        var level = CalculateTaskLevel();
        var kind = SpawnKinds[nextTaskKindIndex++ % SpawnKinds.Length];
        var task = taskManager.CreateTask(kind, surface, level, difficultyProfile.taskLifetimeSec);
        var parent = surface == TaskSurface.Pc ? pcTaskSpawnArea : padTaskSpawnArea;
        taskViews.Add(task.Id, TaskBubbleView.Create(parent, this, task));
    }

    private int CountActiveTasks(TaskSurface surface)
    {
        var count = 0;
        foreach (var task in taskManager.Tasks)
        {
            if (task.Surface == surface && !task.IsTerminal)
            {
                count++;
            }
        }

        return count;
    }

    private int CalculateTaskLevel()
    {
        var level = Mathf.Clamp(difficultyProfile.startingTaskLevel, 1, 4);
        var maximum = Mathf.Clamp(difficultyProfile.maxTaskLevel, level, 4);
        if (difficultyProfile.taskLevelIncreaseIntervalSec <= 0f)
        {
            return maximum;
        }

        var increases = Mathf.FloorToInt(taskManager.ElapsedSec / difficultyProfile.taskLevelIncreaseIntervalSec);
        return Mathf.Clamp(level + increases, level, maximum);
    }

    private void OnTaskResolved(TaskResolutionResult result)
    {
        session.Apply(result);
        if (activePlayerTaskId == result.Task.Id)
        {
            activePlayerTaskId = -1;
            miniGameHost.SetActive(false);
        }
        if (taskViews.TryGetValue(result.Task.Id, out var view))
        {
            taskViews.Remove(result.Task.Id);
            Destroy(view.gameObject);
        }
    }

    private void CompletePlayerMiniGame(int taskId, bool success)
    {
        if (!initialized || activePlayerTaskId != taskId)
        {
            return;
        }

        AudioManager.PlaySfx(success ? AudioCue.MiniGameSuccess : AudioCue.MiniGameFailure);
        taskManager.CompletePlayer(taskId, success);
    }

    private IPlayerMiniGameLauncher FindLauncher(TaskKind kind)
    {
        foreach (var launcher in miniGameLaunchers)
        {
            if (launcher != null && launcher.Kind == kind)
            {
                return launcher;
            }
        }

        return null;
    }

    private void RefreshTaskViews()
    {
        foreach (var view in taskViews.Values)
        {
            view.Refresh();
        }
    }

    private void RefreshHud()
    {
        if (hudText == null || session == null)
        {
            return;
        }

        var totalSeconds = Mathf.CeilToInt(session.RemainingTimeSec);
        var time = session.Difficulty == GameDifficulty.Endless
            ? "--:--"
            : (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
        hudText.text = "HP " + session.Hp + "     SCORE " + session.Score + "     TIME " + time + "     " + session.Difficulty;
    }

    private void FinishSession()
    {
        ending = true;
        Time.timeScale = 1f;
        GameFlowController.EnsureInstance().PresentResult(session.CreateResult());
    }

    private void SetPaused(bool value)
    {
        if (!initialized || ending)
        {
            return;
        }

        paused = value;
        Time.timeScale = paused ? 0f : 1f;
        pausePanel.SetActive(paused);
    }

    private void OnDestroy()
    {
        if (taskManager != null)
        {
            taskManager.TaskResolved -= OnTaskResolved;
        }

        Time.timeScale = 1f;
    }
}
