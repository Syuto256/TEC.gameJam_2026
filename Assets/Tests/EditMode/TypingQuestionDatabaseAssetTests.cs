using NUnit.Framework;
using Overwork.MiniGames.Typing;
using UnityEditor;

/// <summary>実際に出題される問題資産そのものを点検する。</summary>
/// <remarks>
/// 読みに打てない文字（漢字の消し忘れなど）が混ざっていると、その問題を引いたときだけ失敗する。
/// 出題は抽選なので、遊んで気づくとは限らない。ここで全件を機械的に確かめる。
/// </remarks>
public sealed class TypingQuestionDatabaseAssetTests
{
    private const string AssetPath = "Assets/Data/MiniGames/Typing/TypingQuestionDatabase.asset";

    private static TypingQuestionDatabase Load()
    {
        var database = AssetDatabase.LoadAssetAtPath<TypingQuestionDatabase>(AssetPath);
        Assert.That(database, Is.Not.Null, AssetPath + " が見つかりません。");
        return database;
    }

    [Test]
    public void EveryQuestionCanProduceRomanization()
    {
        var problems = Load().FindUnplayableQuestions();
        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    [Test]
    public void EveryLevelHasQuestions()
    {
        var database = Load();
        for (var level = 1; level <= 4; level++)
        {
            Assert.That(database.GetQuestionCount(level), Is.GreaterThanOrEqualTo(1), "レベル " + level + " の問題が 0 件です。");
        }
    }
}
