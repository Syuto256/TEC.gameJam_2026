using System;
using System.Collections.Generic;

namespace Overwork.MiniGames.Qte
{
    /// <summary>キーを 1 回押したときの判定結果。</summary>
    public enum QtePressResult
    {
        /// <summary>お題に使わないキーだった。何も起きない。</summary>
        Ignored,

        /// <summary>出ている警告のどれかに当たり、それが閉じた。</summary>
        Cleared,

        /// <summary>お題に使うキーだが、出ている警告のどれにも当たらなかった。</summary>
        Missed
    }

    /// <summary>画面に出ている警告 1 枚分。</summary>
    public sealed class QteAlert
    {
        internal QteAlert(int keyId, int slotIndex, float graceSec)
        {
            KeyId = keyId;
            SlotIndex = slotIndex;
            GraceSec = graceSec;
            RemainingSec = graceSec;
        }

        /// <summary>この警告を閉じるために押すキーの番号。</summary>
        public int KeyId { get; }

        /// <summary>どの位置に出すか。位置そのものは表示側が持つ。</summary>
        public int SlotIndex { get; }

        /// <summary>この 1 枚に与えられた猶予。</summary>
        public float GraceSec { get; }

        /// <summary>残りの猶予。</summary>
        public float RemainingSec { get; internal set; }

        /// <summary>残りの猶予の割合。ゲージの表示に使う。</summary>
        public float RemainingRatio
        {
            get
            {
                if (GraceSec <= 0f)
                {
                    return 0f;
                }

                var ratio = RemainingSec / GraceSec;
                return ratio < 0f ? 0f : ratio > 1f ? 1f : ratio;
            }
        }
    }

    /// <summary>レベルごとに変わる出題の設定。</summary>
    public sealed class QteAlertBoardSettings
    {
        /// <summary>そのミニゲームで出る警告の総数。</summary>
        public int TotalCount = 3;

        /// <summary>次の警告が出るまでの間隔。猶予より短くすると画面に溜まる。</summary>
        public float SpawnIntervalSec = 2f;

        /// <summary>警告 1 枚あたりの猶予。</summary>
        public float GraceSec = 4f;

        /// <summary>同時に出しておける枚数。</summary>
        public int MaxConcurrent = 3;

        /// <summary>何回ミスしたら失敗にするか。</summary>
        public int AllowedMisses = 2;

        /// <summary>使えるキーの種類数。</summary>
        public int KeyPoolSize = 8;

        /// <summary>置き場所の数。</summary>
        public int SlotCount = 4;
    }

    /// <summary>警告が次々出てきて、猶予のうちに対応するキーを押していく盤面。</summary>
    /// <remarks>
    /// 表示にも入力装置にも依存しないため、判定だけを EditMode テストで確かめられる。
    /// キーは <c>0</c> から始まる番号で扱い、その番号が実際のどのキーを指すかは
    /// <see cref="QteMiniGame"/> が Prefab のキー一覧で決める。
    ///
    /// 設計の根拠は Docs/Decisions/2026-08-06-qte-rework.md にある。要点は 3 つ。
    /// ・出ているキーはどれを押してもよい。順番は指定しない（猶予の差で自然に順序が付く）
    /// ・お題に使うキーを押して、どの警告にも当たらなければミス。
    ///   これが無いと、キーボードを端から叩くだけでクリアできてしまう
    /// ・出ている間は同じキーを重複させない。2 枚あるとどちらが閉じるのか分からなくなる
    /// </remarks>
    public sealed class QteAlertBoard
    {
        private readonly QteAlertBoardSettings settings;
        private readonly Random random;
        private readonly List<QteAlert> active = new List<QteAlert>();
        private readonly List<QteAlert> spawnedThisTick = new List<QteAlert>();
        private readonly List<QteAlert> expiredThisTick = new List<QteAlert>();
        private readonly int maxConcurrent;

        private float sinceLastSpawn;
        private bool hasSpawnedOnce;

