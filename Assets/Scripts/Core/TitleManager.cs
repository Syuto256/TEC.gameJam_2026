using UnityEngine;
using UnityEngine.UI;

/// <summary>Title シーンの入口。ボタンを遷移につなぐだけを担当する。</summary>
/// <remarks>文字・配色・配置は <c>Title.unity</c> の Hierarchy と Inspector で調整する。</remarks>
public sealed class TitleManager : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private CanvasGroup creditsModal;
    [SerializeField] private Button creditsBackdropButton;
    [SerializeField] private Button closeCreditsButton;

    private void Start()
    {
        AppServices.Ensure();
        if (!SceneUiValidation.Require(
                this,
                (nameof(startButton), startButton),
                (nameof(creditsButton), creditsButton),
                (nameof(creditsModal), creditsModal),
                (nameof(creditsBackdropButton), creditsBackdropButton),
                (nameof(closeCreditsButton), closeCreditsButton)))
        {
            return;
        }

        startButton.onClick.AddListener(HandleStart);
        creditsButton.onClick.AddListener(ShowCredits);
        creditsBackdropButton.onClick.AddListener(HideCredits);
        closeCreditsButton.onClick.AddListener(HideCredits);
        SetCreditsVisible(false);
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
    }

    private void HideCredits()
    {
        AppServices.PlayConfirm();
        SetCreditsVisible(false);
    }

    private void SetCreditsVisible(bool visible)
    {
        creditsModal.alpha = visible ? 1f : 0f;
        creditsModal.interactable = visible;
        creditsModal.blocksRaycasts = visible;
    }
}
