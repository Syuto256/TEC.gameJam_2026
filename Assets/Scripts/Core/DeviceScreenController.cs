using System;
using UnityEngine;

/// <summary>どのデバイス面を主作業画面として表示するかと、切替可否だけを担当する。</summary>
public sealed class DeviceScreenController : MonoBehaviour
{
    private DeviceWorkspaceView[] workspaces = Array.Empty<DeviceWorkspaceView>();
    private DeviceTabsView tabs;
    private bool switchEnabled = true;

    public TaskSurface ActiveSurface { get; private set; } = TaskSurface.Pc;

    public void Initialize(DeviceWorkspaceView[] deviceWorkspaces, DeviceTabsView deviceTabs)
    {
        workspaces = deviceWorkspaces ?? Array.Empty<DeviceWorkspaceView>();
        tabs = deviceTabs;

        if (tabs != null)
        {
            tabs.SurfaceRequested += Show;
        }

        Show(TaskSurface.Pc);
    }

    public void SetSwitchEnabled(bool enabled)
    {
        switchEnabled = enabled;
        if (tabs != null)
        {
            tabs.SetInteractable(enabled);
        }
    }

    public void Show(TaskSurface surface)
    {
        if (!switchEnabled)
        {
            return;
        }

        ActiveSurface = surface;
        foreach (var workspace in workspaces)
        {
            if (workspace != null)
            {
                workspace.SetVisible(workspace.Surface == surface);
            }
        }

        if (tabs != null)
        {
            tabs.SetSelected(surface);
        }
    }

    private void OnDestroy()
    {
        if (tabs != null)
        {
            tabs.SurfaceRequested -= Show;
        }
    }
}
