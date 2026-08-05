using System.Collections.Generic;
using NUnit.Framework;
using Overwork.MiniGames.Typing;

/// <summary>英単語のお題とユニーク入力が、実際に打てる綴りになるかを見る。</summary>
public sealed class TypingCandidateBuilderTests
{
    private static IReadOnlyList<string> Build(TypingQuestion question)
    {
        IReadOnlyList<string> candidates;
        string error;
        Assert.That(TypingCandidateBuilder.TryBuild(question, out candidates, out error), Is.True, error);
        return candidates;
    }

    /// <summary>判定器に 1 文字ずつ流して、最後まで通るかを見る。実際の遊びと同じ経路。</summary>
    private static bool CanType(TypingQuestion question, string input)
    {
        var evaluator = new TypingInputEvaluator(Build(question));
        foreach (var character in input)
        {
            if (!evaluator.TryInput(character)) return false;
            if (evaluator.IsCompleted) return true;
        }

        return evaluator.IsCompleted;
    }

    [Test]
    public void EnglishQuestion_TypesTheDisplayTextItself()
    {
        var question = new TypingQuestion(1, "merge", string.Empty);

        Assert.That(Build(question)[0], Is.EqualTo("merge"));
        Assert.That(CanType(question, "merge"), Is.True);
    }

    [Test]
    public void EnglishQuestion_IgnoresLetterCase()
    {
        // 表示は大文字始まりでも、打つのは小文字でよい。
        Assert.That(CanType(new TypingQuestion(1, "Pull", string.Empty), "pull"), Is.True);
    }

    [Test]
    public void UniqueInput_IsAcceptedAlongsideTheGeneratedRomanization()
    {
        var question = new TypingQuestion(1, "プル", "ぷる", "pull");

        Assert.That(CanType(question, "pull"), Is.True, "ユニーク入力が通らない。");
        Assert.That(CanType(question, "puru"), Is.True, "読みから作った綴りが通らなくなっている。");
    }

    [Test]
    public void UniqueInput_ComesFirstSoItBecomesTheHint()
    {
        // 先頭が画面の「ローマ字」欄に出る。手で足した綴りを見せる決まりである。
        Assert.That(Build(new TypingQuestion(1, "プル", "ぷる", "pull"))[0], Is.EqualTo("pull"));

        // ユニーク入力が無ければ、これまでどおり読みからの代表が先頭に来る。
        Assert.That(Build(new TypingQuestion(1, "新聞", "しんぶん"))[0], Is.EqualTo("shinbun"));
    }

    [Test]
    public void UniqueInput_SharesThePrefixWithTheGeneratedSpelling()
    {
        // puru と pull は pu まで共通。途中まで打ってからどちらへも分岐できること。
        var evaluator = new TypingInputEvaluator(Build(new TypingQuestion(1, "プル", "ぷる", "pull")));
        Assert.That(evaluator.TryInput('p'), Is.True);
        Assert.That(evaluator.TryInput('u'), Is.True);
        Assert.That(evaluator.TryInput('l'), Is.True, "分岐が絞り込まれて pull へ進めない。");
        Assert.That(evaluator.TryInput('l'), Is.True);
        Assert.That(evaluator.IsCompleted, Is.True);
    }

    [Test]
    public void LongVowelReading_CanBeTypedWithHyphen()
    {
        // 「ー」からは "-" しか作られない。英字だけを入力として扱っていたころ、
        // ここでミスが数えられて長音符を含む問題は必ず失敗していた。
        Assert.That(CanType(new TypingQuestion(1, "コーヒー", "こーひー"), "ko-hi-"), Is.True);
    }

    [Test]
    public void UntypableUniqueInput_IsReportedInsteadOfBeingDropped()
    {
        IReadOnlyList<string> candidates;
        string error;
        var question = new TypingQuestion(1, "プル", "ぷる", "プル");

        Assert.That(TypingCandidateBuilder.TryBuild(question, out candidates, out error), Is.False);
        Assert.That(error, Does.Contain("ユニーク入力"));
    }

    [Test]
    public void MissingReadingOnAJapaneseQuestion_IsReported()
    {
        // 読みの書き忘れ。お題をそのまま打つ扱いになるが、漢字は打てないので点検で捕まる。
        IReadOnlyList<string> candidates;
        string error;

        Assert.That(TypingCandidateBuilder.TryBuild(new TypingQuestion(1, "新聞", string.Empty), out candidates, out error), Is.False);
        Assert.That(error, Is.Not.Empty);
        Assert.That(candidates, Is.Empty);
    }

    [Test]
    public void UniqueInput_RescuesAQuestionWhoseDisplayTextCannotBeTyped()
    {
        // 読みが無く、お題も打てない文字。ユニーク入力があるなら成立させる。
        var question = new TypingQuestion(1, "★", string.Empty, "star");

        Assert.That(Build(question)[0], Is.EqualTo("star"));
        Assert.That(CanType(question, "star"), Is.True);
    }
}
