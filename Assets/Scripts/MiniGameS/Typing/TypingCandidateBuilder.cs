using System;
using System.Collections.Generic;

namespace Overwork.MiniGames.Typing
{
    /// <summary>1 問から、打てる綴りをすべて集める。</summary>
    /// <remarks>
    /// <para>
    /// 綴りの出どころは 3 つある。**読みからの自動生成（<see cref="RomanizationGenerator"/>）、
    /// 手で書いたユニーク入力、お題そのもの** である。どれをどう混ぜるかの決まりが
    /// ミニゲーム側と問題データの点検側に分かれていると必ずずれるため、ここ 1 箇所に集めている。
    /// </para>
    /// <para>
    /// 並び順には意味がある。**先頭が画面のヒントになる。**
    /// <see cref="TypingInputEvaluator"/> は受け取った先頭を代表として扱うためである。
    /// </para>
    /// </remarks>
    public static class TypingCandidateBuilder
    {
        /// <summary>問題から打てる綴りを作る。</summary>
        /// <param name="candidates">先頭が代表。画面に「ローマ字」として出るのはこれである。</param>
        /// <param name="error">作れなかった理由。作れた場合は空。</param>
        /// <remarks>
        /// 例外を投げないのは、問題データの書き間違いでゲームが止まらないようにするためである。
        /// 呼び出し側は false のときに理由を添えて失敗終了させるか、点検結果として並べる。
        /// </remarks>
        public static bool TryBuild(TypingQuestion question, out IReadOnlyList<string> candidates, out string error)
        {
            candidates = Array.Empty<string>();

            if (question == null || string.IsNullOrWhiteSpace(question.displayText))
            {
                error = "お題が空です。";
                return false;
            }

            var ordered = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. 手で書いた綴りを先に入れる。**先頭がヒントになる**ため、
            //    ユニーク入力を持つ問題では読みから作った綴りより手書きが優先される。
            if (question.uniqueInputs != null)
            {
                foreach (var unique in question.uniqueInputs)
                {
                    if (string.IsNullOrWhiteSpace(unique))
                    {
                        continue;
                    }

                    var trimmed = unique.Trim();
                    string reason;
                    if (!IsTypable(trimmed, out reason))
                    {
                        // 手で書いたものの間違いは黙って捨てない。書いた本人に気づいてもらう。
                        error = "ユニーク入力「" + trimmed + "」が打てません -> " + reason;
                        return false;
                    }

                    if (seen.Add(trimmed))
                    {
                        ordered.Add(trimmed);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(question.reading))
            {
                // 2. 読みがあるなら、そこから作った綴りを足す。
                IReadOnlyList<string> generated;
                string generateError;
                if (!RomanizationGenerator.TryGenerate(question.reading, out generated, out generateError))
                {
                    error = generateError;
                    return false;
                }

                foreach (var candidate in generated)
                {
                    if (seen.Add(candidate))
                    {
                        ordered.Add(candidate);
                    }
                }
            }
            else
            {
                // 3. 読みが無いなら、お題そのものを打つ問題として扱う。**英単語のお題はこの形になる。**
                //    ユニーク入力を書いてあれば、お題が打てない文字でも成立するのでここは咎めない。
                var direct = question.displayText.Trim();
                string reason;
                if (IsTypable(direct, out reason))
                {
                    if (seen.Add(direct))
                    {
                        ordered.Add(direct);
                    }
                }
                else if (ordered.Count == 0)
                {
                    error = "読みが空なので、お題「" + direct + "」をそのまま打つ問題として扱いました。"
                            + "しかし " + reason + " 読みを書くか、ユニーク入力に打てる綴りを足してください。";
                    return false;
                }
            }

            if (ordered.Count == 0)
            {
                error = "打てる綴りが 1 つもありません。";
                return false;
            }

            candidates = ordered;
            error = string.Empty;
            return true;
        }

        /// <summary>その綴りをキーボードで打ち切れるか。問題データの点検に使う。</summary>
        public static bool IsTypable(string value, out string reason)
        {
            if (string.IsNullOrEmpty(value))
            {
                reason = "空です。";
                return false;
            }

            foreach (var character in value)
            {
                if (!TypingInputEvaluator.IsTypableCharacter(character))
                {
                    reason = "「" + character + "」は打てません。";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
