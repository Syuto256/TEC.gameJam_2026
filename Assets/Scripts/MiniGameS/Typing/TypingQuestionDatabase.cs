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

        [Tooltip("正解として受け付けるローマ字。複数書くと、どれで打っても正解になる。\n" +
                 "例:「新聞」なら shinbun と sinbun の両方。\n" +
                 "1つ目が画面に表示される目標として使われる。")]
        public List<string> acceptedRomanizations = new List<string>();

        public TypingQuestion(int level, string displayText, params string[] acceptedRomanizations)
        {
            this.level = level;
            this.displayText = displayText;
            this.acceptedRomanizations = new List<string>(acceptedRomanizations);
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(displayText) && acceptedRomanizations.Exists(value => !string.IsNullOrWhiteSpace(value));
    }

    [CreateAssetMenu(fileName = "TypingQuestionDatabase", menuName = "Overwork/Mini Games/Typing Question Database")]
    public sealed class TypingQuestionDatabase : ScriptableObject
    {
        [Tooltip("タイピングミニゲームの問題一覧。\n" +
                 "出題は、そのときの問題レベルに一致する行からランダムに1件選ばれる。\n" +
                 "各レベルに最低1件は入れること。0件のレベルがあると、そのレベルでは失敗扱いになる。")]
        [SerializeField] private List<TypingQuestion> questions = new List<TypingQuestion>
        {
            new TypingQuestion(1, "\u65B0\u805E", "shinbun", "sinbun"),
            new TypingQuestion(1, "\u5B66\u6821", "gakkou", "gakko"),
            new TypingQuestion(1, "\u96FB\u8A71", "denwa"),
            new TypingQuestion(1, "\u4F1A\u793E", "kaisha"),
            new TypingQuestion(1, "\u97F3\u697D", "ongaku"),
            new TypingQuestion(1, "\u6642\u9593", "jikan"),
            new TypingQuestion(1, "\u4ED5\u4E8B", "shigoto"),
            new TypingQuestion(1, "\u673A", "tsukue"),
            new TypingQuestion(2, "\u4F1A\u8B70", "kaigi"),
            new TypingQuestion(2, "\u7DE0\u5207", "shimekiri"),
            new TypingQuestion(2, "\u4F11\u61A9", "kyuukei", "kyukei"),
            new TypingQuestion(2, "\u8CC7\u6599", "shiryou", "siryou"),
            new TypingQuestion(2, "\u4E88\u5B9A", "yotei"),
            new TypingQuestion(2, "\u4F5C\u696D", "sagyou", "sagyo"),
            new TypingQuestion(2, "\u554F\u984C", "mondai"),
            new TypingQuestion(2, "\u78BA\u8A8D", "kakunin"),
            new TypingQuestion(3, "\u52E4\u6020\u7BA1\u7406", "kintaikanri"),
            new TypingQuestion(3, "\u696D\u52D9\u5831\u544A", "gyoumuhoukoku", "gyomuhokoku"),
            new TypingQuestion(3, "\u9032\u6357\u78BA\u8A8D", "shinchokukakunin"),
            new TypingQuestion(3, "\u6700\u7D42\u78BA\u8A8D", "saishuukakunin", "saishukakunin"),
            new TypingQuestion(3, "\u60C5\u5831\u5171\u6709", "jouhoukyouyuu", "johokyouyuu"),
            new TypingQuestion(3, "\u4F5C\u696D\u624B\u9806", "sagyoutejun", "sagyotejun"),
            new TypingQuestion(3, "\u7DCA\u6025\u9023\u7D61", "kinkyuurennraku", "kinkyurenraku"),
            new TypingQuestion(3, "\u512A\u5148\u9806\u4F4D", "yuusenjuni"),
            new TypingQuestion(4, "\u4ED5\u69D8\u66F8\u78BA\u8A8D", "shiyoushokakunin", "shiyoshokakunin"),
            new TypingQuestion(4, "\u696D\u52D9\u9023\u7D61", "gyoumurenraku", "gyomurenraku"),
            new TypingQuestion(4, "\u4F5C\u696D\u52B9\u7387\u5316", "sagyoukouritsuka", "sagyokouritsuka"),
            new TypingQuestion(4, "\u9032\u884C\u7BA1\u7406", "shinkoukanri", "sinkoukanri"),
            new TypingQuestion(4, "\u512A\u5148\u5EA6\u8ABF\u6574", "yuusendochousei", "yusendochosei"),
            new TypingQuestion(4, "\u554F\u984C\u89E3\u6C7A", "mondaikaiketsu"),
            new TypingQuestion(4, "\u7DE0\u5207\u53B3\u5B88", "shimekirigenshu"),
            new TypingQuestion(4, "\u60C5\u5831\u6574\u7406", "jouhouseiri", "johouseiri")
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
    }
}
