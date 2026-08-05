using System.Collections.Generic;
using NUnit.Framework;
using Overwork.MiniGames.Typing;
using UnityEngine;

public sealed class TypingInputEvaluatorTests
{
    [Test]
    public void AlternativeRomanization_CompletesWithValidAlternative()
    {
        var evaluator = new TypingInputEvaluator(new List<string> { "shinbun", "sinbun" });

        foreach (var input in "sinbun")
        {
            Assert.That(evaluator.TryInput(input), Is.True);
        }

        Assert.That(evaluator.IsCompleted, Is.True);
        Assert.That(evaluator.AcceptedInput, Is.EqualTo("sinbun"));
    }

    [Test]
    public void InvalidInput_DoesNotAdvanceTheAcceptedPrefix()
    {
        var evaluator = new TypingInputEvaluator(new List<string> { "denwa" });

        Assert.That(evaluator.TryInput('x'), Is.False);
        Assert.That(evaluator.AcceptedInput, Is.Empty);
        Assert.That(evaluator.TryInput('d'), Is.True);
        Assert.That(evaluator.AcceptedInput, Is.EqualTo("d"));
    }

    [Test]
    public void TypableCharacters_CoverEverythingASpellingCanContain()
    {
        // 綴りに出てくるのは英字だけではない。「ー」からは -、「ん」からは n'、英熟語には空白が要る。
        Assert.That(TypingInputEvaluator.IsTypableCharacter('a'), Is.True);
        Assert.That(TypingInputEvaluator.IsTypableCharacter('-'), Is.True);
        Assert.That(TypingInputEvaluator.IsTypableCharacter('\''), Is.True);
        Assert.That(TypingInputEvaluator.IsTypableCharacter(' '), Is.True);

        // 日本語入力が有効なまま打つと飛んでくる文字。ミスにせず捨てるため、ここで落とす。
        Assert.That(TypingInputEvaluator.IsTypableCharacter('あ'), Is.False);
        Assert.That(TypingInputEvaluator.IsTypableCharacter('新'), Is.False);
        Assert.That(TypingInputEvaluator.IsTypableCharacter('\b'), Is.False);
    }

    [Test]
    public void NonLetterCharacterInACandidateCanBeTyped()
    {
        var evaluator = new TypingInputEvaluator(new List<string> { "ko-hi-" });

        foreach (var input in "ko-hi-")
        {
            Assert.That(evaluator.TryInput(input), Is.True, "「" + input + "」で止まった。");
        }

        Assert.That(evaluator.IsCompleted, Is.True);
    }

    [Test]
    public void DefaultDatabase_ProvidesEightQuestionsForEachLevel()
    {
        var database = ScriptableObject.CreateInstance<TypingQuestionDatabase>();
        try
        {
            for (var level = 1; level <= 4; level++)
            {
                Assert.That(database.GetQuestionCount(level), Is.GreaterThanOrEqualTo(8));
            }
        }
        finally
        {
            Object.DestroyImmediate(database);
        }
    }
}
