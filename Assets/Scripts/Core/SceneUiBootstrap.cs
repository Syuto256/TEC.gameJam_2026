using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// シーン名に応じた P0 の操作可能な Canvas UI を生成する。
/// 見た目の詳細調整は M6 で Prefab 化・置換する前提の基盤である。
/// </summary>
public sealed class SceneUiBootstrap : MonoBehaviour
{
    private static readonly Color BackgroundColor = new Color(0.06f, 0.09f, 0.15f, 1f);
    private static readonly Color PanelColor = new Color(0.12f, 0.18f, 0.27f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.18f, 0.43f, 0.63f, 1f);

    [SerializeField] private GameTuningSettings tuningSettings;

    private void Start()
    {
        GameFlowController.EnsureInstance();
        AudioManager.EnsureInstance();
        EnsureEventSystem();
        BuildCanvasFor(SceneManager.GetActiveScene().name);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private void BuildCanvasFor(string sceneName)
    {
        if (GameObject.Find("MainCanvas") != null)
        {
            return;
        }

        var canvasObject = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var root = CreatePanel(canvasObject.transform, "ScreenRoot", Vector2.zero, Vector2.one, BackgroundColor);
        switch (sceneName)
        {
            case GameFlowController.TitleSceneName:
                BuildTitle(root.transform);
                break;
            case GameFlowController.DifficultySelectSceneName:
                BuildDifficultySelect(root.transform);
                break;
            case GameFlowController.GameSceneName:
                BuildGame(
                    root.transform,
                    tuningSettings,
                    GetMiniGameLaunchers());
                break;
            case GameFlowController.ClearSceneName:
                BuildResult(root.transform, true);
                break;
            case GameFlowController.GameOverSceneName:
                BuildResult(root.transform, false);
                break;
            default:
                CreateText(root.transform, "Unsupported scene: " + sceneName, Vector2.zero, Vector2.one, 42f, TextAlignmentOptions.Center);
                break;
        }
    }

    private static void BuildTitle(Transform parent)
    {
        CreateText(parent, "OVERWORK YOURSELF", new Vector2(0.15f, 0.6f), new Vector2(0.85f, 0.85f), 86f, TextAlignmentOptions.Center);
        CreateText(parent, "Task management mini-game", new Vector2(0.2f, 0.52f), new Vector2(0.8f, 0.6f), 28f, TextAlignmentOptions.Center);
        CreateButton(parent, "Start", new Vector2(0.5f, 0.38f), () => GameFlowController.EnsureInstance().OpenDifficultySelect());
    }

    private static void BuildDifficultySelect(Transform parent)
    {
        CreateText(parent, "SELECT DIFFICULTY", new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.9f), 58f, TextAlignmentOptions.Center);
        CreateDifficultyButton(parent, "Easy", GameDifficulty.Easy, 0.64f);
        CreateDifficultyButton(parent, "Normal", GameDifficulty.Normal, 0.53f);
        CreateDifficultyButton(parent, "Hard", GameDifficulty.Hard, 0.42f);
        CreateDifficultyButton(parent, "Very Hard", GameDifficulty.VeryHard, 0.31f);
        CreateDifficultyButton(parent, "Endless", GameDifficulty.Endless, 0.20f);
    }

    private static void CreateDifficultyButton(Transform parent, string label, GameDifficulty difficulty, float y)
    {
        CreateButton(parent, label, new Vector2(0.5f, y), () => GameFlowController.EnsureInstance().SelectDifficulty(difficulty));
    }

    private static void BuildGame(
        Transform parent,
        GameTuningSettings tuningSettings,
        IPlayerMiniGameLauncher[] miniGameLaunchers)
    {
        var flow = GameFlowController.EnsureInstance();
        var hud = CreatePanel(parent, "HudPanel", new Vector2(0.02f, 0.87f), new Vector2(0.98f, 0.98f), PanelColor);
        var hudText = CreateText(hud.transform, "HP 100     SCORE 0     TIME 03:00     " + flow.SelectedDifficulty, Vector2.zero, Vector2.one, 32f, TextAlignmentOptions.Center);

        var pcPanel = CreatePanel(parent, "PcTaskPanel", new Vector2(0.02f, 0.18f), new Vector2(0.48f, 0.82f), PanelColor);
        CreateText(pcPanel.transform, "PC TASK AREA", new Vector2(0f, 0.86f), Vector2.one, 26f, TextAlignmentOptions.Center);
        var pcTaskSpawnArea = CreateRectTransform(pcPanel.transform, "TaskSpawnArea", Vector2.zero, new Vector2(1f, 0.84f));

        var padPanel = CreatePanel(parent, "PadTaskPanel", new Vector2(0.52f, 0.18f), new Vector2(0.98f, 0.82f), PanelColor);
        CreateText(padPanel.transform, "PAD TASK AREA", new Vector2(0f, 0.86f), Vector2.one, 26f, TextAlignmentOptions.Center);
        var padTaskSpawnArea = CreateRectTransform(padPanel.transform, "TaskSpawnArea", Vector2.zero, new Vector2(1f, 0.84f));

        var miniGameHost = CreatePanel(parent, "MiniGameHost", new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), new Color(0.04f, 0.06f, 0.1f, 0.96f));
        CreateText(miniGameHost.transform, "MINI GAME HOST\n(M4 / M5)", Vector2.zero, Vector2.one, 30f, TextAlignmentOptions.Center);
        miniGameHost.SetActive(false);

