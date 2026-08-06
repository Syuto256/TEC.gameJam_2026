using System;
using System.Collections;
using UnityEngine;

/// <summary>Game シーンの入口。View とゲーム進行コンポーネントを接続するだけを担当する。</summary>
/// <remarks>
/// ゲーム進行そのものは <see cref="MainGameController"/> が持つ。このクラスは配線係であり、
/// 表示の見た目・座標も、タスクや HP の計算も持たない。
/// </remarks>
public sealed class GameManager : MonoBehaviour
{
    [Header("【設定アセット】")]
    [SerializeField] private GameTuningSettings tuningSettings;

    [Header("【表示部品】")]
    [SerializeField] private HudView hudView;
    [SerializeField] private DeviceTabsView deviceTabsView;
    [SerializeField] private MiniGameHostView miniGameHostView;
    [SerializeField] private PauseMenuView pauseMenuView;

    [Tooltip("ミニゲーム中の集中演出（背後を落とし、窓のまわりを光らせる）。\n" +
             "未設定でも進行には影響しない（明るさが変わらないだけ）。")]
    [SerializeField] private FocusLightingView focusLightingView;

    [Header("【デバイス面】")]
    [Tooltip("デバイス面を並べる。Surface が重複しないこと。3 つ目の面を足す場合もここへ追加する。")]
    [SerializeField] private DeviceWorkspaceView[] workspaces = Array.Empty<DeviceWorkspaceView>();

    [Header("【進行制御】")]
    [SerializeField] private MainGameController mainGameController;
    [SerializeField] private DeviceScreenController deviceScreenController;

    [Header("【終了演出】")]
    [Tooltip("ゲームが終わってから画面を覆い始めるまでの余韻（秒）。\n" +
             "この間はゲーム画面が止まったまま残る。0 にすると終わった瞬間に蓋の演出へ入る。")]
    [Min(0f)] [SerializeField] private float endHoldBeforeCoverSec = 0.6f;

    // デバイス切替を止めたい理由。すべて RefreshDeviceSwitchEnabled で束ねて反映する。
    private bool miniGameActive;
    private bool paused;
    private bool sessionFinished;

    private void Start()
    {
        AppServices.Ensure();

        if (!SceneUiValidation.Require(this,
                (nameof(tuningSettings), tuningSettings),
                (nameof(hudView), hudView), (nameof(deviceTabsView), deviceTabsView),
                (nameof(miniGameHostView), miniGameHostView), (nameof(pauseMenuView), pauseMenuView),
                (nameof(mainGameController), mainGameController), (nameof(deviceScreenController), deviceScreenController)))
        {
            return;
        }

        if (!HasValidWorkspaces())
        {
            return;
        }

        // 単一の & で全 View の不足を一度に報告する。
        var viewsReady = hudView.Initialize() & deviceTabsView.Initialize()
            & miniGameHostView.Initialize() & pauseMenuView.Initialize();
        foreach (var workspace in workspaces)
        {
            viewsReady = workspace.Initialize() & viewsReady;
        }

        if (focusLightingView != null)
        {
            viewsReady = focusLightingView.Initialize() & viewsReady;
        }

        if (!viewsReady)
        {
            return;
        }

        deviceScreenController.Initialize(workspaces, deviceTabsView);
        mainGameController.Initialize(tuningSettings, hudView, workspaces, miniGameHostView, pauseMenuView);

        hudView.PauseRequested += mainGameController.TogglePause;
        pauseMenuView.ResumeRequested += mainGameController.Resume;
        pauseMenuView.BackToDifficultyRequested += mainGameController.ReturnToDifficultySelect;
        mainGameController.PlayerMiniGameActiveChanged += OnPlayerMiniGameActiveChanged;
        mainGameController.PausedChanged += OnPausedChanged;
        mainGameController.SessionFinished += OnSessionFinished;
        miniGameHostView.Hide();
    }

    /// <summary>ゲームを終えたら、PC 面へ戻してから結果画面へ移る。</summary>
    /// <remarks>
    /// **蓋を閉じる絵はノート PC のものである。** 液タブを見たまま終わると、
    /// 見ていない機械が閉じることになるため、先に既存の切替演出で PC 面へ戻す。
    /// すでに PC 面なら待たずに進むので、余計な間は入らない。
    /// <para>
    /// 戻している最中にタブを押されると行き先が変わってしまうため、先に切替を止める。
    /// </para>
    /// <para>
    /// **PC 面へ戻し終えてから余韻を取る。** 戻す前に取ると、液タブを見たまま終わった場合に
    /// 余韻の途中で画面が横に流れてしまう。後に置けば、どちらの面で終わっても
    /// 必ず PC 面で静止した状態の余韻になる。
    /// </para>
    /// </remarks>
    private void OnSessionFinished(GameSessionResult result)
    {
        sessionFinished = true;
        RefreshDeviceSwitchEnabled();

        // 戻すほうは force で通す。禁止のままだと、まさに戻したい場面で戻せない。
        deviceScreenController.ReturnToPc(() => StartCoroutine(PresentAfterHold(result)));
    }

