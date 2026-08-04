using System;
using System.Collections.Generic;

namespace Overwork.MiniGames.Typing
{
    /// <summary>複数のローマ字候補に対し、途中一致を維持して入力を評価する。</summary>
    public sealed class TypingInputEvaluator
    {
        private readonly string canonicalCandidate;
        private List<string> activeCandidates;

        public TypingInputEvaluator(IReadOnlyList<string> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("At least one romanization candidate is required.", nameof(candidates));
            }

            activeCandidates = new List<string>();
            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    activeCandidates.Add(candidate.ToLowerInvariant());
                }
            }

            if (activeCandidates.Count == 0)
            {
                throw new ArgumentException("No valid romanization candidate was supplied.", nameof(candidates));
            }

            canonicalCandidate = activeCandidates[0];
        }

        public string AcceptedInput { get; private set; } = string.Empty;
        public bool IsCompleted { get; private set; }
        public string DisplayCandidate => canonicalCandidate.StartsWith(AcceptedInput, StringComparison.Ordinal) ? canonicalCandidate : activeCandidates[0];
        public string RemainingInput => DisplayCandidate.Substring(AcceptedInput.Length);

        public bool TryInput(char input)
        {
            if (IsCompleted || !char.IsLetter(input))
            {
                return false;
            }

            var nextInput = AcceptedInput + char.ToLowerInvariant(input);
            var matched = activeCandidates.FindAll(candidate => candidate.StartsWith(nextInput, StringComparison.Ordinal));
            if (matched.Count == 0)
            {
                return false;
            }

            AcceptedInput = nextInput;
            activeCandidates = matched;
            IsCompleted = activeCandidates.Exists(candidate => candidate.Length == AcceptedInput.Length);
            return true;
        }
    }
}
