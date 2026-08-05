using UnityEngine;

public static class GameSettings
{
    private const string KeyShowTutorialConfirm = "ShowTutorialConfirm";

    /// <summary>
    /// チュートリアル確認ダイアログを表示するかどうか（デフォルト: true）
    /// </summary>
    public static bool ShowTutorialConfirm
    {
        get => PlayerPrefs.GetInt(KeyShowTutorialConfirm, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyShowTutorialConfirm, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}