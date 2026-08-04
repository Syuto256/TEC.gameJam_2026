using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD へ渡す 1 フレーム分の表示値。</summary>
public readonly struct HudSnapshot
{
    public HudSnapshot(int hp, int maxHp, int score, float remainingTimeSec, bool isEndless, GameDifficulty difficulty)
    {
        Hp = hp;
        MaxHp = maxHp;
        Score = score;
        RemainingTimeSec = remainingTimeSec;
        IsEndless = isEndless;
        Difficulty = difficulty;
    }

    public int Hp { get; }
    public int MaxHp { get; }
    public int Score { get; }
    public float RemainingTimeSec { get; }
    public bool IsEndless { get; }
    public GameDifficulty Difficulty { get; }
}

/// <summary>HP・残り時間・スコア・難易度の表示と、ポーズ要求だけを担当する。</summary>
public sealed class HudView : MonoBehaviour
{
    [Header("【必須】")]
    [Tooltip("Image Type を Filled にしたバー。fillAmount で HP 比率を表示する。")]
    [SerializeField] private Image hpBarFill;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button pauseButton;

    [Header("【任意】")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI difficultyText;

    private int lastHp = int.MinValue;
    private int lastScore = int.MinValue;
    private string lastTimeText;
    private bool initialized;

    /// <summary>ポーズボタンが押された。</summary>
    public event Action PauseRequested;

    /// <summary>参照を検証し、自身の入力を配線する。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this, (nameof(hpBarFill), hpBarFill), (nameof(timeText), timeText),
                (nameof(scoreText), scoreText), (nameof(pauseButton), pauseButton)))
        {
            return false;
        }

        pauseButton.onClick.AddListener(() => PauseRequested?.Invoke());
        initialized = true;
        return true;
    }

    public void Render(in HudSnapshot snapshot)
    {
        if (!initialized)
        {
            return;
        }

        if (snapshot.Hp != lastHp)
        {
            lastHp = snapshot.Hp;
            hpBarFill.fillAmount = snapshot.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)snapshot.Hp / snapshot.MaxHp);
            if (hpText != null)
            {
                hpText.text = snapshot.Hp.ToString();
            }
        }

        if (snapshot.Score != lastScore)
        {
            lastScore = snapshot.Score;
            scoreText.text = snapshot.Score.ToString();
        }

        var time = FormatTime(snapshot.RemainingTimeSec, snapshot.IsEndless);
        if (time != lastTimeText)
        {
            lastTimeText = time;
            timeText.text = time;
        }

        if (difficultyText != null)
        {
            difficultyText.text = snapshot.Difficulty.ToString();
        }
    }

    private static string FormatTime(float remainingSec, bool isEndless)
    {
        if (isEndless)
        {
            return "--:--";
        }

        var totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSec));
        return (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
    }
}
