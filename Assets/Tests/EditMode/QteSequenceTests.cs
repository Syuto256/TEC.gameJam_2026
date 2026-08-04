using System;
using NUnit.Framework;
using Overwork.MiniGames.Qte;

public sealed class QteSequenceTests
{
    [Test]
    public void Press_AdvancesOnCorrectKey()
    {
        var sequence = new QteSequence(new[] { 0, 1 });
        Assert.That(sequence.Press(0), Is.EqualTo(QtePressResult.Correct));
        Assert.That(sequence.Progress, Is.EqualTo(1));
        Assert.That(sequence.CurrentKey, Is.EqualTo(1));
    }

    [Test]
    public void Press_DoesNotAdvanceOnWrongKey()
    {
        var sequence = new QteSequence(new[] { 0, 1 });
        Assert.That(sequence.Press(1), Is.EqualTo(QtePressResult.Wrong));
        Assert.That(sequence.Progress, Is.EqualTo(0));
    }

    [Test]
    public void Press_CompletesAfterWholeSequence()
    {
        var sequence = new QteSequence(new[] { 2, 0 });
        sequence.Press(2);
        sequence.Press(0);
        Assert.That(sequence.IsComplete, Is.True);
        Assert.That(sequence.CurrentKey, Is.EqualTo(-1));
        Assert.That(sequence.Press(2), Is.EqualTo(QtePressResult.Ignored));
    }

    [Test]
    public void Restart_ResetsProgressButKeepsKeys()
    {
        var sequence = new QteSequence(new[] { 1, 3 });
        sequence.Press(1);
        sequence.Restart();
        Assert.That(sequence.Progress, Is.EqualTo(0));
        Assert.That(sequence.KeyAt(1), Is.EqualTo(3));
    }

    [Test]
    public void Constructor_RejectsEmptySequence()
    {
        Assert.Throws<ArgumentException>(() => new QteSequence(new int[0]));
    }

    [Test]
    public void BuildRandomKeys_StaysInRangeAndAvoidsNeighbouringRepeats()
    {
        var keys = QteSequence.BuildRandomKeys(4, 40, new Random(1234));
        Assert.That(keys.Length, Is.EqualTo(40));
        for (var index = 0; index < keys.Length; index++)
        {
            Assert.That(keys[index], Is.InRange(0, 3));
            if (index > 0)
            {
                Assert.That(keys[index], Is.Not.EqualTo(keys[index - 1]), "同じキーが隣り合っている: " + index);
            }
        }
    }

    [Test]
    public void BuildRandomKeys_AllowsRepeatsWhenOnlyOneKeyExists()
    {
        var keys = QteSequence.BuildRandomKeys(1, 3, new Random(1));
        Assert.That(keys, Is.EqualTo(new[] { 0, 0, 0 }));
    }
}
