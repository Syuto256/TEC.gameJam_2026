using System;
using System.Collections.Generic;
using UnityEngine;

namespace Overwork.MiniGames.Tracing
{
    [Serializable]
    public sealed class TracingPathEntry
    {
        [Tooltip("この経路を出す問題レベル（1〜4）。数字が大きいほど後半に出る。")]
        [Range(1, 4)] public int level;

        [Tooltip("なぞる経路の通過点。盤面の左下が (0,0)、右上が (1,1) の割合で指定する。\n" +
                 "例: (0.5, 0.5) は盤面の中央。\n" +
                 "2点で直線、点を増やすほど折れ線が複雑になる。最低2点必要。")]
        public List<Vector2> points = new List<Vector2>();

        [Tooltip("経路からどれだけ離れてもミスにしないかを、盤面の大きさに対する割合で指定する。\n" +
                 "大きいほど簡単になる。")]
        [Range(0.02f, 0.3f)] public float allowedDeviationRatio = 0.09f;

        public TracingPathEntry(int level, params Vector2[] points)
        {
            this.level = level;
            this.points = new List<Vector2>(points);
        }

        public bool IsValid => points != null && points.Count >= 2;
    }

    [CreateAssetMenu(fileName = "TracingPathDatabase", menuName = "Overwork/Mini Games/Tracing Path Database")]
    public sealed class TracingPathDatabase : ScriptableObject
    {
        [Tooltip("なぞりミニゲームの経路一覧。\n" +
                 "出題は、そのときの問題レベルに一致する行からランダムに1件選ばれる。\n" +
                 "各レベルに最低1件は入れること。0件のレベルがあると、そのレベルでは失敗扱いになる。")]
        [SerializeField] private List<TracingPathEntry> paths = new List<TracingPathEntry>
        {
            new TracingPathEntry(1, new Vector2(0.12f, .25f), new Vector2(.38f, .65f), new Vector2(.7f, .45f), new Vector2(.88f, .75f)),
            new TracingPathEntry(2, new Vector2(.1f, .7f), new Vector2(.3f, .3f), new Vector2(.55f, .7f), new Vector2(.82f, .32f)),
            new TracingPathEntry(3, new Vector2(.1f, .2f), new Vector2(.25f, .75f), new Vector2(.48f, .3f), new Vector2(.7f, .75f), new Vector2(.9f, .35f)),
            new TracingPathEntry(4, new Vector2(.08f, .5f), new Vector2(.2f, .8f), new Vector2(.42f, .2f), new Vector2(.6f, .78f), new Vector2(.82f, .2f), new Vector2(.94f, .55f))
        };

        public bool TryGetRandomPath(int level, out TracingPathEntry path)
        {
            var candidates = paths.FindAll(entry => entry != null && entry.IsValid && entry.level == Mathf.Clamp(level, 1, 4));
            if (candidates.Count == 0) { path = null; return false; }
            path = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return true;
        }
    }
}
