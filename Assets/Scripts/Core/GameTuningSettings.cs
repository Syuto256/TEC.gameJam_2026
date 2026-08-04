using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameTuningSettings", menuName = "Game/GameTuningSettings")]
public class GameTuningSettings : ScriptableObject
{
    [Header("【全体ゲーム設定】")]
    [Tooltip("1プレイの制限時間（秒）。難易度ごとの設定がある場合はそちらが優先される。")]
    public float gameDurationSec = 180f;

    [Tooltip("プレイヤーの最大HP。難易度ごとの設定がある場合はそちらが優先される。")]
    public int maxHP = 100;

    [Header("【ダメージ設定】")]
    [Tooltip("タスクの失敗や放置でHPがどれだけ減るか。全難易度で共通。")]
    public DamageSettings damage;

    [Header("【AI処理設定】")]
    [Tooltip("タスクをAIに任せたときのふるまい。全難易度で共通。")]
    public AISettings ai;

    [Header("【スコア設定】")]
    [Tooltip("タスクを片付けたときに入るスコア。全難易度で共通。")]
    public ScoreSettings score;

    [Header("【難易度ごとの設定】")]
    [Tooltip("難易度選択で選ばれた難易度の設定を、ここから探して使う。\n" +
             "行が無い難易度は、上の【全体ゲーム設定】の値で動く。\n" +
             "行を足した直後は全項目が 0 になるため、必ず全部の値を入れること（0 のままの項目は上の値で補われる）。")]
    public List<DifficultyProfile> difficultyProfiles = new List<DifficultyProfile>();

    [Serializable]
    public class DamageSettings
    {
        [Tooltip("自力でミニゲームに失敗したときに減るHP。")]
        public int playerFail = 5;

        [Tooltip("AIに任せたタスクが失敗したときに減るHP。")]
        public int aiFail = 5;

        [Tooltip("タスクを放置して時間切れになったときに減るHP。")]
        public int expired = 8;
    }

    [Serializable]
    public class AISettings
    {
        [Tooltip("AIに任せたタスクが成功する確率。1 で必ず成功、0 で必ず失敗。")]
        [Range(0f, 1f)] public float successRate = 0.90f;

        [Tooltip("AIに任せてから結果が出るまでの秒数。短いほどAIが強くなる。")]
        public float processDurationSec = 0.40f;

        [Tooltip("次にAIへ依頼できるようになるまでの待ち時間（秒）。\n" +
                 "0 にすると待ち時間なしで、何件でも同時にAIへ任せられる。")]
        public float cooldownSec = 0.00f;

        [Tooltip("AIに任せて成功したときのスコア倍率。\n" +
                 "1 より小さくすると自力で片付けたほうが得になり、自力を選ぶ理由が生まれる。")]
        public float scoreMultiplier = 0.60f;
    }

    [Serializable]
    public class ScoreSettings
    {
        [Tooltip("問題レベル1のタスクを片付けたときの基礎スコア。")]
        public int baseScoreDiff1 = 100;

        [Tooltip("問題レベル2のタスクを片付けたときの基礎スコア。")]
        public int baseScoreDiff2 = 150;

        [Tooltip("問題レベル3のタスクを片付けたときの基礎スコア。")]
        public int baseScoreDiff3 = 220;

        [Tooltip("問題レベル4のタスクを片付けたときの基礎スコア。")]
        public int baseScoreDiff4 = 300;

        [Tooltip("【未使用】現在どこからも参照していない。使う予定が無ければ消してよい。")]
        public int craftPointsDiff1 = 10;

        [Tooltip("【未使用】現在どこからも参照していない。使う予定が無ければ消してよい。")]
        public int craftPointsDiff2 = 15;

        [Tooltip("【未使用】現在どこからも参照していない。使う予定が無ければ消してよい。")]
        public int craftPointsDiff3 = 25;

        [Tooltip("早く着手したときのボーナス上限。\n" +
                 "タスクの残り寿命が多いうちに着手するほどスコアが伸びる。\n" +
                 "0.5 なら、出現直後に着手した場合が最大で 1.5 倍。0 にするとボーナス無し。")]
        public float maxTimeBonusAdd = 0.50f;
    }

    [Serializable]
    public class DifficultyProfile
    {
        [Serializable]
        public class TaskLevelMilestone
        {
            [Tooltip("ゲーム開始から何秒経ったら切り替えるか。0 なら開始直後から。")]
            [Min(0f)] public float elapsedSec;

            [Tooltip("その時点から出すタスクの問題レベル（1〜4）。\n" +
                     "開始レベルと上限レベルの範囲に収められる。")]
            [Range(1, 4)] public int level = 1;
        }

        [Serializable]
        public class SpawnIntervalMilestone
        {
            [Tooltip("ゲーム開始から何秒経ったら切り替えるか。0 なら開始直後から。")]
            [Min(0f)] public float elapsedSec;

            [Tooltip("その時点からのタスク出現間隔（秒）。短いほど忙しくなる。")]
            [Min(0.1f)] public float intervalSec = 5f;
        }

        [Tooltip("この設定を適用する難易度。難易度選択の画面で選ばれたものと一致する行が使われる。")]
        public GameDifficulty difficulty;

        [Tooltip("この難易度での1プレイの制限時間（秒）。時間切れでクリアになる。")]
        [Min(0f)] public float durationSec = 180f;

        [Tooltip("この難易度でのプレイヤーの最大HP。0 になるとゲームオーバー。")]
        [Min(1)] public int maxHp = 100;

