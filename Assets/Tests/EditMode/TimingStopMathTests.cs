using NUnit.Framework;
using Overwork.MiniGames.TimingStop;

public sealed class TimingStopMathTests
{
    [Test]
    public void PingPong01_GoesRightThenBack()
    {
        Assert.That(TimingStopMath.PingPong01(0f, 2f), Is.EqualTo(0f).Within(.0001f));
        Assert.That(TimingStopMath.PingPong01(.5f, 2f), Is.EqualTo(.5f).Within(.0001f));
        Assert.That(TimingStopMath.PingPong01(1f, 2f), Is.EqualTo(1f).Within(.0001f));
        Assert.That(TimingStopMath.PingPong01(1.5f, 2f), Is.EqualTo(.5f).Within(.0001f));
        Assert.That(TimingStopMath.PingPong01(2f, 2f), Is.EqualTo(0f).Within(.0001f));
    }

    [Test]
    public void PingPong01_StaysAtLeftWhenCycleIsZero()
    {
        Assert.That(TimingStopMath.PingPong01(3f, 0f), Is.EqualTo(0f));
    }

    // 端ちょうど（0.6）は浮動小数の丸めでどちらにも転ぶため、テストしない。
    // 画面上は 1 ドット未満の差であり、遊びに影響しない。
    [Test]
    public void IsHit_UsesHalfWidthAroundCentre()
    {
        Assert.That(TimingStopMath.IsHit(.5f, .5f, .2f), Is.True, "中央は当たり");
        Assert.That(TimingStopMath.IsHit(.59f, .5f, .2f), Is.True, "ゾーンの内側は当たり");
        Assert.That(TimingStopMath.IsHit(.41f, .5f, .2f), Is.True, "ゾーンの内側は当たり");
        Assert.That(TimingStopMath.IsHit(.61f, .5f, .2f), Is.False, "ゾーンの外は外れ");
        Assert.That(TimingStopMath.IsHit(.39f, .5f, .2f), Is.False, "ゾーンの外は外れ");
    }

    [Test]
    public void ClampZoneCenter_KeepsZoneInsideTheBar()
    {
        Assert.That(TimingStopMath.ClampZoneCenter(0f, .3f), Is.EqualTo(.15f).Within(.0001f));
        Assert.That(TimingStopMath.ClampZoneCenter(1f, .3f), Is.EqualTo(.85f).Within(.0001f));
        Assert.That(TimingStopMath.ClampZoneCenter(.5f, .3f), Is.EqualTo(.5f).Within(.0001f));
    }
}
