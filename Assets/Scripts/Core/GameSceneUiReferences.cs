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
    [SerializeField] private GameObject pcOnly;
    [SerializeField] private GameObject tabletOnly;
    [SerializeField] private Transform pcTaskSpawnArea;
    [SerializeField] private Transform tabletTaskSpawnArea;

    [Header("Controllers")]
    [SerializeField] private MainGameController mainGameController;
    [SerializeField] private DeviceScreenController deviceScreenController;

    public void Initialize(GameTuningSettings tuningSettings, IPlayerMiniGameLauncher[] miniGameLaunchers)
    {
        if (!SceneUiValidation.Require(this,
                (nameof(hudView), hudView), (nameof(deviceTabsView), deviceTabsView),
                (nameof(miniGameHostView), miniGameHostView), (nameof(pauseMenuView), pauseMenuView),
                (nameof(pcOnly), pcOnly), (nameof(tabletOnly), tabletOnly),
                (nameof(pcTaskSpawnArea), pcTaskSpawnArea), (nameof(tabletTaskSpawnArea), tabletTaskSpawnArea),
                (nameof(mainGameController), mainGameController), (nameof(deviceScreenController), deviceScreenController)))
        {
            return;
        }

        // 単一の & で全 View の不足を一度に報告する。
        var viewsReady = hudView.Initialize() & deviceTabsView.Initialize()
            & miniGameHostView.Initialize() & pauseMenuView.Initialize();
        if (!viewsReady)
        {
            return;
        }

        deviceScreenController.Initialize(pcOnly, tabletOnly, deviceTabsView);
        mainGameController.Initialize(
            tuningSettings,
            hudView,
            pcTaskSpawnArea,
            tabletTaskSpawnArea,
            miniGameHostView,
            pauseMenuView,
            miniGameLaunchers);

        hudView.PauseRequested += mainGameController.TogglePause;
        pauseMenuView.ResumeRequested += mainGameController.Resume;
        pauseMenuView.BackToDifficultyRequested += mainGameController.ReturnToDifficultySelect;
        miniGameHostView.Hide();
    }
}
