using System.Collections.Generic;
using UnityEngine;

/// <summary>Game シーンでセッション、タスク生成、AI 処理、HUD、終了遷移を接続する。</summary>
public sealed class MainGameController : MonoBehaviour
{
    private static readonly TaskKind[] SpawnKinds = { TaskKind.Typing, TaskKind.Tracing, TaskKind.RapidClick, TaskKind.DragDrop };

    [Tooltip("タスク吹き出しの見た目はこの Prefab で調整する。")]
    [SerializeField] private TaskBubbleView taskBubblePrefab;

    [Tooltip("ミニゲームの登録簿。追加・差し替えはこのアセットだけで完結する。")]
    [SerializeField] private MiniGameCatalog miniGameCatalog;

    private readonly Dictionary<int, TaskBubbleView> taskViews = new Dictionary<int, TaskBubbleView>();
    private GameTuningSettings tuningSettings;
    private GameTuningSettings.DifficultyProfile difficultyProfile;
    private TaskManager taskManager;
    private GameSession session;
    private HudView hudView;
    private DeviceWorkspaceView[] workspaces;
    private MiniGameHostView miniGameHost;
    private PauseMenuView pauseMenu;
    private int activePlayerTaskId = -1;
    private float spawnElapsedSec;
    private int nextTaskKindIndex;
    private bool initialized;
    private bool ending;
    private bool paused;

    public GameSession Session => session;
    public TaskManager TaskManager => taskManager;
    public bool IsPaused => paused;

    /// <summary>自力ミニゲームの進行中・終了を通知する。デバイス切替の可否に使う。</summary>
    public event System.Action<bool> PlayerMiniGameActiveChanged;

    public void Initialize(
        GameTuningSettings settings,
        HudView hud,
        DeviceWorkspaceView[] deviceWorkspaces,
        MiniGameHostView host,
        PauseMenuView pause)
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

        if (hud == null || host == null || pause == null)
        {
            Debug.LogError("MainGameController requires HudView, MiniGameHostView, and PauseMenuView.");
            return;
        }

        workspaces = deviceWorkspaces ?? System.Array.Empty<DeviceWorkspaceView>();
        if (ResolveSpawnArea(TaskSurface.Pc) == null || ResolveSpawnArea(TaskSurface.Pad) == null)
        {
            Debug.LogError("MainGameController requires a DeviceWorkspaceView with a spawn area for Pc and for Pad.", this);
            return;
        }

        if (taskBubblePrefab == null)
        {
            Debug.LogError("MainGameController requires a TaskBubbleView prefab.", this);
            return;
        }

        if (!taskBubblePrefab.ValidateReferences())
        {
            return;
        }

        if (miniGameCatalog == null)
        {
            Debug.LogError("MainGameController requires a MiniGameCatalog.", this);
            return;
        }

        if (!miniGameCatalog.Validate())
        {
            return;
        }

        tuningSettings = settings;
        hudView = hud;
        miniGameHost = host;
        pauseMenu = pause;

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

        if (!miniGameCatalog.TryGetEntry(task.Kind, out var entry) || entry.prefab == null)
        {
            Debug.LogError("MiniGameCatalog に " + task.Kind + " の Prefab が登録されていません。", this);
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

        miniGameHost.Show();
        SetActivePlayerTask(taskId);

        var miniGame = miniGameHost.Spawn(entry.prefab);
        if (miniGame == null)
        {
            SetActivePlayerTask(-1);
            miniGameHost.Hide();
            taskManager.CompletePlayer(taskId, false);
            return false;
        }

        // 生成物の破棄は TaskResolved -> miniGameHost.Hide() が担当する。
        miniGame.OnCompleted += (success, reason) => CompletePlayerMiniGame(taskId, success);
        miniGame.Initialize(task.Level, entry.GetTimeLimit(task.Level));
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
        var parent = ResolveSpawnArea(surface);
        if (parent == null)
        {
            return;
        }

        var task = taskManager.CreateTask(kind, surface, level, difficultyProfile.taskLifetimeSec);
        var bubble = Instantiate(taskBubblePrefab, parent, false);
        bubble.name = "TaskBubble_" + task.Id;
        miniGameCatalog.TryGetEntry(kind, out var entry);
        bubble.Bind(this, task, entry?.displayName, entry?.icon);
        taskViews.Add(task.Id, bubble);
    }

    /// <summary>自力ミニゲームの担当タスクを更新し、開始・終了の変化だけを通知する。</summary>
    private void SetActivePlayerTask(int taskId)
    {
        var wasActive = activePlayerTaskId >= 0;
        activePlayerTaskId = taskId;
        var isActive = activePlayerTaskId >= 0;
        if (wasActive != isActive)
        {
            PlayerMiniGameActiveChanged?.Invoke(isActive);
        }
    }

    private RectTransform ResolveSpawnArea(TaskSurface surface)
    {
        foreach (var workspace in workspaces)
        {
            if (workspace != null && workspace.Surface == surface)
            {
                return workspace.PickSpawnArea();
            }
        }

        return null;
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
            SetActivePlayerTask(-1);
            miniGameHost.Hide();
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

    private void RefreshTaskViews()
    {
        foreach (var view in taskViews.Values)
        {
            view.Refresh();
        }
    }

    private void RefreshHud()
    {
        if (hudView == null || session == null)
        {
            return;
        }

        hudView.Render(new HudSnapshot(
            session.Hp,
            session.MaxHp,
            session.Score,
            session.RemainingTimeSec,
            session.IsEndless,
            session.Difficulty));
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
        pauseMenu.SetVisible(paused);
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
