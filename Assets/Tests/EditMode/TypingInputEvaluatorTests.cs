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
