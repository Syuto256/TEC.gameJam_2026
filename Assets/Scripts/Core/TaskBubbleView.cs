using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>一件の TaskInstance を表す暫定 Canvas UI。左クリックは自力、右クリックは AI を依頼する。</summary>
public sealed class TaskBubbleView : MonoBehaviour, IPointerClickHandler
{
    private MainGameController controller;
    private TaskInstance task;
    private Image background;
    private TextMeshProUGUI label;

    public int TaskId => task?.Id ?? -1;

    public static TaskBubbleView Create(Transform parent, MainGameController controller, TaskInstance task)
    {
        var bubbleObject = new GameObject("TaskBubble_" + task.Id, typeof(RectTransform), typeof(Image), typeof(TaskBubbleView));
        bubbleObject.transform.SetParent(parent, false);

        var rect = bubbleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(220f, 110f);
        rect.anchoredPosition = GetInitialPosition(task.Id);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(bubbleObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 8f);
        labelRect.offsetMax = new Vector2(-10f, -8f);

        var view = bubbleObject.GetComponent<TaskBubbleView>();
        view.controller = controller;
        view.task = task;
        view.background = bubbleObject.GetComponent<Image>();
        view.label = labelObject.GetComponent<TextMeshProUGUI>();
        view.label.font = TMP_Settings.defaultFontAsset;
        view.label.fontSize = 22f;
        view.label.alignment = TextAlignmentOptions.Center;
        view.label.enableWordWrapping = true;
        view.label.color = Color.white;
        view.Refresh();
        return view;
    }

    public void Refresh()
    {
        if (task == null || label == null)
        {
            return;
        }

        var stateText = task.State switch
        {
            TaskState.Available => "L: SELF / R: AI",
            TaskState.PlayerPlaying => "SELF PLAYING\n(M4/M5)",
            TaskState.AiProcessing => "AI PROCESSING",
            _ => "RESOLVED"
        };
        label.text = task.Kind + "  Lv." + task.Level + "\n" + stateText + "\n" + task.RemainingLifetimeSec.ToString("0.0") + " sec";
        background.color = task.State switch
        {
            TaskState.Available => new Color(0.16f, 0.42f, 0.66f, 1f),
            TaskState.PlayerPlaying => new Color(0.56f, 0.24f, 0.59f, 1f),
            TaskState.AiProcessing => new Color(0.70f, 0.46f, 0.10f, 1f),
            _ => new Color(0.24f, 0.24f, 0.24f, 1f)
        };
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (task == null || controller == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            controller.TryAssignPlayer(task.Id);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            controller.TryAssignAi(task.Id);
        }
    }

    private static Vector2 GetInitialPosition(int taskId)
    {
        var column = (taskId - 1) % 3;
        var row = ((taskId - 1) / 3) % 2;
        return new Vector2(-150f + column * 150f, 80f - row * 190f);
    }
}
