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
        var fallback = new DifficultyProfile
        {
            difficulty = difficulty,
            durationSec = gameDurationSec,
            maxHp = maxHP,
            isEndless = difficulty == GameDifficulty.Endless
        };

        var profile = difficultyProfiles?.Find(candidate => candidate != null && candidate.difficulty == difficulty);
        return profile == null ? fallback : Normalize(profile, fallback);
    }

    /// <summary>
    /// 未入力（0）の項目を既定値で埋めた複製を返す。
    /// Inspector のリストに行を足すと全項目が 0 になり、そのままでは
    /// 最大 HP 0 で即ゲームオーバーになるため、ここで吸収する。
    /// </summary>
    private static DifficultyProfile Normalize(DifficultyProfile source, DifficultyProfile fallback)
    {
        var result = new DifficultyProfile
        {
            difficulty = source.difficulty,
            durationSec = source.durationSec > 0f ? source.durationSec : fallback.durationSec,
            maxHp = source.maxHp > 0 ? source.maxHp : fallback.maxHp,
            spawnIntervalSec = source.spawnIntervalSec > 0f ? source.spawnIntervalSec : fallback.spawnIntervalSec,
            taskLifetimeSec = source.taskLifetimeSec > 0f ? source.taskLifetimeSec : fallback.taskLifetimeSec,
            maxTasksPerSurface = source.maxTasksPerSurface > 0 ? source.maxTasksPerSurface : fallback.maxTasksPerSurface,
            startingTaskLevel = source.startingTaskLevel > 0 ? source.startingTaskLevel : fallback.startingTaskLevel,
            maxTaskLevel = source.maxTaskLevel > 0 ? source.maxTaskLevel : fallback.maxTaskLevel,
            taskLevelIncreaseIntervalSec = source.taskLevelIncreaseIntervalSec,
            isEndless = source.isEndless || fallback.isEndless
        };

        if (result.maxTaskLevel < result.startingTaskLevel)
        {
            result.maxTaskLevel = result.startingTaskLevel;
        }

        return result;
    }

    public int GetBaseScoreForTaskLevel(int level)
    {
        if (level <= 1) return score.baseScoreDiff1;
        if (level == 2) return score.baseScoreDiff2;
        if (level == 3) return score.baseScoreDiff3;
        return score.baseScoreDiff4;
    }
}
