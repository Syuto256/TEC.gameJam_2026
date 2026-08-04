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

        [Tooltip("画面に表示するお題。漢字のままでよい。")]
        public string displayText;

        [Tooltip("お題の読み。ひらがなで書く（カタカナも可）。\n" +
                 "打てるローマ字は、ここから実行時にすべて自動で作られる。\n" +
                 "ローマ字を手で書く必要はない。shinbun でも sinbun でも shinnbun でも通る。")]
        public string reading;

        public TypingQuestion(int level, string displayText, string reading)
        {
            this.level = level;
            this.displayText = displayText;
            this.reading = reading;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(displayText) && !string.IsNullOrWhiteSpace(reading);
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
            new TypingQuestion(4, "情報整理", "じょうほうせいり")
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

        /// <summary>読みからローマ字を作れない問題を列挙する。</summary>
        /// <remarks>
        /// 打てない文字（漢字の書き忘れなど）が読みに混ざっていると、その問題は遊べない。
        /// 出題されて初めて気づくと原因が分かりにくいため、まとめて調べられるようにしている。
        /// </remarks>
        public IReadOnlyList<string> FindUnplayableQuestions()
        {
            var problems = new List<string>();
            foreach (var question in questions)
            {
                if (question == null || !question.IsValid)
                {
                    problems.Add((question == null ? "(空の行)" : question.displayText) + ": お題か読みが空です。");
                    continue;
                }

                string error;
                if (!RomanizationGenerator.IsSupported(question.reading, out error))
                {
                    problems.Add(question.displayText + " (" + question.reading + "): " + error);
                }
            }

            return problems;
        }
    }
}
