using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>一件の TaskInstance を表す吹き出し。左クリックは自力、右クリックは AI を依頼する。</summary>
/// <remarks>
/// 大きさ・配色・文字・配置は Prefab と `TaskSpawnArea` の Layout Group で調整する。
/// このクラスはタスクの状態を割り当てられた表示先へ書き込むだけで、座標もサイズも持たない。
/// </remarks>
public sealed class TaskBubbleView : MonoBehaviour, IPointerClickHandler
{
    [Header("Required")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI kindText;
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("Optional")]
    [SerializeField] private TextMeshProUGUI timeText;
    [Tooltip("残り寿命を表すバー。Sprite を割り当て、Image Type を Filled にする。")]
    [SerializeField] private Image lifetimeGauge;
    [Tooltip("種別アイコン。MiniGameCatalog の icon が空なら非表示にする。")]
    [SerializeField] private Image kindIcon;

    [Header("State colors")]
    [SerializeField] private Color availableColor = new Color(0.16f, 0.42f, 0.66f, 1f);
    [SerializeField] private Color playerPlayingColor = new Color(0.56f, 0.24f, 0.59f, 1f);
    [SerializeField] private Color aiProcessingColor = new Color(0.70f, 0.46f, 0.10f, 1f);
    [SerializeField] private Color resolvedColor = new Color(0.24f, 0.24f, 0.24f, 1f);

    [Header("State labels")]
    [SerializeField] private string availableLabel = "L: SELF / R: AI";
    [SerializeField] private string playerPlayingLabel = "SELF PLAYING";
    [SerializeField] private string aiProcessingLabel = "AI PROCESSING";
    [SerializeField] private string resolvedLabel = "RESOLVED";

    private MainGameController controller;
    private TaskInstance task;
    private string kindLabel;

    public int TaskId => task?.Id ?? -1;

    /// <summary>Prefab の必須参照を検証する。生成前に一度だけ呼ぶ。</summary>
    public bool ValidateReferences()
    {
        return SceneUiValidation.Require(this,
            (nameof(background), background), (nameof(kindText), kindText), (nameof(stateText), stateText));
    }

    /// <summary>表示するタスクと通知先を割り当てる。表示名とアイコンは MiniGameCatalog の登録内容を渡す。</summary>
    public void Bind(MainGameController owner, TaskInstance instance, string displayName, Sprite icon)
    {
        controller = owner;
        task = instance;
        kindLabel = displayName;

        if (kindIcon != null)
        {
            kindIcon.sprite = icon;
            kindIcon.enabled = icon != null;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (task == null)
        {
            return;
        }

        if (kindText != null)
        {
            kindText.text = (string.IsNullOrWhiteSpace(kindLabel) ? task.Kind.ToString() : kindLabel) + "  Lv." + task.Level;
        }

        if (stateText != null)
        {
            stateText.text = task.State switch
            {
                TaskState.Available => availableLabel,
                TaskState.PlayerPlaying => playerPlayingLabel,
                TaskState.AiProcessing => aiProcessingLabel,
                _ => resolvedLabel
            };
        }

        if (timeText != null)
        {
            timeText.text = task.RemainingLifetimeSec.ToString("0.0");
        }

        if (lifetimeGauge != null)
        {
            lifetimeGauge.fillAmount = task.InitialLifetimeSec <= 0f
                ? 0f
                : Mathf.Clamp01(task.RemainingLifetimeSec / task.InitialLifetimeSec);
        }

        if (background != null)
        {
            background.color = task.State switch
            {
                TaskState.Available => availableColor,
                TaskState.PlayerPlaying => playerPlayingColor,
                TaskState.AiProcessing => aiProcessingColor,
                _ => resolvedColor
            };
        }
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
}
