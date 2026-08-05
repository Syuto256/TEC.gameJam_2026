using UnityEngine;
using UnityEngine.UI;

public sealed class OptionView : MonoBehaviour
{
    [Header("【設定UI】")]
    [Tooltip("「イージー/ノーマル選択時にチュートリアル確認を表示する」Toggle")]
    [SerializeField] private Toggle showTutorialConfirmToggle;

    private void OnEnable()
    {
        if (showTutorialConfirmToggle != null)
        {
            // 現在の保存値を UI に反映（一時的にリスナーを外して発火を防ぐ）
            showTutorialConfirmToggle.SetIsOnWithoutNotify(GameSettings.ShowTutorialConfirm);
            
            showTutorialConfirmToggle.onValueChanged.RemoveAllListeners();
            showTutorialConfirmToggle.onValueChanged.AddListener(OnConfirmToggleChanged);
        }
    }

    private void OnConfirmToggleChanged(bool isOn)
    {
        // 設定を保存
        GameSettings.ShowTutorialConfirm = isOn;
        Debug.Log($"[Option] チュートリアル確認表示: {isOn}");
    }
}