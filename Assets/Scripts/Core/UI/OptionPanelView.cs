using UnityEngine;
using UnityEngine.UI;

/// <summary>オプションパネル内部の設定 UI と保存済み設定をつなぐ。</summary>
public sealed class OptionPanelView : MonoBehaviour
{
    [Header("【設定 UI】")]
    [Tooltip("BGM 音量のスライダー")]
    [SerializeField] private Slider bgmSlider;

    [Tooltip("SE 音量のスライダー")]
    [SerializeField] private Slider sfxSlider;

    [Tooltip("オプションパネルを閉じるボタン")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.onValueChanged.AddListener(HandleBgmChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void OnEnable()
    {
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(AudioManager.BgmVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioManager.SfxVolume);
        }
    }

    /// <summary>オプションパネルを表示する。</summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>オプションパネルを非表示にする。</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static void HandleBgmChanged(float value)
    {
        AudioManager.BgmVolume = value;
    }

    private static void HandleSfxChanged(float value)
    {
        AudioManager.SfxVolume = value;
        AudioManager.PlaySfx(AudioCue.UiConfirm);
    }
}
