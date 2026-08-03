using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TypingWordEntry
{
    [SerializeField] private string displayText;
    [SerializeField] private string reading;
    [SerializeField] private TypingDifficulty difficulty = TypingDifficulty.Easy;

    public string DisplayText => displayText;
    public string Reading => reading;
    public TypingDifficulty Difficulty => difficulty;

    public bool IsValid => !string.IsNullOrWhiteSpace(displayText) && !string.IsNullOrWhiteSpace(reading);
}

/// <summary>
/// 全難易度のタイピング問題を一元管理する ScriptableObject。
/// </summary>
[CreateAssetMenu(fileName = "TypingWordDatabase", menuName = "Suzuki/Typing Mini Game/Word Database")]
public sealed class TypingWordDatabase : ScriptableObject
{
    [SerializeField] private List<TypingWordEntry> entries = new List<TypingWordEntry>();

    public bool TryGetRandomEntry(TypingDifficulty difficulty, out TypingWordEntry entry)
    {
        var matchingEntries = new List<TypingWordEntry>();
        foreach (var candidate in entries)
        {
            if (candidate != null && candidate.IsValid && candidate.Difficulty == difficulty)
            {
                matchingEntries.Add(candidate);
            }
        }

        if (matchingEntries.Count == 0)
        {
            entry = null;
            return false;
        }

        entry = matchingEntries[UnityEngine.Random.Range(0, matchingEntries.Count)];
        return true;
    }
}
