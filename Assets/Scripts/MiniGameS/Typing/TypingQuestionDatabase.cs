using System;
using System.Collections.Generic;
using UnityEngine;

namespace Overwork.MiniGames.Typing
{
    [Serializable]
    public sealed class TypingQuestion
    {
        [Tooltip("この問題を出す問題レベル（1〜4）。数字が大きいほど後半に出る。")]
        [Range(1, 4)] public int level;

        [Tooltip("画面に表示するお題。漢字のままでよい。英単語もそのまま書ける。")]
        public string displayText;

        [Tooltip("お題の読み。ひらがなで書く（カタカナも可）。\n" +
                 "打てるローマ字は、ここから実行時にすべて自動で作られる。\n" +
                 "ローマ字を手で書く必要はない。shinbun でも sinbun でも shinnbun でも通る。\n" +
                 "\n" +
                 "空にすると、お題の文字をそのまま打つ問題になる。英単語のお題はこれで作る。")]
        public string reading;

        [Tooltip("読みから作られる綴りに加えて、これも打てるようにする。\n" +
                 "例: お題「プル」読み「ぷる」に pull を足すと、puru でも pull でも通る。\n" +
                 "\n" +
                 "1 件目は画面のヒントにも出る。手で書くものなので、自動では増えない。")]
        public string[] uniqueInputs = Array.Empty<string>();

        /// <param name="uniqueInputs">読みから作られる綴りに足して打てるようにする綴り。</param>
        public TypingQuestion(int level, string displayText, string reading, params string[] uniqueInputs)
        {
            this.level = level;
            this.displayText = displayText;
            this.reading = reading;
            this.uniqueInputs = uniqueInputs ?? Array.Empty<string>();
        }

        /// <remarks>
        /// **読みは必須ではない。** 読みが空の行は「お題をそのまま打つ」英単語の問題として扱う。
        /// 打てるかどうかまではここでは見ない。<see cref="TypingCandidateBuilder"/> が判断し、
        /// <see cref="TypingQuestionDatabase.FindUnplayableQuestions"/> がまとめて報告する。
        /// </remarks>
        public bool IsValid => !string.IsNullOrWhiteSpace(displayText);
    }

    [CreateAssetMenu(fileName = "TypingQuestionDatabase", menuName = "Overwork/Mini Games/Typing Question Database")]
    public sealed class TypingQuestionDatabase : ScriptableObject
    {
        [Tooltip("タイピングミニゲームの問題一覧。\n" +
                 "出題は、そのときの問題レベルに一致する行からランダムに1件選ばれる。\n" +
                 "各レベルに最低1件は入れること。0件のレベルがあると、そのレベルでは失敗扱いになる。")]
        [SerializeField] private List<TypingQuestion> questions = new List<TypingQuestion>
        {
            new TypingQuestion(1, "新聞", "しんぶん"),
            new TypingQuestion(1, "学校", "がっこう"),
            new TypingQuestion(1, "電話", "でんわ"),
            new TypingQuestion(1, "会社", "かいしゃ"),
            new TypingQuestion(1, "音楽", "おんがく"),
            new TypingQuestion(1, "時間", "じかん"),
            new TypingQuestion(1, "仕事", "しごと"),
            new TypingQuestion(1, "机", "つくえ"),
            new TypingQuestion(2, "会議", "かいぎ"),
            new TypingQuestion(2, "締切", "しめきり"),
            new TypingQuestion(2, "休憩", "きゅうけい"),
            new TypingQuestion(2, "資料", "しりょう"),
            new TypingQuestion(2, "予定", "よてい"),
            new TypingQuestion(2, "作業", "さぎょう"),
            new TypingQuestion(2, "問題", "もんだい"),
            new TypingQuestion(2, "確認", "かくにん"),
            new TypingQuestion(3, "勤怠管理", "きんたいかんり"),
            new TypingQuestion(3, "業務報告", "ぎょうむほうこく"),
            new TypingQuestion(3, "進捗確認", "しんちょくかくにん"),
            new TypingQuestion(3, "最終確認", "さいしゅうかくにん"),
            new TypingQuestion(3, "情報共有", "じょうほうきょうゆう"),
            new TypingQuestion(3, "作業手順", "さぎょうてじゅん"),
            new TypingQuestion(3, "緊急連絡", "きんきゅうれんらく"),
            new TypingQuestion(3, "優先順位", "ゆうせんじゅんい"),
            new TypingQuestion(4, "仕様書確認", "しようしょかくにん"),
            new TypingQuestion(4, "業務連絡", "ぎょうむれんらく"),
            new TypingQuestion(4, "作業効率化", "さぎょうこうりつか"),
            new TypingQuestion(4, "進行管理", "しんこうかんり"),
            new TypingQuestion(4, "優先度調整", "ゆうせんどちょうせい"),
            new TypingQuestion(4, "問題解決", "もんだいかいけつ"),
            new TypingQuestion(4, "締切厳守", "しめきりげんしゅ"),
            new TypingQuestion(4, "情報整理", "じょうほうせいり"),

            // ここから下は、英単語のお題とユニーク入力の書き方の見本を兼ねている。
            // 出題の傾向として不要なら消してよい。仕組みはデータに依存しない。
            new TypingQuestion(1, "プル", "ぷる", "pull"),
            new TypingQuestion(1, "コピー", "こぴー", "copy"),
            new TypingQuestion(2, "セーブ", "せーぶ", "save"),
            new TypingQuestion(2, "merge", string.Empty),
            new TypingQuestion(3, "バックアップ", "ばっくあっぷ", "backup"),
            new TypingQuestion(3, "デバッグ", "でばっぐ", "debug"),
            new TypingQuestion(4, "リファクタリング", "りふぁくたりんぐ", "refactoring"),
            new TypingQuestion(4, "deadline", string.Empty)
        };

        public bool TryGetRandomQuestion(int level, out TypingQuestion question)
        {
            var matching = questions.FindAll(candidate => candidate != null && candidate.IsValid && candidate.level == Mathf.Clamp(level, 1, 4));
            if (matching.Count == 0)
            {
                question = null;
                return false;
            }

            question = matching[UnityEngine.Random.Range(0, matching.Count)];
            return true;
        }

        public int GetQuestionCount(int level)
        {
            return questions.FindAll(candidate => candidate != null && candidate.IsValid && candidate.level == Mathf.Clamp(level, 1, 4)).Count;
        }

        /// <summary>打てる綴りを作れない問題を列挙する。</summary>
        /// <remarks>
        /// 打てない文字（漢字の書き忘れ、ユニーク入力の打ち間違いなど）が混ざっていると、その問題は遊べない。
        /// 出題は抽選なので、遊んで気づくとは限らない。まとめて調べられるようにしている。
        /// </remarks>
        public IReadOnlyList<string> FindUnplayableQuestions()
        {
            var problems = new List<string>();
            foreach (var question in questions)
            {
                if (question == null || !question.IsValid)
                {
                    problems.Add((question == null ? "(空の行)" : question.displayText) + ": お題が空です。");
                    continue;
                }

                IReadOnlyList<string> unused;
                string error;
                if (!TypingCandidateBuilder.TryBuild(question, out unused, out error))
                {
                    problems.Add(question.displayText + " (" + question.reading + "): " + error);
                }
            }

            return problems;
        }
    }
}
