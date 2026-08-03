using System;
using System.Collections.Generic;

/// <summary>
/// ローマ字候補に対する、1 文字単位の入力状態を管理する Unity 非依存クラス。
/// </summary>
public sealed class TypingInputEvaluator
{
    private readonly List<string> allCandidates;
    private readonly string canonicalCandidate;
    private List<string> activeCandidates;

    public TypingInputEvaluator(IReadOnlyList<string> candidates, string canonicalCandidate)
    {
        if (candidates == null || candidates.Count == 0)
        {
            throw new ArgumentException("ローマ字候補がありません。", nameof(candidates));
        }

        allCandidates = new List<string>(candidates);
        this.canonicalCandidate = canonicalCandidate;
        activeCandidates = new List<string>(allCandidates);
    }

    public string AcceptedInput { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; }

    public bool TryInput(char input)
    {
        if (IsCompleted || !char.IsLetter(input))
        {
            return false;
        }

        var nextInput = AcceptedInput + char.ToLowerInvariant(input);
        var matchedCandidates = new List<string>();
        foreach (var candidate in activeCandidates)
        {
            if (candidate.StartsWith(nextInput, StringComparison.Ordinal))
            {
                matchedCandidates.Add(candidate);
            }
        }

        if (matchedCandidates.Count == 0)
        {
            return false;
        }

        AcceptedInput = nextInput;
        activeCandidates = matchedCandidates;
        foreach (var candidate in activeCandidates)
        {
            if (candidate.Length == AcceptedInput.Length)
            {
                IsCompleted = true;
                break;
            }
        }

        return true;
    }

    public string GetDisplayCandidate()
    {
        if (canonicalCandidate != null && canonicalCandidate.StartsWith(AcceptedInput, StringComparison.Ordinal))
        {
            return canonicalCandidate;
        }

        return activeCandidates[0];
    }

    public string GetRemainingInput()
    {
        var displayCandidate = GetDisplayCandidate();
        return displayCandidate.Length > AcceptedInput.Length
            ? displayCandidate.Substring(AcceptedInput.Length)
            : string.Empty;
    }
}
