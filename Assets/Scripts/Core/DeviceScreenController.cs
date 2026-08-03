using UnityEngine;

/// <summary>PC / Tablet ワークスペースの排他表示と、切替可否だけを担当する。</summary>
public sealed class DeviceScreenController : MonoBehaviour
{
    private GameObject pcOnly;
    private GameObject tabletOnly;
    private DeviceTabsView tabs;
    private bool switchEnabled = true;

    public TaskSurface ActiveSurface { get; private set; } = TaskSurface.Pc;

    public void Initialize(GameObject pc, GameObject tablet, DeviceTabsView deviceTabs)
    {
        pcOnly = pc;
        tabletOnly = tablet;
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
        if (!switchEnabled || pcOnly == null || tabletOnly == null)
        {
            return;
        }

        ActiveSurface = surface;
        pcOnly.SetActive(surface == TaskSurface.Pc);
        tabletOnly.SetActive(surface == TaskSurface.Pad);

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
