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

    [Header("【待機列の設定】")]
    [Tooltip("表示枠が埋まっているときに、あふれたタスクをどう扱うか。全難易度で共通。")]
    public TaskQueueSettings taskQueue;

    [Header("【難易度ごとの設定】")]
    [Tooltip("難易度選択で選ばれた難易度の設定を、ここから探して使う。\n" +
             "行が無い難易度は、上の【全体ゲーム設定】の値で動く。\n" +
             "行を足した直後は全項目が 0 になるため、必ず全部の値を入れること（0 のままの項目は上の値で補われる）。")]
    public List<DifficultyProfile> difficultyProfiles = new List<DifficultyProfile>();

    [Header("【チュートリアルの設定】")]
    [Tooltip("チュートリアルとして Game シーンを開いたときだけ使う値。\n" +
             "チュートリアルは難易度の 1 つではないため、上の【難易度ごとの設定】には行を作らない。\n" +
             "難易度そのものは選ばれたものをそのまま使い、ここの値だけを上書きする。")]
    public TutorialSettings tutorial = new TutorialSettings();

    /// <summary>チュートリアルとして走らせるときだけ効く上書き。</summary>
    /// <remarks>
    /// **本編との違いはここにある 5 つだけである。** 以前は Tutorial シーン側の
    /// <c>MainGameController</c> に直接書かれており、シーンを複製しないと成立しなかった。
    /// </remarks>
    [Serializable]
    public class TutorialSettings
    {
        [Tooltip("自動でタスクを出すか。チュートリアルは案内の順に出題するため false。")]
        public bool enableAutoSpawn = false;

        [Tooltip("一斉飛来（ラッシュ）を起こすか。チュートリアル中は起こさない。")]
        public bool enableTaskRush = false;

        [Tooltip("タスクの制限時間（秒）。案内を読む間に期限切れにならないよう長く取る。")]
        [Min(0f)] public float taskLifetimeSec = 99f;

        [Tooltip("ミニゲームの制限時間（秒）。同じ理由で長く取る。")]
        [Min(0f)] public float miniGameTimeLimitSec = 99f;

        [Tooltip("AI に任せたときの成功率。チュートリアルでは必ず成功させたいので 1（100%）。")]
        [Range(0f, 1f)] public float aiSuccessRate = 1f;
    }

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
    public class TaskQueueSettings
    {
        [Tooltip("待機中のタスクも寿命が減るようにする。\n" +
                 "OFF（既定）だと、表示枠が空いて画面に出た時点から寿命が減り始める。\n" +
                 "ON にすると、一度も画面に出ないまま時間切れになることがある。\n" +
                 "出ていないタスクはクリックできないため、ON はプレイヤーに手立てが無い被弾を生む。")]
        public bool lifetimeTicksWhileQueued;

        [Tooltip("1つのデバイス面で待機列に積めるタスクの上限。0 で無制限。\n" +
                 "上限に達している面には新しいタスクが出ない。")]
        [Min(0)] public int maxQueuedPerSurface;
    }

    [Serializable]
    public class AISettings
    {
        [Tooltip("AIに任せたタスクが成功する確率。1 で必ず成功、0 で必ず失敗。")]
        [Range(0f, 1f)] public float successRate = 0.90f;

        [Tooltip("AIに任せてから結果が出るまでの秒数。短いほどAIが強くなる。\n" +
                 "この時間が吹き出しの円ゲージの長さになる。1 秒を下回ると\n" +
                 "「AIが作業中」の表示が読まれる前に終わってしまう。")]
        public float processDurationSec = 1.50f;

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

        [Tooltip("早く着手したときのボーナス上限。\n" +
                 "タスクの残り寿命が多いうちに着手するほどスコアが伸びる。\n" +
                 "0.5 なら、出現直後に着手した場合が最大で 1.5 倍。0 にするとボーナス無し。")]
        public float maxTimeBonusAdd = 0.50f;

        [Header("【コンボ】")]
        [Tooltip("コンボが1段増えるごとに上乗せされるスコア倍率。\n" +
                 "0.1 なら 1 コンボごとに +10%（2コンボ目が1.1倍、3コンボ目が1.2倍）。\n" +
                 "0 にするとコンボでスコアが変わらなくなる。")]
        [Min(0f)] public float comboScoreAddPerCombo = 0.10f;

        [Tooltip("コンボ倍率の上限。2 なら何コンボ繋いでも最大2倍で頭打ちになる。\n" +
                 "1 にするとコンボによるスコア上昇が実質的に無くなる。")]
        [Min(1f)] public float maxComboMultiplier = 2.00f;

        [Tooltip("何コンボごとに専用の効果音を鳴らすか。5 なら 5・10・15 コンボで鳴る。\n" +
                 "節目では通常の成功音のかわりにこの音が鳴る（同時には鳴らさない）。\n" +
                 "0 にすると鳴らさず、常に通常の成功音になる。")]
        [Min(0)] public int comboMilestoneInterval = 5;
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

        [Tooltip("1つのデバイス面に同時に「表示」できるタスクの数。\n" +
                 "上限に達している面で発生したタスクは、枠が空くまで待機列に積まれる。\n" +
                 "画面の左右の帯には吹き出しが縦に2つまでしか入らないため、実質の上限は4。")]
        [Min(1)] public int maxTasksPerSurface = 4;

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
