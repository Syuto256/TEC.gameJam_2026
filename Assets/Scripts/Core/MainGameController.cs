using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>Game シーンでセッション、タスク生成、AI 処理、HUD、終了遷移を接続する。</summary>
public sealed class MainGameController : MonoBehaviour
{
    [Tooltip("タスク吹き出しの見た目はこの Prefab で調整する。")]
    [SerializeField] private TaskBubbleView taskBubblePrefab;

    [Tooltip("ミニゲームの登録簿。追加・差し替えはこのアセットだけで完結する。")]
    [SerializeField] private MiniGameCatalog miniGameCatalog;

    [Tooltip("デバイス面ごとの出現タスク。どの面に何が出るかはこのアセットで決める。")]
    [SerializeField] private TaskSpawnTable taskSpawnTable;

    [Tooltip("タスクの決着を吹き出しの位置に知らせる層。未設定でも進行には影響しない（演出が出ないだけ）。")]
    [SerializeField] private ResultEffectLayerView resultEffectLayer;

    [Tooltip("画面に出しきれず待機しているタスクの件数表示。未設定でも進行には影響しない（件数が出ないだけ）。")]
    [SerializeField] private TaskBacklogView taskBacklogView;

    [Header("【タスク生成設定】")]
    [Tooltip("通常の自動タスク生成（定期発生）を有効にするか。チュートリアル等では false にする")]
    [SerializeField] private bool enableAutoSpawn = true;
    [Tooltip("タスク制限時間の上書き（秒）。0より大きい値を入れるとその秒数で固定される。チュートリアルで無制限にする場合は 9999 等を設定")]
    [SerializeField] private float overrideTaskLifetimeSec = 0f;

    [Header("【一斉飛来（ラッシュ）イベント設定】")]
    [Tooltip("一斉飛来（ラッシュ）イベントを有効にするか")]
    [SerializeField] private bool enableTaskRush = true;

    [Tooltip("ラッシュ発生の間隔（最小秒数）")]
    [Min(1f)] [SerializeField] private float minRushIntervalSec = 20f;

    [Tooltip("ラッシュ発生の間隔（最大秒数）")]
    [Min(1f)] [SerializeField] private float maxRushIntervalSec = 35f;

    [Tooltip("一斉に発生させるタスクの数")]
    [Min(2)] [SerializeField] private int rushTaskCount = 3;

    private float rushElapsedSec;
    private float nextRushIntervalSec;

    [Header("【音】")]
    [Tooltip("HP がこの割合を下回ったら警告音を一度だけ鳴らす。")]
    [Range(0f, 1f)] [SerializeField] private float hpLowRatio = 0.3f;

    private readonly Dictionary<int, TaskBubbleView> taskViews = new Dictionary<int, TaskBubbleView>();
    private readonly Dictionary<TaskSurface, int> nextKindIndexBySurface = new Dictionary<TaskSurface, int>();
    private GameTuningSettings tuningSettings;
    private GameTuningSettings.DifficultyProfile difficultyProfile;
    private TaskManager taskManager;
    private GameSession session;
    private HudView hudView;
    private DeviceWorkspaceView[] workspaces;
    private MiniGameHostView miniGameHost;
    private PauseMenuView pauseMenu;
    private MiniGameBase activeMiniGame;
    private int activePlayerTaskId = -1;
    private float spawnElapsedSec;
    private bool hpLowNotified;
    private bool initialized;
    private bool ending;
    private bool paused;

    public GameSession Session => session;
    public TaskManager TaskManager => taskManager;
    public bool IsPaused => paused;
    /// <summary>★追加: チュートリアル用：プレイヤーの自力ミニゲーム開始（左クリック）を禁止するフラグ</summary>
    public bool ForceAiOnlyMode { get; set; }

    /// <summary>自力ミニゲームの進行中・終了を通知する。デバイス切替の可否に使う。</summary>
    public event System.Action<bool> PlayerMiniGameActiveChanged;
    public event System.Action<TaskResolutionResult> TaskResolved;

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

        if (taskSpawnTable == null)
        {
            Debug.LogError("MainGameController requires a TaskSpawnTable.", this);
            return;
        }

