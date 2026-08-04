using UnityEngine;

public static class HighScoreManager
{
    private const string HighScoreKeyPrefix = "HighScore_";

    /// <summary>指定した難易度のハイスコアを取得する</summary>
    public static int GetHighScore(GameDifficulty difficulty)
    {
        return PlayerPrefs.GetInt(HighScoreKeyPrefix + difficulty.ToString(), 0);
    }

    /// <summary>スコアがハイスコアを更新していたら保存する</summary>
    public static bool SaveHighScore(GameDifficulty difficulty, int newScore)
    {
        int currentHighScore = GetHighScore(difficulty);
        if (newScore > currentHighScore)
        {
            PlayerPrefs.SetInt(HighScoreKeyPrefix + difficulty.ToString(), newScore);
            PlayerPrefs.Save();
            return true; // 更新された
        }
        return false; // 更新されなかった
    }
}