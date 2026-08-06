using System;
using UnityEngine;

namespace Overwork.MiniGames.DragDrop
{
    /// <summary>仕分けるファイルの種類。</summary>
    public enum SortingFileKind
    {
        Document,
        Image,
        Audio,
        Script
    }

    /// <summary>ファイルとフォルダの種類ごとの見た目。</summary>
    [Serializable]
    public sealed class SortingKindStyle
    {
        public SortingFileKind kind;

        [Tooltip("ファイルのアイコン。色は絵が持っているので染めない。")]
        public Sprite fileIcon;

        [Tooltip("フォルダを染める色。フォルダの絵は無地なので、種類の見分けはこの色が担う。")]
        public Color folderTint = Color.white;

        [Tooltip("フォルダの下に出す名前。")]
        public string label;
    }

    /// <summary>仕分けミニゲームのレベル別生成設定。</summary>
    [Serializable]
    public sealed class SortingLevelSetting
    {
        [Range(1, 4)] public int level = 1;

        [Tooltip("整理するファイルの枚数。")]
        [Min(1)] public int fileCount = 3;

        [Tooltip("同時に出る種類数の上限。1 なら 1 種類だけが出る。")]
        [Range(1, 4)] public int maxKinds = 1;

        [Tooltip("このレベルで失敗になるまでの誤配置数。")]
        [Min(1)] public int allowedMisses = 2;
    }
}
