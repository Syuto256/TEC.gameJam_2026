using UnityEngine;
using UnityEngine.UI;

/// <summary>Title シーンの入口。ボタンを遷移につなぐだけを担当する。</summary>
/// <remarks>文字・配色・配置は <c>Title.unity</c> の Hierarchy と Inspector で調整する。</remarks>
public sealed class TitleManager : MonoBehaviour
{
    [Header("【必須】")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private CanvasGroup creditsModal;
    [SerializeField] private Button creditsBackdropButton;
    [SerializeField] private Button closeCreditsButton;
    [SerializeField] private Button openOflLicenseButton;
    [SerializeField] private CanvasGroup oflLicenseModal;
    [SerializeField] private Button oflLicenseBackdropButton;
    [SerializeField] private Button backToCreditsButton;

    [Header("【オプション】")]
    [SerializeField] private Button optionButton;
    [SerializeField] private CanvasGroup optionModal;
    [SerializeField] private Button optionBackdropButton;
    [SerializeField] private Button closeOptionButton;

    [Tooltip("BGM の音量。0 で無音、1 で最大。値は AudioManager が PlayerPrefs に保存する。")]
    [SerializeField] private Slider bgmSlider;

    [Tooltip("効果音の音量。0 で無音、1 で最大。")]
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        AppServices.Ensure();
        if (!SceneUiValidation.Require(
                this,
                (nameof(startButton), startButton),
                (nameof(creditsButton), creditsButton),
                (nameof(creditsModal), creditsModal),
                (nameof(creditsBackdropButton), creditsBackdropButton),
                (nameof(closeCreditsButton), closeCreditsButton),
                (nameof(openOflLicenseButton), openOflLicenseButton),
                (nameof(oflLicenseModal), oflLicenseModal),
                (nameof(oflLicenseBackdropButton), oflLicenseBackdropButton),
                (nameof(backToCreditsButton), backToCreditsButton),
                (nameof(optionButton), optionButton),
                (nameof(optionModal), optionModal),
                (nameof(optionBackdropButton), optionBackdropButton),
                (nameof(closeOptionButton), closeOptionButton),
                (nameof(bgmSlider), bgmSlider),
                (nameof(sfxSlider), sfxSlider)))
        {
            return;
        }

        startButton.onClick.AddListener(HandleStart);
        creditsButton.onClick.AddListener(ShowCredits);
        creditsBackdropButton.onClick.AddListener(HideCredits);
        closeCreditsButton.onClick.AddListener(HideCredits);
        openOflLicenseButton.onClick.AddListener(ShowOflLicense);
        oflLicenseBackdropButton.onClick.AddListener(ReturnToCredits);
        backToCreditsButton.onClick.AddListener(ReturnToCredits);

        optionButton.onClick.AddListener(ShowOption);
        optionBackdropButton.onClick.AddListener(HideOption);
        closeOptionButton.onClick.AddListener(HideOption);

        // 保存されている音量をつまみに反映してから、動かしたときの処理をつなぐ。
        // 逆にすると、反映した時点で決定音が鳴り、保存済みの値を書き戻すことになる。
        bgmSlider.SetValueWithoutNotify(AudioManager.BgmVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.SfxVolume);
        bgmSlider.onValueChanged.AddListener(value => AudioManager.BgmVolume = value);
        sfxSlider.onValueChanged.AddListener(HandleSfxChanged);

        SetCreditsVisible(false);
        SetOflLicenseVisible(false);
        SetOptionVisible(false);
    }

    private void ShowOption()
    {
        AppServices.PlayConfirm();
        SetOptionVisible(true);
    }

    private void HideOption()
    {
        AppServices.PlayConfirm();
        SetOptionVisible(false);
    }

    private void SetOptionVisible(bool visible)
    {
        SetModalVisible(optionModal, visible);
    }

    /// <summary>効果音の音量を変えたら、その音量で 1 度鳴らして聞かせる。</summary>
    /// <remarks>
    /// **鳴らさないと、動かしても何が変わったのか分からない。** BGM は鳴り続けているため要らない。
    /// </remarks>
    private void HandleSfxChanged(float value)
    {
        AudioManager.SfxVolume = value;
        AudioManager.PlaySfx(AudioCue.UiConfirm);
    }

    private void HandleStart()
    {
        AppServices.PlayConfirm();
        GameFlowController.EnsureInstance().OpenDifficultySelect();
    }

    private void ShowCredits()
    {
        AppServices.PlayConfirm();
        SetCreditsVisible(true);
        SetOflLicenseVisible(false);
    }

    private void HideCredits()
    {
        AppServices.PlayConfirm();
        SetCreditsVisible(false);
        SetOflLicenseVisible(false);
    }

    private void ShowOflLicense()
    {
        AppServices.PlayConfirm();
        SetCreditsVisible(false);
        SetOflLicenseVisible(true);
    }

    private void ReturnToCredits()
    {
        AppServices.PlayConfirm();
        SetOflLicenseVisible(false);
        SetCreditsVisible(true);
    }

    private void SetCreditsVisible(bool visible)
    {
        SetModalVisible(creditsModal, visible);
    }

    private void SetOflLicenseVisible(bool visible)
    {
        SetModalVisible(oflLicenseModal, visible);
    }

    private static void SetModalVisible(CanvasGroup modal, bool visible)
    {
        modal.alpha = visible ? 1f : 0f;
        modal.interactable = visible;
        modal.blocksRaycasts = visible;
    }
}
