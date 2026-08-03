using UnityEngine;
using UnityEngine.UI;

/// <summary>Title シーンの入口。ボタンを遷移につなぐだけを担当する。</summary>
/// <remarks>文字・配色・配置は <c>Title.unity</c> の Hierarchy と Inspector で調整する。</remarks>
public sealed class TitleManager : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private Button startButton;

    private void Start()
    {
        AppServices.Ensure();
        if (!SceneUiValidation.Require(this, (nameof(startButton), startButton)))
        {
            return;
        }

        startButton.onClick.AddListener(HandleStart);
    }

    private void HandleStart()
    {
        AppServices.PlayConfirm();
        GameFlowController.EnsureInstance().OpenDifficultySelect();
    }
}
