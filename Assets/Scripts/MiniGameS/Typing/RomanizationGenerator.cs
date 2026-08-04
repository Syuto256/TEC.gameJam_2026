using System;
using System.Collections.Generic;

namespace Overwork.MiniGames.Typing
{
    /// <summary>ひらがなの読みから、打てるローマ字の綴りをすべて作る。</summary>
    /// <remarks>
    /// <para>
    /// 問題データが持つのは読みだけである。訓令式・ヘボン式の違い、促音や撥音の打ち方の癖は、
    /// ここが実行時にまとめて面倒を見る。**問題を足す人はローマ字を書かなくてよい。**
    /// </para>
    /// <para>
    /// 手で候補を並べていたころは「しんぶん」に <c>shinbun</c> と <c>sinbun</c> だけを書いていたため、
    /// 「ん」を <c>nn</c> と打つ人が語中の「ん」で必ず詰まっていた。書き漏らしが避けられないので、
    /// 綴りを人が持つのをやめてここに寄せた。
    /// </para>
    /// <para>
    /// この表は Suzuki の試作（<c>Assets/Personal/Suzuki/TypingMiniGame/</c>）から取り込んでいる。
    /// </para>
    /// </remarks>
    public static class RomanizationGenerator
    {
        private static readonly Dictionary<string, string[]> KanaMap = new Dictionary<string, string[]>
        {
            { "あ", new[] { "a" } }, { "い", new[] { "i", "yi" } }, { "う", new[] { "u", "wu" } }, { "え", new[] { "e" } }, { "お", new[] { "o" } },
            { "か", new[] { "ka", "ca" } }, { "き", new[] { "ki" } }, { "く", new[] { "ku", "cu", "qu" } }, { "け", new[] { "ke" } }, { "こ", new[] { "ko", "co" } },
            { "さ", new[] { "sa" } }, { "し", new[] { "shi", "si", "ci", "syi" } }, { "す", new[] { "su" } }, { "せ", new[] { "se", "ce" } }, { "そ", new[] { "so" } },
            { "た", new[] { "ta" } }, { "ち", new[] { "chi", "ti" } }, { "つ", new[] { "tsu", "tu" } }, { "て", new[] { "te" } }, { "と", new[] { "to" } },
            { "な", new[] { "na" } }, { "に", new[] { "ni" } }, { "ぬ", new[] { "nu" } }, { "ね", new[] { "ne" } }, { "の", new[] { "no" } },
            { "は", new[] { "ha" } }, { "ひ", new[] { "hi" } }, { "ふ", new[] { "fu", "hu" } }, { "へ", new[] { "he" } }, { "ほ", new[] { "ho" } },
            { "ま", new[] { "ma" } }, { "み", new[] { "mi" } }, { "む", new[] { "mu" } }, { "め", new[] { "me" } }, { "も", new[] { "mo" } },
            { "や", new[] { "ya" } }, { "ゆ", new[] { "yu" } }, { "よ", new[] { "yo" } },
            { "ら", new[] { "ra" } }, { "り", new[] { "ri" } }, { "る", new[] { "ru" } }, { "れ", new[] { "re" } }, { "ろ", new[] { "ro" } },
            { "わ", new[] { "wa" } }, { "ゐ", new[] { "wi" } }, { "ゑ", new[] { "we" } }, { "を", new[] { "wo", "o" } },
            { "が", new[] { "ga" } }, { "ぎ", new[] { "gi" } }, { "ぐ", new[] { "gu" } }, { "げ", new[] { "ge" } }, { "ご", new[] { "go" } },
            { "ざ", new[] { "za" } }, { "じ", new[] { "ji", "zi" } }, { "ず", new[] { "zu" } }, { "ぜ", new[] { "ze" } }, { "ぞ", new[] { "zo" } },
            { "だ", new[] { "da" } }, { "ぢ", new[] { "di", "ji", "zi" } }, { "づ", new[] { "du", "zu" } }, { "で", new[] { "de" } }, { "ど", new[] { "do" } },
            { "ば", new[] { "ba" } }, { "び", new[] { "bi" } }, { "ぶ", new[] { "bu" } }, { "べ", new[] { "be" } }, { "ぼ", new[] { "bo" } },
            { "ぱ", new[] { "pa" } }, { "ぴ", new[] { "pi" } }, { "ぷ", new[] { "pu" } }, { "ぺ", new[] { "pe" } }, { "ぽ", new[] { "po" } },
            { "ゔ", new[] { "vu" } },

            { "きゃ", new[] { "kya", "kilya", "kixya" } }, { "きゅ", new[] { "kyu", "kilyu", "kixyu" } }, { "きょ", new[] { "kyo", "kilyo", "kixyo" } },
            { "ぎゃ", new[] { "gya", "gilya", "gixya" } }, { "ぎゅ", new[] { "gyu", "gilyu", "gixyu" } }, { "ぎょ", new[] { "gyo", "gilyo", "gixyo" } },
            { "しゃ", new[] { "sha", "sya", "shilya", "shixya" } }, { "しゅ", new[] { "shu", "syu", "shilyu", "shixyu" } }, { "しょ", new[] { "sho", "syo", "shilyo", "shixyo" } },
            { "じゃ", new[] { "ja", "jya", "zya", "jilya", "jixya" } }, { "じゅ", new[] { "ju", "jyu", "zyu", "jilyu", "jixyu" } }, { "じょ", new[] { "jo", "jyo", "zyo", "jilyo", "jixyo" } },
            { "ちゃ", new[] { "cha", "tya", "cya", "chilya", "chixya" } }, { "ちゅ", new[] { "chu", "tyu", "cyu", "chilyu", "chixyu" } }, { "ちょ", new[] { "cho", "tyo", "cyo", "chilyo", "chixyo" } },
            { "ぢゃ", new[] { "dya", "ja", "jya", "zya" } }, { "ぢゅ", new[] { "dyu", "ju", "jyu", "zyu" } }, { "ぢょ", new[] { "dyo", "jo", "jyo", "zyo" } },
            { "にゃ", new[] { "nya", "nilya", "nixya" } }, { "にゅ", new[] { "nyu", "nilyu", "nixyu" } }, { "にょ", new[] { "nyo", "nilyo", "nixyo" } },
            { "ひゃ", new[] { "hya", "hilya", "hixya" } }, { "ひゅ", new[] { "hyu", "hilyu", "hixyu" } }, { "ひょ", new[] { "hyo", "hilyo", "hixyo" } },
            { "びゃ", new[] { "bya", "bilya", "bixya" } }, { "びゅ", new[] { "byu", "bilyu", "bixyu" } }, { "びょ", new[] { "byo", "bilyo", "bixyo" } },
            { "ぴゃ", new[] { "pya", "pilya", "pixya" } }, { "ぴゅ", new[] { "pyu", "pilyu", "pixyu" } }, { "ぴょ", new[] { "pyo", "pilyo", "pixyo" } },
            { "みゃ", new[] { "mya", "milya", "mixya" } }, { "みゅ", new[] { "myu", "milyu", "mixyu" } }, { "みょ", new[] { "myo", "milyo", "mixyo" } },
            { "りゃ", new[] { "rya", "rilya", "rixya" } }, { "りゅ", new[] { "ryu", "rilyu", "rixyu" } }, { "りょ", new[] { "ryo", "rilyo", "rixyo" } },
            { "ゔゃ", new[] { "vya", "vilya", "vixya" } }, { "ゔゅ", new[] { "vyu", "vilyu", "vixyu" } }, { "ゔょ", new[] { "vyo", "vilyo", "vixyo" } },

            { "ふぁ", new[] { "fa", "fwa", "fula", "fuxa" } }, { "ふぃ", new[] { "fi", "fyi", "fuli", "fuxi" } }, { "ふぇ", new[] { "fe", "fye", "fule", "fuxe" } }, { "ふぉ", new[] { "fo", "fwo", "fulo", "fuxo" } },
            { "てぃ", new[] { "ti", "thi", "teli", "texi" } }, { "でぃ", new[] { "di", "dhi", "deli", "dexi" } }, { "とぅ", new[] { "tu", "twu", "tolu", "toxu" } }, { "どぅ", new[] { "du", "dwu", "dolu", "doxu" } },
            { "しぇ", new[] { "she", "sye", "shile", "shixe" } }, { "じぇ", new[] { "je", "zye", "jile", "jixe" } }, { "ちぇ", new[] { "che", "cye", "tye", "chile", "chixe" } },
            { "つぁ", new[] { "tsa", "tula", "tuxa" } }, { "つぃ", new[] { "tsi", "tuli", "tuxi" } }, { "つぇ", new[] { "tse", "tule", "tuxe" } }, { "つぉ", new[] { "tso", "tulo", "tuxo" } },
            { "うぃ", new[] { "wi", "uli", "uxi" } }, { "うぇ", new[] { "we", "ule", "uxe" } }, { "うぉ", new[] { "who", "ulo", "uxo" } },
            { "くぁ", new[] { "kwa", "qwa", "kula", "kuxa" } }, { "くぃ", new[] { "kwi", "qwi", "kuli", "kuxi" } }, { "くぇ", new[] { "kwe", "qwe", "kule", "kuxe" } }, { "くぉ", new[] { "kwo", "qwo", "kulo", "kuxo" } },

            { "ぁ", new[] { "xa", "la" } }, { "ぃ", new[] { "xi", "li" } }, { "ぅ", new[] { "xu", "lu" } }, { "ぇ", new[] { "xe", "le" } }, { "ぉ", new[] { "xo", "lo" } },
            { "ゃ", new[] { "xya", "lya" } }, { "ゅ", new[] { "xyu", "lyu" } }, { "ょ", new[] { "xyo", "lyo" } }, { "ゎ", new[] { "xwa", "lwa" } },
            { "ー", new[] { "-" } },
        };

