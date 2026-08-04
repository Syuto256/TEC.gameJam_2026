using System.Collections.Generic;
using NUnit.Framework;
using Overwork.MiniGames.Typing;

public sealed class RomanizationGeneratorTests
{
    private static IReadOnlyList<string> Generate(string reading)
    {
        IReadOnlyList<string> candidates;
        string error;
        Assert.That(RomanizationGenerator.TryGenerate(reading, out candidates, out error), Is.True, error);
        return candidates;
    }

    /// <summary>判定器に 1 文字ずつ流して、最後まで通るかを見る。実際の遊びと同じ経路。</summary>
    private static bool CanType(string reading, string input)
    {
        var evaluator = new TypingInputEvaluator(Generate(reading));
        foreach (var character in input)
        {
            if (!evaluator.TryInput(character)) return false;
            if (evaluator.IsCompleted) return true;
        }

        return evaluator.IsCompleted;
    }

    [Test]
    public void CanonicalComesFirst()
    {
        // TypingInputEvaluator は先頭を画面表示に使うため、辞書順の先頭ではなく代表が来ること。
        Assert.That(Generate("しんぶん")[0], Is.EqualTo("shinbun"));
        Assert.That(Generate("がっこう")[0], Is.EqualTo("gakkou"));
        Assert.That(Generate("かいしゃ")[0], Is.EqualTo("kaisha"));
    }

    [Test]
    public void HepburnAndKunreiAreBothAccepted()
    {
        Assert.That(CanType("しごと", "shigoto"), Is.True);
        Assert.That(CanType("しごと", "sigoto"), Is.True);
        Assert.That(CanType("つくえ", "tsukue"), Is.True);
        Assert.That(CanType("つくえ", "tukue"), Is.True);
        Assert.That(CanType("じかん", "jikan"), Is.True);
        Assert.That(CanType("じかん", "zikan"), Is.True);
        Assert.That(CanType("かいしゃ", "kaisha"), Is.True);
        Assert.That(CanType("かいしゃ", "kaisya"), Is.True);
    }

    [Test]
    public void SyllabicNCanBeTypedAsSingleOrDoubleN()
    {
        // 手書きの候補だった時代に、語中の「ん」を nn と打つ人が必ず詰まっていた箇所。
        Assert.That(CanType("しんぶん", "shinbun"), Is.True);
        Assert.That(CanType("しんぶん", "shinnbun"), Is.True);
        Assert.That(CanType("しんぶん", "sinnbun"), Is.True);
        Assert.That(CanType("きんたいかんり", "kintaikanri"), Is.True);
        Assert.That(CanType("きんたいかんり", "kinntaikanri"), Is.True);
    }

    [Test]
    public void SyllabicNBeforeVowelRequiresDoubleN()
    {
        // 「じゅんい」を juni と打つと「じゅに」になってしまうため、n 1 つは許さない。
        Assert.That(Generate("ゆうせんじゅんい")[0], Is.EqualTo("yuusenjunni"));
        Assert.That(CanType("ゆうせんじゅんい", "yuusenjunni"), Is.True);
        Assert.That(CanType("ゆうせんじゅんい", "yuusenjuni"), Is.False);
    }

    [Test]
    public void SokuonDoublesTheNextConsonant()
    {
        Assert.That(CanType("がっこう", "gakkou"), Is.True);
        Assert.That(CanType("がっこう", "gaxtukou"), Is.True);
        Assert.That(CanType("がっこう", "galtukou"), Is.True);
    }

    [Test]
    public void SokuonBeforeChiDoublesTWithoutDoublingC()
    {
        Assert.That(CanType("いっち", "itchi"), Is.True);
        Assert.That(CanType("いっち", "itti"), Is.True);
        Assert.That(CanType("いっち", "icchi"), Is.False);
    }

    [Test]
    public void LongVowelMustBeTypedOut()
    {
        // 移行前の手書きデータには gakko や sagyo が混ざっていて、単語が途中で完成していた。
        Assert.That(CanType("がっこう", "gakko"), Is.False);
        Assert.That(CanType("さぎょう", "sagyou"), Is.True);
        Assert.That(CanType("さぎょう", "sagyo"), Is.False);
    }

    [Test]
    public void KatakanaReadingIsAccepted()
    {
        Assert.That(Generate("シンブン")[0], Is.EqualTo("shinbun"));
    }

    [Test]
    public void UnsupportedCharacterIsReportedInsteadOfThrowing()
    {
        IReadOnlyList<string> candidates;
        string error;

        Assert.That(RomanizationGenerator.TryGenerate("しん聞", out candidates, out error), Is.False);
        Assert.That(error, Does.Contain("聞"));
        Assert.That(candidates, Is.Empty);

        Assert.That(RomanizationGenerator.TryGenerate("  ", out candidates, out error), Is.False);
        Assert.That(error, Is.Not.Empty);
    }
}
