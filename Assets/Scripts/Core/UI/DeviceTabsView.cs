using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>PC / Tablet タブの外観と入力だけを担当する。どちらを表示するかは決めない。</summary>
public sealed class DeviceTabsView : MonoBehaviour
{
    [Header("【必須】")]
    [SerializeField] private Button pcTab;
    [SerializeField] private Button tabletTab;

    [Header("【任意】")]
    [Tooltip("選択中のタブに重ねる強調表示。未設定なら interactable の切替だけで表す。")]
    [SerializeField] private GameObject pcSelectedMark;
    [SerializeField] private GameObject tabletSelectedMark;

    private TaskSurface selected = TaskSurface.Pc;
    private bool interactable = true;
    private bool initialized;

    /// <summary>プレイヤーが表示したいデバイスを選んだ。</summary>
    public event Action<TaskSurface> SurfaceRequested;

    /// <summary>参照を検証し、自身の入力を配線する。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this, (nameof(pcTab), pcTab), (nameof(tabletTab), tabletTab)))
        {
            return false;
        }

        pcTab.onClick.AddListener(() => SurfaceRequested?.Invoke(TaskSurface.Pc));
        tabletTab.onClick.AddListener(() => SurfaceRequested?.Invoke(TaskSurface.Pad));
        initialized = true;
        Refresh();
        return true;
    }

    public void SetSelected(TaskSurface surface)
    {
        selected = surface;
        Refresh();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        Refresh();
    }

    private void Refresh()
    {
        if (!initialized)
        {
            return;
        }

        pcTab.interactable = interactable && selected != TaskSurface.Pc;
        tabletTab.interactable = interactable && selected != TaskSurface.Pad;

        if (pcSelectedMark != null)
        {
            pcSelectedMark.SetActive(selected == TaskSurface.Pc);
        }

        if (tabletSelectedMark != null)
        {
            tabletSelectedMark.SetActive(selected == TaskSurface.Pad);
        }
    }
}