        /// <summary>読みから打てる綴りをすべて作る。</summary>
        /// <param name="reading">ひらがな（カタカナも受け付ける）の読み。</param>
        /// <param name="candidates">先頭が代表の綴り。画面に「ローマ字」として出るのはこれである。</param>
        /// <param name="error">作れなかった理由。作れた場合は空。</param>
        /// <remarks>
        /// 例外を投げないのは、問題データの打ち間違いでゲームが止まらないようにするためである。
        /// 呼び出し側は false のときに理由を添えて失敗終了させる。
        /// </remarks>
        public static bool TryGenerate(string reading, out IReadOnlyList<string> candidates, out string error)
        {
            candidates = Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(reading))
            {
                error = "読みが空です。";
                return false;
            }

            var normalized = NormalizeReading(reading);
            var unsupported = FindUnsupported(normalized);
            if (unsupported != null)
            {
                error = "読みに打てない文字が含まれています -> " + unsupported;
                return false;
            }

            var all = BuildCandidates(normalized, 0, new Dictionary<int, HashSet<string>>());
            var canonical = BuildCanonical(normalized, 0);

            // 代表を先頭に置く。TypingInputEvaluator は先頭を画面表示に使うため、
            // ここが辞書順の先頭（cinbun のような綴り）になると読めない表示になる。
            var ordered = new List<string>(all.Count) { canonical };
            var rest = new List<string>(all);
            rest.Sort(StringComparer.Ordinal);
            foreach (var candidate in rest)
            {
                if (!string.Equals(candidate, canonical, StringComparison.Ordinal))
                {
                    ordered.Add(candidate);
                }
            }

