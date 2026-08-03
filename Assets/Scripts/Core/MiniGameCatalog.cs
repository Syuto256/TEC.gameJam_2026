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
        [Tooltip("担当するタスク種別。カタログ内で重複させない。")]
        public TaskKind kind;

        [Tooltip("タスク吹き出しに出す名前。空ならタスク種別名をそのまま使う。")]
        public string displayName;

        [Tooltip("タスク吹き出しに出すアイコン。任意。")]
        public Sprite icon;

        [Tooltip("MiniGameHost へ生成する Prefab。ルートに MiniGameBase 派生と RectTransform が必要。")]
        public MiniGameBase prefab;

        [Tooltip("タスクレベル 1〜4 の制限時間（秒）。要素が足りない分は最後の値を使う。")]
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