    /// <summary>終了の余韻を置いてから結果画面へ移る。</summary>
    /// <remarks>
    /// 待っているあいだ、<see cref="MainGameController"/> は <c>ending</c> のため進行を止めており、
    /// タスクの生成も操作も通らない。画面は終わった瞬間の姿のまま残る。
    /// </remarks>
    private IEnumerator PresentAfterHold(GameSessionResult result)
    {
        // 実時間で測る。ポーズ経路と組み合わさっても長さが変わらないようにするためである。
        if (endHoldBeforeCoverSec > 0f)
        {
            yield return new WaitForSecondsRealtime(endHoldBeforeCoverSec);
        }

        GameFlowController.EnsureInstance().PresentResult(result);
    }

    /// <summary>ミニゲーム中はデバイス切替を受け付けない。</summary>
    /// <remarks>
    /// **タスクの操作は止めない。** ミニゲームを解いている最中でも、
    /// 残りのタスクを右クリックで AI に任せられるようにするためである。
    /// <para>
    /// 以前はここで全ての面の操作を無効にしていた。AI に任せる操作まで一緒に
    /// 塞がっていたので外した。左クリックで 2 つ目のミニゲームが開くのは
    /// <see cref="MainGameController.TryAssignPlayer"/> 側で弾いている。
    /// </para>
    /// <para>
    /// デバイス切替を止めるのは残す。ミニゲームは面をまたがないため、
    /// 解いている最中に別の面へ移れても得るものが無い。
    /// </para>
    /// </remarks>
    private void OnPlayerMiniGameActiveChanged(bool active)
    {
        miniGameActive = active;
        RefreshDeviceSwitchEnabled();

        if (focusLightingView != null)
        {
            focusLightingView.SetFocused(active);
        }
    }

    /// <summary>一時停止中はデバイス切替を受け付けない。</summary>
    /// <remarks>
    /// タスクの吹き出しは <see cref="MainGameController"/> が自分で弾いている。
    /// タブだけが素通りしていたため、同じ方式へ揃える。
    /// </remarks>
    private void OnPausedChanged(bool value)
    {
        paused = value;
        RefreshDeviceSwitchEnabled();
    }

    /// <summary>デバイス切替を許すかどうかを、止めたい理由すべてから決め直す。</summary>
    /// <remarks>
    /// **理由ごとに <c>SetSwitchEnabled</c> を直接呼んではいけない。** あれは単なる代入で
    /// 後から呼んだほうが勝つため、たとえばミニゲーム中にポーズして解除すると、
    /// ミニゲームが開いたままなのに切替が復活してしまう。必ずここで束ねて反映する。
    /// </remarks>
    private void RefreshDeviceSwitchEnabled()
    {
        deviceScreenController.SetSwitchEnabled(!miniGameActive && !paused && !sessionFinished);
    }

    private bool HasValidWorkspaces()
    {
        if (workspaces == null || workspaces.Length == 0)
        {
            Debug.LogError("GameManager (" + name + "): workspaces が空です。", this);
            return false;
        }

        for (var i = 0; i < workspaces.Length; i++)
        {
            if (workspaces[i] == null)
            {
                Debug.LogError("GameManager (" + name + "): workspaces[" + i + "] が未設定です。", this);
                return false;
            }

            for (var j = i + 1; j < workspaces.Length; j++)
            {
                if (workspaces[j] != null && workspaces[j].Surface == workspaces[i].Surface)
                {
                    Debug.LogError(
                        "GameManager (" + name + "): Surface が重複しています -> " + workspaces[i].Surface, this);
                    return false;
                }
            }
        }

        return true;
    }

    private void OnDestroy()
    {
        if (mainGameController != null)
        {
            mainGameController.PlayerMiniGameActiveChanged -= OnPlayerMiniGameActiveChanged;
            mainGameController.PausedChanged -= OnPausedChanged;
            mainGameController.SessionFinished -= OnSessionFinished;
        }
    }
}
