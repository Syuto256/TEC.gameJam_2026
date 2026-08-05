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

        /// <summary>キーボードから打てる文字か。打てない文字はミスにも進捗にもしない。</summary>
        /// <remarks>
        /// <para>
        /// **英字に限らないのは、綴りに英字以外が出てくるためである。** 長音符「ー」からは
        /// <c>-</c> が、「ん」からは <c>n'</c> が作られる。英熟語をお題にすれば空白も要る。
        /// 英字だけを通していたころは「コーヒー」が <c>ko-hi-</c> にしかならず、
        /// <c>-</c> がミスとして数えられて必ず失敗していた。
        /// </para>
        /// <para>
        /// **かな・漢字がここで落ちるのは意図した動きである。** 日本語入力が有効なまま打つと
        /// かなが飛んでくるが、それをミスとして数えると打ち始めた瞬間に失敗する。
        /// 退避キー（BackSpace など）も同じ理由でここに含めない。
        /// </para>
        /// </remarks>
        public static bool IsTypableCharacter(char value)
        {
            return value >= ' ' && value <= '~';
        }

        public bool TryInput(char input)
        {
            if (IsCompleted || !IsTypableCharacter(input))
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
