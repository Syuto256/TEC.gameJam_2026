using NUnit.Framework;
using Overwork.MiniGames.Tracing;
using UnityEngine;

public sealed class TracingPathMathTests
{
    [Test]
    public void DistanceToPolyline_IsZeroOnSegment()
    {
        var points = new[] { Vector2.zero, Vector2.right };
        Assert.That(TracingPathMath.DistanceToPolyline(new Vector2(.5f, 0f), points), Is.EqualTo(0f).Within(.0001f));
    }
    [Test]
    public void DistanceToPolyline_UsesNearestSegment()
    {
        var points = new[] { Vector2.zero, Vector2.right, Vector2.one };
        Assert.That(TracingPathMath.DistanceToPolyline(new Vector2(.5f, .2f), points), Is.EqualTo(.2f).Within(.0001f));
    }
}
