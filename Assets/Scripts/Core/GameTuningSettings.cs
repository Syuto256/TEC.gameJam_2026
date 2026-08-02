using System;
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
        public float cooldownSec = 2.00f;
        public float scoreMultiplier = 0.60f;
    }

    [Serializable]
    public class ScoreSettings
    {
        public int baseScoreDiff1 = 100;
        public int baseScoreDiff2 = 150;
        public int baseScoreDiff3 = 220;
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
}