            candidates = ordered;
            error = string.Empty;
            return true;
        }

        /// <summary>読みが打てる文字だけでできているかを調べる。問題データの点検に使う。</summary>
        public static bool IsSupported(string reading, out string error)
        {
            IReadOnlyList<string> unused;
            return TryGenerate(reading, out unused, out error);
        }

        /// <summary>対応していない最初の文字を返す。すべて対応していれば null。</summary>
        private static string FindUnsupported(string reading)
        {
            for (var index = 0; index < reading.Length; index++)
            {
                var current = reading[index];
                if (current == 'ん' || current == 'っ')
                {
                    continue;
                }

                if (index + 1 < reading.Length && KanaMap.ContainsKey(reading.Substring(index, 2)))
                {
                    index++;
                    continue;
                }

                if (!KanaMap.ContainsKey(current.ToString()))
                {
                    return current.ToString();
                }
            }

            return null;
        }

        private static HashSet<string> BuildCandidates(string reading, int index, Dictionary<int, HashSet<string>> memo)
        {
            if (index >= reading.Length)
            {
                return new HashSet<string>(StringComparer.Ordinal) { string.Empty };
            }

            HashSet<string> cached;
            if (memo.TryGetValue(index, out cached))
            {
                return cached;
            }

            var result = new HashSet<string>(StringComparer.Ordinal);
            var current = reading[index];

            if (current == 'ん')
            {
                // 次が母音・や行・んのときは n 1 つでは次の字と混ざるため、nn などに限る。
                var prefixes = RequiresDoubleN(reading, index + 1)
                    ? new[] { "nn", "xn", "n'" }
                    : new[] { "n", "nn", "xn" };
                AddWithPrefixes(result, prefixes, BuildCandidates(reading, index + 1, memo));
            }
            else if (current == 'っ')
            {
                var tails = BuildCandidates(reading, index + 1, memo);
                AddWithPrefixes(result, new[] { "xtu", "ltu" }, tails);
                foreach (var tail in tails)
                {
                    var doubled = CreateSokuonPrefix(tail);
                    if (!string.IsNullOrEmpty(doubled))
                    {
                        result.Add(doubled);
                    }
                }
            }
            else
            {
                var token = FindToken(reading, index);
                var suffixes = BuildCandidates(reading, index + token.Length, memo);
                AddWithPrefixes(result, KanaMap[token], suffixes);
            }

            memo[index] = result;
            return result;
        }