        var pausePanel = CreatePanel(parent, "PausePanel", new Vector2(0.32f, 0.3f), new Vector2(0.68f, 0.7f), PanelColor);
        CreateText(pausePanel.transform, "PAUSED", new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.9f), 48f, TextAlignmentOptions.Center);
        var controller = parent.gameObject.AddComponent<MainGameController>();
        controller.Initialize(tuningSettings, hudText, pcTaskSpawnArea, padTaskSpawnArea, miniGameHost, pausePanel, miniGameLaunchers);

        CreateButton(pausePanel.transform, "Resume", new Vector2(0.5f, 0.5f), controller.Resume);
        CreateButton(pausePanel.transform, "Back to Difficulty", new Vector2(0.5f, 0.25f), controller.ReturnToDifficultySelect);
        pausePanel.SetActive(false);

        var optionPanel = CreatePanel(parent, "OptionPanel", new Vector2(0.32f, 0.3f), new Vector2(0.68f, 0.7f), PanelColor);
        CreateText(optionPanel.transform, "OPTIONS\n(M6)", Vector2.zero, Vector2.one, 40f, TextAlignmentOptions.Center);
        optionPanel.SetActive(false);

        CreateButton(parent, "Pause", new Vector2(0.88f, 0.08f), controller.TogglePause, new Vector2(180f, 56f));
        CreateButton(parent, "Difficulty", new Vector2(0.12f, 0.08f), controller.ReturnToDifficultySelect, new Vector2(180f, 56f));
    }

    private IPlayerMiniGameLauncher[] GetMiniGameLaunchers()
    {
        var components = GetComponents<MonoBehaviour>();
        var launchers = new System.Collections.Generic.List<IPlayerMiniGameLauncher>();
        foreach (var component in components)
        {
            if (component is IPlayerMiniGameLauncher launcher)
            {
                launchers.Add(launcher);
            }
        }

        return launchers.ToArray();
    }

    private static void BuildResult(Transform parent, bool cleared)
    {
        var flow = GameFlowController.EnsureInstance();
        CreateText(parent, cleared ? "CLEAR" : "GAME OVER", new Vector2(0.2f, 0.64f), new Vector2(0.8f, 0.85f), 88f, TextAlignmentOptions.Center);

        var result = flow.LastSessionResult;
        var summary = result == null
            ? "Result will be shown after a session."
            : "Difficulty: " + result.Difficulty + "\nScore: " + result.FinalScore + "\nHP: " + result.FinalHp;
        CreateText(parent, summary, new Vector2(0.25f, 0.43f), new Vector2(0.75f, 0.6f), 32f, TextAlignmentOptions.Center);

        if (cleared)
        {
            CreateButton(parent, "Back to Difficulty", new Vector2(0.5f, 0.25f), () => flow.OpenDifficultySelect());
        }
        else
        {
            CreateButton(parent, "Retry", new Vector2(0.5f, 0.33f), () => flow.Retry());
            CreateButton(parent, "Back to Difficulty", new Vector2(0.5f, 0.20f), () => flow.OpenDifficultySelect());
        }
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Transform CreateRectTransform(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var area = new GameObject(name, typeof(RectTransform));
        area.transform.SetParent(parent, false);
        var rect = area.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return area.transform;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string value, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(12f, 8f);
        rect.offsetMax = new Vector2(-12f, -8f);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.color = Color.white;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchorPosition, Action onClick, Vector2? sizeOverride = null)
    {
        var buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorPosition;
        rect.anchorMax = anchorPosition;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeOverride ?? new Vector2(320f, 76f);
        rect.anchoredPosition = Vector2.zero;

        var image = buttonObject.GetComponent<Image>();
        image.color = ButtonColor;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            AudioManager.PlaySfx(AudioCue.UiConfirm);
            onClick();
        });

        CreateText(buttonObject.transform, label, Vector2.zero, Vector2.one, 30f, TextAlignmentOptions.Center);
        return button;
    }
}
