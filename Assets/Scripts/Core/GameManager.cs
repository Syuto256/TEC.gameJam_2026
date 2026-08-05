using System;
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
        miniGameHostView.Hide();
    }

    /// <summary>ミニゲーム中はデバイス切替を受け付けない（暫定仕様）。</summary>
    private void OnPlayerMiniGameActiveChanged(bool active)
    {
        deviceScreenController.SetSwitchEnabled(!active);

        if (focusLightingView != null)
        {
            focusLightingView.SetFocused(active);
        }

            // 追加：ミニゲーム中は全 Workspace 内のタスク操作を無効化する
        foreach (var workspace in workspaces)
        {
            if (workspace != null)
            {
                workspace.SetInteractionEnabled(!active);
            }
        }
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
        }
    }
}
