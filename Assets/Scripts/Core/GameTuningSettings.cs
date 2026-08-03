using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameTuningSettings", menuName = "Game/GameTuningSettings")]
public class GameTuningSettings : ScriptableObject
{
    [Header("【全体ゲーム設定】")]
    [Tooltip("1プレイの制限時間（秒）")]
    public float gameDurationSec = 180f;
    [Tooltip("プレイヤーの最大HP")]
    public int maxHP = 100;

    [Header("【ダメージ設定】")]
    public DamageSettings damage;

    [Header("【AI処理設定】")]
    public AISettings ai;

    [Header("【スコア設定】")]
    public ScoreSettings score;

    [Header("【ミニゲーム制限時間（秒）】")]
    public MiniGameTimeLimits miniGameTimes;

    [Header("Difficulty profiles")]
    [Tooltip("Difficulty-select used by the shared game loop. Empty entries use the legacy values above as a fallback.")]
    public List<DifficultyProfile> difficultyProfiles = new List<DifficultyProfile>();

    [Serializable]
    public class DamageSettings
    {
        public int playerFail = 5;
        public int aiFail = 5;
        public int expired = 8;
    }

    [Serializable]
    public class AISettings
    {
        [Range(0f, 1f)] public float successRate = 0.90f;
        public float processDurationSec = 0.40f;
        [Tooltip("0 permits concurrent AI requests. A positive value enables a global request cooldown.")]
        public float cooldownSec = 0.00f;
        public float scoreMultiplier = 0.60f;
    }

    [Serializable]
    public class ScoreSettings
    {
        public int baseScoreDiff1 = 100;
        public int baseScoreDiff2 = 150;
        public int baseScoreDiff3 = 220;
        public int baseScoreDiff4 = 300;
        public int craftPointsDiff1 = 10;
        public int craftPointsDiff2 = 15;
        public int craftPointsDiff3 = 25;
        public float maxTimeBonusAdd = 0.50f;
    }

    [Serializable]
    public class MiniGameTimeLimits
    {
        public float typing = 7.0f;
        public float dragDrop = 8.0f;
        public float qte = 1.5f; // 1入力あたりの時間
        public float timing = 6.5f;
        public float rapidClick = 4.0f;
        public float tracing = 7.0f;
    }

    [Serializable]
    public class DifficultyProfile
    {
        public GameDifficulty difficulty;
        [Min(0f)] public float durationSec = 180f;
        [Min(1)] public int maxHp = 100;
        [Min(0.1f)] public float spawnIntervalSec = 5f;
        [Min(0.1f)] public float taskLifetimeSec = 20f;
        [Min(1)] public int maxTasksPerSurface = 2;
        [Range(1, 4)] public int startingTaskLevel = 1;
        [Range(1, 4)] public int maxTaskLevel = 1;
        [Min(0f)] public float taskLevelIncreaseIntervalSec = 45f;
        public bool isEndless;
    }

    public DifficultyProfile GetDifficultyProfile(GameDifficulty difficulty)
    {
        var profile = difficultyProfiles?.Find(candidate => candidate != null && candidate.difficulty == difficulty);
        if (profile != null)
        {
            return profile;
        }

        return new DifficultyProfile
        {
            difficulty = difficulty,
            durationSec = gameDurationSec,
            maxHp = maxHP,
            isEndless = difficulty == GameDifficulty.Endless
        };
    }

    public int GetBaseScoreForTaskLevel(int level)
    {
        if (level <= 1) return score.baseScoreDiff1;
        if (level == 2) return score.baseScoreDiff2;
        if (level == 3) return score.baseScoreDiff3;
        return score.baseScoreDiff4;
    }
}
