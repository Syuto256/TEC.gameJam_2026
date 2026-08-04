using System;
using System.Collections.Generic;

namespace Overwork.MiniGames.Qte
{
    /// <summary>キーを 1 回押したときの判定結果。</summary>
    public enum QtePressResult
    {
        /// <summary>お題に使われていないキーだった。何も起きない。</summary>
        Ignored,

        /// <summary>次に押すべきキーだった。</summary>
        Correct,

        /// <summary>お題のキーではあるが、順番が違った。</summary>
        Wrong
    }

    /// <summary>「どのキーを何番目に押すか」だけを持つお題。表示にも入力装置にも依存しない。</summary>
    /// <remarks>
    /// キーは <c>0</c> から始まる番号で扱う。この番号が実際のどのキーを指すかは
    /// <see cref="QteMiniGame"/> が Prefab のキー一覧で決める。
    /// ここを表示から切り離しているため、判定だけを EditMode テストで確かめられる。
    /// </remarks>
    public sealed class QteSequence
    {
        private readonly int[] keys;

        public QteSequence(IReadOnlyList<int> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                throw new ArgumentException("お題が空です。", nameof(keys));
            }

            this.keys = new int[keys.Count];
            for (var index = 0; index < keys.Count; index++)
            {
                this.keys[index] = keys[index];
            }
        }

        /// <summary>お題の長さ。</summary>
        public int Length => keys.Length;

        /// <summary>ここまでに正しく押せた数。</summary>
        public int Progress { get; private set; }

        public bool IsComplete => Progress >= keys.Length;

        /// <summary>次に押すべきキー番号。完了後は -1 を返す。</summary>
        public int CurrentKey => IsComplete ? -1 : keys[Progress];

        public int KeyAt(int index) => keys[index];

        /// <summary>キーを 1 つ押す。正解なら 1 つ進む。</summary>
        public QtePressResult Press(int keyId)
        {
            if (IsComplete || keyId < 0)
            {
                return QtePressResult.Ignored;
            }

            if (keyId != keys[Progress])
            {
                return QtePressResult.Wrong;
            }

            Progress++;
            return QtePressResult.Correct;
        }

        /// <summary>進捗を最初に戻す。お題そのものは変えない。</summary>
        public void Restart()
        {
            Progress = 0;
        }

        /// <summary>お題の並びをランダムに作る。同じキーが 2 つ続かないようにする。</summary>
        /// <remarks>
        /// 並びが読み取れないと理不尽になるため、隣り合う重複だけは避けている。
        /// キーが 1 種類しかない場合は避けようがないので、そのまま同じキーが並ぶ。
        /// </remarks>
        public static int[] BuildRandomKeys(int poolSize, int length, Random random)
        {
            if (poolSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(poolSize), "キーが 1 種類もありません。");
            }

            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "お題の長さが 0 以下です。");
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var result = new int[length];
            var previous = -1;
            for (var index = 0; index < length; index++)
            {
                var picked = random.Next(poolSize);
                if (poolSize > 1 && picked == previous)
                {
                    picked = (picked + 1 + random.Next(poolSize - 1)) % poolSize;
                }

                result[index] = picked;
                previous = picked;
            }

            return result;
        }
    }
}