        if (!taskSpawnTable.Validate(miniGameCatalog))
        {
            return;
        }

        if (!HasSpawnableSurface(deviceWorkspaces))
        {
            Debug.LogError(
                "MainGameController: TaskSpawnTable にタスクが設定された面が、workspaces に 1 つもありません。", this);
            return;
        }

        tuningSettings = settings;
        hudView = hud;
        miniGameHost = host;
        pauseMenu = pause;

        if (resultEffectLayer != null && !resultEffectLayer.Initialize())
        {
            return;
        }

        if (taskBacklogView != null && !taskBacklogView.Initialize())
        {
            return;
        }

        var flow = GameFlowController.EnsureInstance();
        difficultyProfile = tuningSettings.GetDifficultyProfile(flow.SelectedDifficulty);
        WarnIfSlotsAreFewerThanVisibleLimit();
        taskManager = new TaskManager(new TaskManagerSettings
        {
            // ★修正: チュートリアル時（overrideTaskLifetimeSec > 0）は AI 成功率を 100% (1f) にして絶対成功させる
            AiSuccessRate = overrideTaskLifetimeSec > 0f ? 1f : tuningSettings.ai.successRate,
            AiProcessDurationSec = tuningSettings.ai.processDurationSec,
            AiCooldownSec = tuningSettings.ai.cooldownSec,
            MaxVisibleTasksPerSurface = difficultyProfile.maxTasksPerSurface,
            MaxQueuedTasksPerSurface = tuningSettings.taskQueue.maxQueuedPerSurface,
            QueuedLifetimeTicks = tuningSettings.taskQueue.lifetimeTicksWhileQueued
        });
        taskManager.TaskResolved += OnTaskResolved;
        taskManager.TaskShown += OnTaskShown;
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
            MaxTimeBonusAdd = tuningSettings.score.maxTimeBonusAdd,
            ComboScoreAddPerCombo = tuningSettings.score.comboScoreAddPerCombo,
            MaxComboMultiplier = tuningSettings.score.maxComboMultiplier
        });

        // ★ 追加: 最初のラッシュタイマーをセット
        ResetNextRushInterval();

        initialized = true;
        RefreshHud();
        RefreshBacklog();
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

        // ★修正: enableAutoSpawn が true の時だけ自動生成のタイマーを進める
        if (enableAutoSpawn)
        {
            spawnElapsedSec += deltaTime;
            var spawnInterval = difficultyProfile.GetSpawnInterval(taskManager.ElapsedSec);
            if (spawnElapsedSec >= spawnInterval)
            {
                spawnElapsedSec -= spawnInterval;
                TrySpawnTask();
            }
        }

        // ★ 追加: ラッシュイベントの経過計測と発生判定
        if (enableTaskRush)
        {
            rushElapsedSec += deltaTime;
            if (rushElapsedSec >= nextRushIntervalSec)
            {
                TriggerTaskRush(rushTaskCount);
                ResetNextRushInterval();
            }
        }

        UpdateHpLowCue();
        RefreshTaskViews();
        RefreshHud();
        RefreshBacklog();
    }

    /// <summary>HP が危険域へ入った瞬間に一度だけ警告音を鳴らす。</summary>
    private void UpdateHpLowCue()
    {
        if (session.MaxHp <= 0)
        {
            return;
        }

        var ratio = (float)session.Hp / session.MaxHp;
        if (!hpLowNotified && ratio <= hpLowRatio)
        {
            hpLowNotified = true;
            AudioManager.PlaySfx(AudioCue.HpLow);
        }
        else if (hpLowNotified && ratio > hpLowRatio)
        {
            hpLowNotified = false;
        }
    }

    /// <summary>タスクを自分で片付ける。ミニゲームの窓を開く。</summary>
    /// <remarks>
    /// **すでに 1 つ抱えているあいだは受け付けない。**
    /// <see cref="MiniGameHostView.Spawn"/> は窓の中身を捨てて入れ替えるため、
    /// 2 つ目を開くと 1 つ目が黙って消える。決着の購読だけが残り、
    /// 消えたはずのミニゲームがタスクを片付けてしまう。
    /// <para>
    /// 決着を見せているあいだも <c>activePlayerTaskId</c> は落ちない
    /// （<see cref="CloseMiniGame"/> を参照）。そのため結果表示の途中で
    /// 次のミニゲームが始まることもない。
    /// </para>
    /// </remarks>
    public bool TryAssignPlayer(int taskId)
    {
        // AI へ右クリックで誘導している最中は、左クリックで始めさせない。
        if (ForceAiOnlyMode)
        {
            return false;
        }

        if (activePlayerTaskId >= 0)
        {
            return false;
        }

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
        string taskName = !string.IsNullOrEmpty(entry.displayName) ? entry.displayName : task.Kind.ToString();
        hudView.ShowCurrentTaskName(taskName);
        var miniGame = miniGameHost.Spawn(entry.prefab);
        if (miniGame == null)
        {
            SetActivePlayerTask(-1);
            miniGameHost.Hide();
            hudView.HideCurrentTaskName();
            taskManager.CompletePlayer(taskId, false);
            return false;
        }

        // 生成物の破棄は TaskResolved -> CloseMiniGame() が担当する。
        // 決着表示の秒数をそこで読むため、実体を控えておく。
        activeMiniGame = miniGame;

        // **成功も失敗もそのまま通す。チュートリアルでも例外を作らない。**
        // 以前はチュートリアル中の失敗をここで捨てていたが、
        // <see cref="MiniGameBase.FinishGame"/> は通知より先に IsPlaying を false にするため、
        // 捨てた時点でミニゲームは既に終わっている。決着が誰にも届かず、窓が閉じないまま
        // 操作も止まったままになり、そこから進めなくなっていた。
        // 失敗したときの出題し直しは TutorialSequenceController.OnTaskResolved が持っている。
        miniGame.OnCompleted += (success, reason) => CompletePlayerMiniGame(taskId, success);

        // 時間は 9999 などの極端な数値ではなく 99 秒程度にして計算崩れを防ぐ
        var timeLimit = overrideTaskLifetimeSec > 0f ? 99f : entry.GetTimeLimit(task.Level);
        miniGame.Initialize(task.Level, timeLimit);
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

        AudioManager.PlaySfx(AudioCue.AiRequested);
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
        if (!TryPickSpawnSurface(out var surface) || !taskSpawnTable.TryGetKinds(surface, out var kinds))
        {
            return;
        }

        var level = CalculateTaskLevel();
        var kind = kinds[NextKindIndex(surface) % kinds.Length];
        var parent = ResolveSpawnArea(surface);
        if (parent == null)
        {
            return;
        }

        var lifetime = overrideTaskLifetimeSec > 0f ? overrideTaskLifetimeSec : difficultyProfile.taskLifetimeSec;

        // ★ 修正: 最後の引数を difficultyProfile... から lifetime に変更
        taskManager.CreateTask(kind, surface, level, lifetime);
    }

    // ★ 追加: ラッシュ実行メソッド
    public void TriggerTaskRush(int count)
    {
        for (var i = 0; i < count; i++)
        {
            TrySpawnTask();
        }

        Debug.Log($"[Task Rush] タスクが一斉に {count} 個発生しました！");
    }

    
    /// <summary>★追加: チュートリアル用：指定したレベルと寿命でタスクを1つ生成する</summary>
    public void SpawnCustomTask(int level, float customLifetimeSec)
    {
        if (!TryPickSpawnSurface(out var surface) || !taskSpawnTable.TryGetKinds(surface, out var kinds))
        {
            return;
        }

        var kind = kinds[NextKindIndex(surface) % kinds.Length];
        var parent = ResolveSpawnArea(surface);
        if (parent == null)
        {
            return;
        }

        // 指定された寿命（3秒や99秒など）で生成
        taskManager.CreateTask(kind, surface, level, customLifetimeSec);
    }

    /// <summary>★追加: チュートリアル用：指定した面（PC/Pad等）・レベル・寿命でタスクを生成する</summary>
    public void SpawnCustomTaskOnSurface(TaskSurface surface, int level, float customLifetimeSec)
    {
        if (!taskSpawnTable.TryGetKinds(surface, out var kinds))
        {
            return;
        }

        var kind = kinds[NextKindIndex(surface) % kinds.Length];
        var parent = ResolveSpawnArea(surface);
        if (parent == null)
        {
            return;
        }

        taskManager.CreateTask(kind, surface, level, customLifetimeSec);
    }

    // ★ 追加: 次のラッシュ時間のリセット処理
    private void ResetNextRushInterval()
    {
        rushElapsedSec = 0f;
        var minSec = Mathf.Min(minRushIntervalSec, maxRushIntervalSec);
        var maxSec = Mathf.Max(minRushIntervalSec, maxRushIntervalSec);
        // using System; を足したため Random だけでは System.Random と紛れる。
        nextRushIntervalSec = UnityEngine.Random.Range(minSec, maxSec);
    }

    /// <summary>タスクが画面に出るときに吹き出しを作る。発生直後とは限らない（待機列からの繰り上げを含む）。</summary>
    private void OnTaskShown(TaskInstance task)
    {
        var parent = ResolveSpawnArea(task.Surface);
        if (parent == null)
        {
            return;
        }

        var bubble = Instantiate(taskBubblePrefab, parent, false);
        bubble.name = "TaskBubble_" + task.Id;
        miniGameCatalog.TryGetEntry(task.Kind, out var entry);
        bubble.Bind(this, task, entry?.GetBubbleSprite(task.Level));
        taskViews.Add(task.Id, bubble);
        AudioManager.PlaySfx(AudioCue.TaskSpawned);
    }

    /// <summary>
    /// タスクを出す面を選ぶ。出現タスクが設定されていない面と、上限に達している面は対象外にする。
    /// 候補が複数ある場合は未解決タスクの少ない面を選ぶ。デバイス面が 3 つ以上でもそのまま動く。
    /// </summary>
    private bool TryPickSpawnSurface(out TaskSurface surface)
    {
        surface = default;
        var fewest = int.MaxValue;
        var found = false;

        foreach (var workspace in workspaces)
        {
            if (workspace == null || !taskSpawnTable.HasAnyKind(workspace.Surface))
            {
                continue;
            }

            // 表示上限に達していても待機列があるので、ここで弾くのは待機列が満杯の面だけ。
            var count = CountActiveTasks(workspace.Surface);
            if (!taskManager.CanAcceptTask(workspace.Surface) || count >= fewest)
            {
                continue;
            }

            fewest = count;
            surface = workspace.Surface;
            found = true;
        }

        return found;
    }

    /// <summary>面ごとに独立した順番でタスク種別を選ぶ。</summary>
    private int NextKindIndex(TaskSurface surface)
    {
        nextKindIndexBySurface.TryGetValue(surface, out var index);
        nextKindIndexBySurface[surface] = index + 1;
        return index;
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
        
            // ★ 追加: 自力ミニゲームが終了したら（isActive == false）表示を非表示にする
            if (!isActive)
            {
                hudView.HideCurrentTaskName();
            }
        }
    }

    /// <summary>出現タスクが設定された面が、実際に置かれているデバイス面の中に 1 つ以上あるか。</summary>
    private bool HasSpawnableSurface(DeviceWorkspaceView[] deviceWorkspaces)
    {
        foreach (var workspace in deviceWorkspaces)
        {
            if (workspace != null && taskSpawnTable.HasAnyKind(workspace.Surface))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>決着の演出を出す。待ち時間が指定されていれば、その分だけ遅らせる。</summary>
    private void PlayResultEffect(Vector3 position, TaskResolution resolution, int addedScore, float delaySec)
    {
        if (resultEffectLayer == null)
        {
            return;
        }

        if (delaySec <= 0f)
        {
            resultEffectLayer.Play(position, resolution, addedScore);
            return;
        }

        // 遅らせているあいだにシーンが終わることがあるため、この GameObject に寿命を紐づける。
        DOVirtual.DelayedCall(delaySec, () => resultEffectLayer.Play(position, resolution, addedScore), false)
            .SetLink(gameObject);
    }

    /// <summary>枠が足りない面を報告する。足りないと、表示されるはずのタスクが画面に出ない。</summary>
    /// <remarks>
    /// 吹き出しの大きさと画面の空きから、1 面に置けるのは 4 つまで。
    /// 難易度設定の値だけ上げても枠は増えないため、ここで気づけるようにしている。
    /// </remarks>
    private void WarnIfSlotsAreFewerThanVisibleLimit()
    {
        var limit = Mathf.Max(1, difficultyProfile.maxTasksPerSurface);
        foreach (var workspace in workspaces)
        {
            if (workspace != null && workspace.SlotCount < limit)
            {
                Debug.LogError(
                    "MainGameController: " + workspace.Surface + " の枠が " + workspace.SlotCount
                    + " 個しかありませんが、同時に表示できる数は " + limit + " です。"
                    + "枠を増やすか、難易度設定の maxTasksPerSurface を下げてください。", workspace);
            }
        }
    }

    private RectTransform ResolveSpawnArea(TaskSurface surface)
    {
        foreach (var workspace in workspaces)
        {
            if (workspace != null && workspace.Surface == surface && workspace.TryPickFreeSlot(out var slot))
            {
                return slot;
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
        return difficultyProfile.GetTaskLevel(taskManager.ElapsedSec);
    }

    private void OnTaskResolved(TaskResolutionResult result)
    {
        // ★追加: 外部（チュートリアル等）へクリア通知を飛ばす
        TaskResolved?.Invoke(result);
        // ここで最新の ComboCount が確定する。
        var addedScore = session.Apply(result);
        PlayResolutionCue(result.Resolution);

        if (IsDamageResolution(result.Resolution))
        {
            PlayDamageShake();
            hudView.PlayDamageFlash();
        }

        // 自力成功。獲得点を見せ、節目とそれ以外で鳴らす音を切り替える。
        if (result.Resolution == TaskResolution.PlayerSuccess)
        {
            if (addedScore > 0)
            {
                hudView.ShowScorePopup(addedScore, session.ComboCount);
            }

            // 節目の音と通常の成功音は排他にする。同時に鳴らさない。
            if (IsComboMilestone(session.ComboCount))
            {
                AudioManager.PlaySfx(AudioCue.ComboMilestone);  // ★ 節目達成時のみ再生
            }
            else
            {
                AudioManager.PlaySfx(AudioCue.MiniGameSuccess); // ★ 通常クリア時のみ再生
            }
        }
        // ★ 自力ミニゲーム失敗時の処理（音再生をここへ移動）
        else if (result.Resolution == TaskResolution.PlayerFailure)
        {
            AudioManager.PlaySfx(AudioCue.MiniGameFailure);
        }

        var playerHoldSec = 0f;
        if (activePlayerTaskId == result.Task.Id)
        {
            playerHoldSec = activeMiniGame != null ? Mathf.Max(0f, activeMiniGame.ResultHoldSec) : 0f;
            CloseMiniGame(playerHoldSec);
        }

        if (taskViews.TryGetValue(result.Task.Id, out var view))
        {
            // 演出の位置は、吹き出しが消え始める前に控えておく。
            var effectPosition = view.transform.position;

            // 先に一覧から外すことで、消滅演出の間 Refresh の対象から外れる。破棄は View 自身が行う。
            taskViews.Remove(result.Task.Id);

            // AI の決着では吹き出しの上に結果が出る。同時に粒を飛ばすと読み取りが喧嘩するため、
            // 結果を見せ終わってから出す。待ち時間は吹き出し側が持っている値を使う。
            var holdSec = view.PlayExitAndDestroy(result.Resolution);

            // 自力の決着では窓の中に結果が出る。粒はその上へ重ねず、窓が閉じてから飛ばす。
            PlayResultEffect(effectPosition, result.Resolution, addedScore, holdSec + playerHoldSec);
        }
    }

    /// <summary>ミニゲームの窓を閉じる。決着表示があるときはその秒数だけ待つ。</summary>
    /// <remarks>
    /// **担当中の印を落とすのも一緒に遅らせる。** 先に落とすと、結果を出している最中に
    /// 次のミニゲームを始められてしまい、まだ読んでいない結果ごと窓が差し替わる
    /// （<see cref="TryAssignPlayer"/> はこの印を見て弾いている）。
    /// デバイス切替が戻るのも同じ理由で、決着を見せ終わってからにする。
    /// </remarks>
    private void CloseMiniGame(float delaySec)
    {
        activeMiniGame = null;

        if (delaySec <= 0f)
        {
            SetActivePlayerTask(-1);
            miniGameHost.Hide();
            return;
        }

        DOVirtual.DelayedCall(delaySec, () =>
        {
            SetActivePlayerTask(-1);
            miniGameHost.Hide();
        }, false).SetLink(gameObject);
    }

    /// <summary>HP が減る決着かどうか。</summary>
    private static bool IsDamageResolution(TaskResolution resolution)
    {
        return resolution == TaskResolution.PlayerFailure
            || resolution == TaskResolution.AiFailure
            || resolution == TaskResolution.Expired;
    }

    /// <summary>被弾を表す揺れを、表示中のデバイス面に出す。HUD は揺らさない。</summary>
    private void PlayDamageShake()
    {
        foreach (var workspace in workspaces)
        {
            if (workspace != null)
            {
                workspace.PlayDamageShake();
            }
        }
    }

    /// <summary>コンボ数が節目に達したか。間隔は GameTuningSettings の comboMilestoneInterval で変える。</summary>
    private bool IsComboMilestone(int combo)
    {
        var interval = tuningSettings.score.comboMilestoneInterval;
        return combo > 0 && interval > 0 && combo % interval == 0;
    }

    /// <summary>タスクの決着に応じた音を鳴らす。自力の成否は <see cref="CompletePlayerMiniGame"/> が鳴らす。</summary>
    private static void PlayResolutionCue(TaskResolution resolution)
    {
        switch (resolution)
        {
            case TaskResolution.Expired:
                AudioManager.PlaySfx(AudioCue.TaskExpired);
                break;
            case TaskResolution.AiSuccess:
                AudioManager.PlaySfx(AudioCue.AiSucceeded);
                break;
            case TaskResolution.AiFailure:
                AudioManager.PlaySfx(AudioCue.AiFailed);
                break;
        }
    }

    private void CompletePlayerMiniGame(int taskId, bool success)
    {
        // ending も見る。**ミニゲームは自前の Update で動いているため、
        // 時間切れや HP0 でセッションが終わったあとも余韻のあいだ動き続ける。**
        // ここを通すと、終了演出の最中に吹き出しが弾け、決着演出まで走ってしまう。
        if (!initialized || ending || activePlayerTaskId != taskId)
        {
            return;
        }

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
            session.Difficulty,
            session.ComboCount));
    }

    /// <summary>待機中のタスク件数を表示へ渡す。</summary>
    /// <remarks>
    /// 面ごとの内訳ではなく合計を渡す。どの面に溜まっているかは、その面へ切り替えれば
    /// 吹き出しの数で分かるためである。
    /// </remarks>
    private void RefreshBacklog()
    {
        if (taskBacklogView == null || taskManager == null)
        {
            return;
        }

        taskBacklogView.Render(taskManager.QueuedCount);
    }

    /// <summary>セッションが終わり、結果画面へ移る直前。</summary>
    /// <remarks>
    /// **終了演出を差しはさむ余地をここに作っている。** 購読者がいればそちらに任せ、
    /// いなければこれまでどおり自分で結果画面へ移る。
    /// この Controller はどの機械を映しているかを知らないため、
    /// 「PC 面へ戻してから蓋を閉じる」といった判断は購読側（<c>GameManager</c>）が持つ。
    /// </remarks>
    public event Action<GameSessionResult> SessionFinished;

    private void FinishSession()
    {
        ending = true;
        Time.timeScale = 1f;

        var result = session.CreateResult();
        if (SessionFinished != null)
        {
            SessionFinished(result);
            return;
        }

        GameFlowController.EnsureInstance().PresentResult(result);
    }

    private void SetPaused(bool value)
    {
        if (!initialized || ending || paused == value)
        {
            return;
        }

        paused = value;
        Time.timeScale = paused ? 0f : 1f;
        pauseMenu.SetVisible(paused);
        AudioManager.PlaySfx(paused ? AudioCue.PauseOpen : AudioCue.PauseClose);
    }

    private void OnDestroy()
    {
        if (taskManager != null)
        {
            taskManager.TaskResolved -= OnTaskResolved;
            taskManager.TaskShown -= OnTaskShown;
        }

        Time.timeScale = 1f;
    }
}