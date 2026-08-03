using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MiniGameCatalog", menuName = "Game/Mini Game Catalog")]
public sealed class MiniGameCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public TaskKind kind;
        public Sprite icon;
        public MiniGameBase prefab;
        [Min(0f)] public float[] timeLimitsByLevel = { 7f, 7f, 7f, 7f };

        public float GetTimeLimit(int level)
        {
            if (timeLimitsByLevel == null || timeLimitsByLevel.Length == 0) return 0f;
            return timeLimitsByLevel[Mathf.Clamp(level - 1, 0, timeLimitsByLevel.Length - 1)];
        }
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public bool TryGetEntry(TaskKind kind, out Entry entry)
    {
        entry = entries.Find(candidate => candidate != null && candidate.kind == kind);
        return entry != null;
    }
}
