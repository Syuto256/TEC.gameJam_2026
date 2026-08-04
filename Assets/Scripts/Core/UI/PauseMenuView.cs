using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ポーズとオプションのパネル表示・ボタン入力だけを担当する。ゲーム進行は判断しない。</summary>
/// <remarks>
/// パネルは非表示状態で開始するため、このコンポーネントは常時有効な親（例: ModalLayer）へ置き、
/// <see cref="panel"/> に非表示にしたい枝を指定する。
/// </remarks>
public sealed class PauseMenuView : MonoBehaviour
{
    [Header("【必須】")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button backToDifficultyButton;

    [Header("【任意】")]
    [SerializeField] private Button optionButton;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button optionCloseButton;

    [Header("【任意】音量スライダー")]
    [Tooltip("BGM 音量のスライダー。0〜1 の範囲にする。")]
    [SerializeField] private Slider bgmVolumeSlider;

    [Tooltip("SE 音量のスライダー。0〜1 の範囲にする。")]
    [SerializeField] private Slider sfxVolumeSlider;

    private bool initialized;

    public event Action ResumeRequested;
    public event Action BackToDifficultyRequested;

    /// <summary>参照を検証し、自身の入力を配線する。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this, (nameof(panel), panel), (nameof(resumeButton), resumeButton),
                (nameof(backToDifficultyButton), backToDifficultyButton)))
        {
            return false;
        }

        resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());
        backToDifficultyButton.onClick.AddListener(() => BackToDifficultyRequested?.Invoke());

        if (optionButton != null)
        {
            optionButton.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx(AudioCue.UiConfirm);
                SetOptionVisible(true);
            });
        }

        if (optionCloseButton != null)
        {
            optionCloseButton.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx(AudioCue.UiCancel);
                SetOptionVisible(false);
            });
        }

        InitializeVolumeSlider(bgmVolumeSlider, AudioManager.BgmVolume, value => AudioManager.BgmVolume = value);
        InitializeVolumeSlider(sfxVolumeSlider, AudioManager.SfxVolume, value => AudioManager.SfxVolume = value);

        initialized = true;
        SetVisible(false);
        return true;
    }

    public void SetVisible(bool value)
    {
        if (panel != null)
        {
            panel.SetActive(value);
        }

        if (!value)
        {
            SetOptionVisible(false);
        }
    }

    private void SetOptionVisible(bool value)
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(value);
        }
    }

    /// <summary>保存済みの音量をスライダーへ反映し、操作を <see cref="AudioManager"/> へつなぐ。</summary>
    private static void InitializeVolumeSlider(Slider slider, float currentValue, Action<float> apply)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(currentValue);
        slider.onValueChanged.AddListener(value => apply(value));
    }
}
