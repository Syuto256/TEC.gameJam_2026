using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>ミニゲームの登録簿。1 種別につき 1 行を持つ。</summary>
/// <remarks>
/// ミニゲームを 1 本追加・差し替えする手順は、Prefab を作ってこのアセットへ行を 1 つ足すだけである。
/// Scene もゲーム進行コードも変更しない。ミニゲーム固有のデータ（問題集など）は
/// 各ミニゲームの Prefab が自分で持ち、このカタログには載せない。
/// </remarks>
[CreateAssetMenu(fileName = "MiniGameCatalog", menuName = "Game/Mini Game Catalog")]
public sealed class MiniGameCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("このミニゲームが担当するタスクの種別。同じ種別を2行に書かないこと。")]
        public TaskKind kind;

        [Tooltip("タスクの吹き出しに出す名前。空にすると種別名がそのまま出る。")]
        public string displayName;

        [Tooltip("タスクの吹き出しに出すアイコン。無くてもよい。")]
        public Sprite icon;

        [Tooltip("この種別のタスクを選んだときに開くミニゲームのPrefab。\n" +
                 "Assets/Prefabs/MiniGames/ の中から選ぶ。")]
        public MiniGameBase prefab;

        [Tooltip("問題レベル1〜4それぞれの制限時間（秒）。左から順にレベル1・2・3・4。\n" +
                 "短くすると難しくなる。要素が4つ未満のときは、足りない分に最後の値が使われる。")]
        [Min(0f)] public float[] timeLimitsByLevel = { 7f, 7f, 7f, 7f };

        public float GetTimeLimit(int level)
        {
            if (timeLimitsByLevel == null || timeLimitsByLevel.Length == 0)
            {
                return 0f;
            }

            return timeLimitsByLevel[Mathf.Clamp(level - 1, 0, timeLimitsByLevel.Length - 1)];
        }
    }

    [Tooltip("ミニゲームの一覧。1種別につき1行。\n" +
             "ミニゲームを1本増やすときは、ここに1行足してからタスク出現表にも足す。")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    public bool TryGetEntry(TaskKind kind, out Entry entry)
    {
        entry = entries?.Find(candidate => candidate != null && candidate.kind == kind);
        return entry != null;
    }

    /// <summary>登録内容を検証する。開始時に一度だけ呼び、不備をすべて列挙する。</summary>
    public bool Validate()
    {
        if (entries == null || entries.Count == 0)
        {
            Debug.LogError("MiniGameCatalog (" + name + "): 登録が 1 件もありません。", this);
            return false;
        }

        var valid = true;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null)
            {
                Debug.LogError("MiniGameCatalog (" + name + "): entries[" + i + "] が空です。", this);
                valid = false;
                continue;
            }

            if (entry.prefab == null)
            {
                Debug.LogError("MiniGameCatalog (" + name + "): " + entry.kind + " の prefab が未設定です。", this);
                valid = false;
            }
            else if (!(entry.prefab.transform is RectTransform))
            {
                Debug.LogError(
                    "MiniGameCatalog (" + name + "): " + entry.kind + " の prefab のルートに RectTransform がありません。", this);
                valid = false;
            }

            for (var j = i + 1; j < entries.Count; j++)
            {
                if (entries[j] != null && entries[j].kind == entry.kind)
                {
                    Debug.LogError("MiniGameCatalog (" + name + "): kind が重複しています -> " + entry.kind, this);
                    valid = false;
                }
            }
        }

        return valid;
    }
}