        public QteAlertBoard(QteAlertBoardSettings settings, Random random)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.TotalCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "出る枚数が 0 以下です。");
            }

            if (settings.GraceSec <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "猶予が 0 以下です。");
            }

            if (settings.KeyPoolSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "キーが 1 種類もありません。");
            }

            if (settings.SlotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "置き場所が 1 つもありません。");
            }

            this.settings = settings;
            this.random = random ?? throw new ArgumentNullException(nameof(random));

            // 同じキーの警告を 2 枚出さないため、同時枚数はキーの種類数を超えられない。
            // 置き場所の数も同じく上限になる。
            maxConcurrent = Math.Max(1, Math.Min(settings.MaxConcurrent,
                Math.Min(settings.KeyPoolSize, settings.SlotCount)));

            NotYetSpawned = settings.TotalCount;
        }

        /// <summary>今画面に出ている警告。</summary>
        public IReadOnlyList<QteAlert> ActiveAlerts => active;

        /// <summary>直近の <see cref="Tick"/> で出た警告。表示を作るのに使う。</summary>
        public IReadOnlyList<QteAlert> SpawnedThisTick => spawnedThisTick;

        /// <summary>直近の <see cref="Tick"/> で猶予切れになった警告。</summary>
        public IReadOnlyList<QteAlert> ExpiredThisTick => expiredThisTick;

        /// <summary>まだ出していない枚数。</summary>
        public int NotYetSpawned { get; private set; }

        public int Misses { get; private set; }

        public int ClearedCount { get; private set; }

        /// <summary>実際に同時に出せる枚数。設定より小さく丸められることがある。</summary>
        public int MaxConcurrent => maxConcurrent;

        public bool IsFailed => Misses >= settings.AllowedMisses;

        public bool IsComplete => !IsFailed && NotYetSpawned == 0 && active.Count == 0;

        /// <summary>時間を進める。猶予切れの処理と、次の警告の出現を行う。</summary>
        public void Tick(float deltaTime)
        {
            spawnedThisTick.Clear();
            expiredThisTick.Clear();

            if (IsFailed || IsComplete || deltaTime <= 0f)
            {
                return;
            }

            ExpireOverdue(deltaTime);

            if (IsFailed)
            {
                return;
            }

            sinceLastSpawn += deltaTime;

            // 最初の 1 枚だけは間隔を待たずに出す。待つと開始直後に何も無い時間ができる。
            while (NotYetSpawned > 0
                   && active.Count < maxConcurrent
                   && (!hasSpawnedOnce || sinceLastSpawn >= settings.SpawnIntervalSec))
            {
                if (!TrySpawnOne())
                {
                    break;
                }

                if (hasSpawnedOnce)
                {
                    sinceLastSpawn -= settings.SpawnIntervalSec;
                }
                else
                {
                    hasSpawnedOnce = true;
                    sinceLastSpawn = 0f;
                }
            }

            // 同時上限で出せなかったぶんの時間は繰り越さない。
            // 繰り越すと、1 枚さばいた瞬間に溜まっていた枚数がまとめて出る。
            if (sinceLastSpawn > settings.SpawnIntervalSec)
            {
                sinceLastSpawn = settings.SpawnIntervalSec;
            }
        }

        /// <summary>キーを 1 つ押す。</summary>
        public QtePressResult Press(int keyId)
        {
            LastCleared = null;

            if (IsFailed || IsComplete || keyId < 0)
            {
                return QtePressResult.Ignored;
            }

            for (var index = 0; index < active.Count; index++)
            {
                if (active[index].KeyId != keyId)
                {
                    continue;
                }

                LastCleared = active[index];
                active.RemoveAt(index);
                ClearedCount++;
                return QtePressResult.Cleared;
            }

            Misses++;
            return QtePressResult.Missed;
        }

        /// <summary>直近の <see cref="Press"/> で閉じた警告。閉じなかったときは null。</summary>
        public QteAlert LastCleared { get; private set; }

        private void ExpireOverdue(float deltaTime)
        {
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var alert = active[index];
                alert.RemainingSec -= deltaTime;
                if (alert.RemainingSec > 0f)
                {
                    continue;
                }

                active.RemoveAt(index);
                expiredThisTick.Add(alert);
                Misses++;
            }
        }

        private bool TrySpawnOne()
        {
            var keyId = PickUnusedKey();
            if (keyId < 0)
            {
                return false;
            }

            var alert = new QteAlert(keyId, PickFreeSlot(), settings.GraceSec);
            active.Add(alert);
            spawnedThisTick.Add(alert);
            NotYetSpawned--;
            return true;
        }

        /// <summary>今出ていないキーの中から選ぶ。</summary>
        private int PickUnusedKey()
        {
            var candidates = new List<int>(settings.KeyPoolSize);
            for (var keyId = 0; keyId < settings.KeyPoolSize; keyId++)
            {
                if (!IsKeyActive(keyId))
                {
                    candidates.Add(keyId);
                }
            }

            return candidates.Count == 0 ? -1 : candidates[random.Next(candidates.Count)];
        }

        /// <summary>空いている置き場所のうち、いちばん小さい番号を返す。</summary>
        /// <remarks>
        /// 抽選しないのは、毎回位置が変わると探し直しになるためである。
        /// 「読んで即押す」ゲームなので、探す時間が入ると致命的になる。
        /// </remarks>
        private int PickFreeSlot()
        {
            for (var slot = 0; slot < settings.SlotCount; slot++)
            {
                if (!IsSlotActive(slot))
                {
                    return slot;
                }
            }

            return 0;
        }

        private bool IsKeyActive(int keyId)
        {
            for (var index = 0; index < active.Count; index++)
            {
                if (active[index].KeyId == keyId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSlotActive(int slotIndex)
        {
            for (var index = 0; index < active.Count; index++)
            {
                if (active[index].SlotIndex == slotIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
