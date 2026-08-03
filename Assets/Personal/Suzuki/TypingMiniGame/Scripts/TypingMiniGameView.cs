using TMPro;
using UnityEngine;

/// <summary>
/// タイピングミニゲームの表示を担当する。日本語フォントは Inspector で後から割り当てる。
/// </summary>
public sealed class TypingMiniGameView : MonoBehaviour
{
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text romanizationText;
    [SerializeField] private TMP_Text acceptedInputText;
    [SerializeField] private TMP_Text remainingInputText;
    [SerializeField] private TMP_Text missCountText;
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private TMP_Text resultText;

    public void ShowQuestion(string question, string romanization)
    {
        SetText(questionText, $"お題: {question}");
        SetText(romanizationText, $"ローマ字: {romanization}");
        SetText(resultText, string.Empty);
    }

    public void ShowProgress(string acceptedInput, string remainingInput, int missCount, int maxMissCount, float timeRemaining)
    {
        SetText(acceptedInputText, $"入力済み: {acceptedInput}");
        SetText(remainingInputText, $"残り: {remainingInput}");
        SetText(missCountText, $"ミス: {missCount} / {maxMissCount}");
        SetText(timeRemainingText, $"残り時間: {Mathf.Max(0f, timeRemaining):F1} 秒");
    }

    public void ShowResult(string result)
    {
        SetText(resultText, result);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