        [Tooltip("タスクが出現する間隔（秒）。短いほど忙しくなる。\n" +
                 "下の「出現間隔の切り替え」に行がある場合は、そちらが優先される。")]
        [Min(0.1f)] public float spawnIntervalSec = 5f;

        [Tooltip("タスクが出現してから時間切れになるまでの秒数。短いほど余裕が無くなる。")]
        [Min(0.1f)] public float taskLifetimeSec = 20f;

        [Tooltip("1つのデバイス面に同時に置いておけるタスクの数。\n" +
                 "上限に達している面には新しいタスクが出ない。")]
        [Min(1)] public int maxTasksPerSurface = 2;

        [Tooltip("ゲーム開始時のタスクの問題レベル（1〜4）。数字が大きいほどミニゲームが難しくなる。")]
        [Range(1, 4)] public int startingTaskLevel = 1;

        [Tooltip("このプレイ中に上がりうる問題レベルの上限（1〜4）。\n" +
                 "開始レベルと同じ値にすると、最後までレベルが上がらない。")]
        [Range(1, 4)] public int maxTaskLevel = 1;

        [Tooltip("問題レベルが1段階上がるまでの秒数。\n" +
                 "0 にすると最初から上限レベルで出る。\n" +
                 "下の「問題レベルの切り替え」に行がある場合は、そちらが優先される。")]
        [Min(0f)] public float taskLevelIncreaseIntervalSec = 45f;

        [Tooltip("問題レベルを、決まった時刻で切り替えたい場合に使う。\n" +
                 "1件でも入れると、上の「一定間隔で上げる」設定より優先される。")]
        public List<TaskLevelMilestone> taskLevelMilestones = new List<TaskLevelMilestone>();

        [Tooltip("タスクの出現間隔を、決まった時刻で切り替えたい場合に使う。\n" +
                 "1件でも入れると、上の「出現間隔」の値より優先される。")]
        public List<SpawnIntervalMilestone> spawnIntervalMilestones = new List<SpawnIntervalMilestone>();

        [Tooltip("制限時間なしで遊べるようにする。ONにすると時間切れによるクリアが起きず、HPが尽きるまで続く。")]
        public bool isEndless;

        public int GetTaskLevel(float elapsedSec)
        {
            var startingLevel = Mathf.Clamp(startingTaskLevel, 1, 4);
            var maximumLevel = Mathf.Clamp(maxTaskLevel, startingLevel, 4);
            if (taskLevelMilestones != null && taskLevelMilestones.Count > 0)
            {
                var result = startingLevel;
                var latestTime = float.NegativeInfinity;
                foreach (var milestone in taskLevelMilestones)
                {
                    if (milestone == null || milestone.elapsedSec > elapsedSec || milestone.elapsedSec < latestTime)
                    {
                        continue;
                    }

                    latestTime = milestone.elapsedSec;
                    result = Mathf.Clamp(milestone.level, startingLevel, maximumLevel);
                }

                return result;
            }

            if (taskLevelIncreaseIntervalSec <= 0f)
            {
                return maximumLevel;
            }

            var increases = Mathf.FloorToInt(elapsedSec / taskLevelIncreaseIntervalSec);
            return Mathf.Clamp(startingLevel + increases, startingLevel, maximumLevel);
        }

        public float GetSpawnInterval(float elapsedSec)
        {
            var result = Mathf.Max(0.1f, spawnIntervalSec);
            if (spawnIntervalMilestones == null || spawnIntervalMilestones.Count == 0)
            {
                return result;
            }

            var latestTime = float.NegativeInfinity;
            foreach (var milestone in spawnIntervalMilestones)
            {
                if (milestone == null || milestone.elapsedSec > elapsedSec || milestone.elapsedSec < latestTime)
                {
                    continue;
                }

                latestTime = milestone.elapsedSec;
                result = Mathf.Max(0.1f, milestone.intervalSec);
            }

            return result;
        }
    }

    /// <summary>行が用意されていない難易度を列挙する。</summary>
    /// <remarks>
    /// 行が無い難易度は <see cref="GetDifficultyProfile"/> の既定値で動く。既定値は
    /// <c>maxTaskLevel = 1</c> なので、**最後までレベル 1 のまま = いちばん簡単**になる。
    /// 難易度選択にボタンを足したのに行を足し忘れると、その難易度だけ静かに簡単になるため、
    /// まとめて調べられるようにしている。
    /// </remarks>
    public IReadOnlyList<GameDifficulty> FindMissingDifficultyProfiles()
    {
        var missing = new List<GameDifficulty>();
        foreach (GameDifficulty difficulty in Enum.GetValues(typeof(GameDifficulty)))
        {
            if (difficultyProfiles == null
                || !difficultyProfiles.Exists(candidate => candidate != null && candidate.difficulty == difficulty))
            {
                missing.Add(difficulty);
            }
        }

        return missing;
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
            taskLevelMilestones = source.taskLevelMilestones == null
                ? new List<DifficultyProfile.TaskLevelMilestone>()
                : new List<DifficultyProfile.TaskLevelMilestone>(source.taskLevelMilestones),
            spawnIntervalMilestones = source.spawnIntervalMilestones == null
                ? new List<DifficultyProfile.SpawnIntervalMilestone>()
                : new List<DifficultyProfile.SpawnIntervalMilestone>(source.spawnIntervalMilestones),
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