        private static string BuildCanonical(string reading, int index)
        {
            if (index >= reading.Length)
            {
                return string.Empty;
            }

            if (reading[index] == 'ん')
            {
                var prefix = RequiresDoubleN(reading, index + 1) ? "nn" : "n";
                return prefix + BuildCanonical(reading, index + 1);
            }

            if (reading[index] == 'っ')
            {
                var tail = BuildCanonical(reading, index + 1);
                var prefix = CreateSokuonPrefix(tail);
                return !string.IsNullOrEmpty(prefix) ? prefix : "xtu" + tail;
            }

            var token = FindToken(reading, index);
            return KanaMap[token][0] + BuildCanonical(reading, index + token.Length);
        }

        private static void AddWithPrefixes(HashSet<string> target, IEnumerable<string> prefixes, IEnumerable<string> suffixes)
        {
            foreach (var prefix in prefixes)
            {
                foreach (var suffix in suffixes)
                {
                    target.Add(prefix + suffix);
                }
            }
        }

        /// <summary>促音を「次の子音を重ねる」形にする。「ちゃ」系だけは ch ではなく t を重ねる。</summary>
        private static string CreateSokuonPrefix(string tail)
        {
            if (string.IsNullOrEmpty(tail) || !IsConsonant(tail[0]))
            {
                return string.Empty;
            }

            if (tail.StartsWith("ch", StringComparison.Ordinal))
            {
                return "t" + tail;
            }

            return tail[0] + tail;
        }

        private static bool RequiresDoubleN(string reading, int nextIndex)
        {
            if (nextIndex >= reading.Length)
            {
                return false;
            }

            var next = reading[nextIndex];
            return next == 'あ' || next == 'い' || next == 'う' || next == 'え' || next == 'お' ||
                   next == 'や' || next == 'ゆ' || next == 'よ' || next == 'ゃ' || next == 'ゅ' || next == 'ょ' || next == 'ん';
        }

        private static string FindToken(string reading, int index)
        {
            if (index + 1 < reading.Length)
            {
                var pair = reading.Substring(index, 2);
                if (KanaMap.ContainsKey(pair))
                {
                    return pair;
                }
            }

            return reading[index].ToString();
        }

        private static bool IsConsonant(char value)
        {
            return value >= 'a' && value <= 'z' && value != 'a' && value != 'i' && value != 'u' && value != 'e' && value != 'o' && value != 'n';
        }

        /// <summary>カタカナをひらがなに寄せ、前後の空白を落とす。</summary>
        private static string NormalizeReading(string reading)
        {
            var chars = reading.Trim().ToCharArray();
            for (var index = 0; index < chars.Length; index++)
            {
                if (chars[index] >= 'ァ' && chars[index] <= 'ヶ')
                {
                    chars[index] = (char)(chars[index] - 'ァ' + 'ぁ');
                }
            }

            return new string(chars);
        }
    }
}
