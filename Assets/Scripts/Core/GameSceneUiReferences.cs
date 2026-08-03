using System;
using UnityEngine;

/// <summary>Game シーン上の View とゲーム進行コンポーネントを接続するだけの入口。</summary>
public sealed class GameSceneUiReferences : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private HudView hudView;
    [SerializeField] private DeviceTabsView deviceTabsView;
    [SerializeField] private MiniGameHostView miniGameHostView;
    [SerializeField] private PauseMenuView pauseMenuView;

    [Header("Device workspaces")]
    [Tooltip("デバイス面を並べる。Surface が重複しないこと。3 つ目の面を足す場合もここへ追加する。")]
    [SerializeField] private DeviceWorkspaceView[] workspaces = Array.Empty<DeviceWorkspaceView>();

    [Header("Controllers")]
    [SerializeField] private MainGameController mainGameController;
    [SerializeField] private DeviceScreenController deviceScreenController;

    public void Initialize(GameTuningSettings tuningSettings, IPlayerMiniGameLauncher[] miniGameLaunchers)
    {
        if (!SceneUiValidation.Require(this,
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

        if (!viewsReady)
        {
            return;
        }

        deviceScreenController.Initialize(workspaces, deviceTabsView);
        mainGameController.Initialize(
            tuningSettings,
            hudView,
            workspaces,
            miniGameHostView,
            pauseMenuView,
            miniGameLaunchers);

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
    }

    private bool HasValidWorkspaces()
    {
        if (workspaces == null || workspaces.Length == 0)
        {
            Debug.LogError("GameSceneUiReferences (" + name + "): workspaces が空です。", this);
            return false;
        }

        for (var i = 0; i < workspaces.Length; i++)
        {
            if (workspaces[i] == null)
            {
                Debug.LogError("GameSceneUiReferences (" + name + "): workspaces[" + i + "] が未設定です。", this);
                return false;
            }

            for (var j = i + 1; j < workspaces.Length; j++)
            {
                if (workspaces[j] != null && workspaces[j].Surface == workspaces[i].Surface)
                {
                    Debug.LogError(
                        "GameSceneUiReferences (" + name + "): Surface が重複しています -> " + workspaces[i].Surface, this);
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
