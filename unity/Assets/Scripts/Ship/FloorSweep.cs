using System.Collections.Generic;
using UnityEngine;

// Axis-separated sweep of a round walker against rectangles in the XZ plane (Rect.y is world Z).
// The walker slides along edges instead of stopping dead and never tunnels through thin glass.
public static class FloorSweep
{
    public static Vector3 Move(Vector3 from, Vector3 to, IReadOnlyList<Rect> walls, float radius)
    {
        var result = from;
        result.x = Axis(from.x, to.x, from.z, walls, radius, true);
        result.z = Axis(from.z, to.z, result.x, walls, radius, false);
        result.y = to.y;
        return result;
    }

    static float Axis(float from, float to, float cross, IReadOnlyList<Rect> walls, float radius, bool horizontal)
    {
        float result = to;
        foreach (var wall in walls)
        {
            if (wall.width <= 0 || wall.height <= 0) continue;
            float crossMin = (horizontal ? wall.yMin : wall.xMin) - radius;
            float crossMax = (horizontal ? wall.yMax : wall.xMax) + radius;
            if (cross <= crossMin || cross >= crossMax) continue;
            float minimum = (horizontal ? wall.xMin : wall.yMin) - radius;
            float maximum = (horizontal ? wall.xMax : wall.yMax) + radius;
            // Only entries from outside are blocked, so a walker placed inside can always leave.
            if (to > from && from <= minimum && result > minimum) result = minimum;
            if (to < from && from >= maximum && result < maximum) result = maximum;
        }
        return result;
    }
}
