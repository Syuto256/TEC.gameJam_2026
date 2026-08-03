using System.Collections.Generic;
using UnityEngine;

namespace Overwork.MiniGames.Tracing
{
    public static class TracingPathMath
    {
        public static float DistanceToPolyline(Vector2 point, IReadOnlyList<Vector2> points)
        {
            var minimum = float.MaxValue;
            for (var index = 0; index < points.Count - 1; index++)
            {
                var from = points[index]; var to = points[index + 1]; var segment = to - from;
                var lengthSquared = segment.sqrMagnitude;
                var projected = lengthSquared <= Mathf.Epsilon ? from : from + segment * Mathf.Clamp01(Vector2.Dot(point - from, segment) / lengthSquared);
                minimum = Mathf.Min(minimum, Vector2.Distance(point, projected));
            }
            return minimum;
        }
    }
}
