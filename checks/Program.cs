using System;
using System.Collections.Generic;
using ScoutsMap;

var occupied = new List<MarkerPoint>();
foreach (var (scale, safe) in new[]
{
    (1f, new MarkerBounds(200, 110, 1080, 580)),
    (1.5f, new MarkerBounds(300, 165, 980, 510))
})
{
    occupied.Clear();
    if (!MarkerLayout.TryPlace(safe, 2000, 1450, 640, 360, scale, occupied, out var first))
        throw new Exception("An off-screen encounter should fit inside the native marker frame.");
    if (!MarkerLayout.TryPlace(safe, 2000, 1450, 640, 360, scale, occupied, out var second))
        throw new Exception("Two encounters sharing a bearing should both be reachable.");
    foreach (var point in occupied)
    {
        if (point.X - 16 * scale < safe.Left || point.X + 16 * scale > safe.Right ||
            point.Y - 16 * scale < safe.Bottom || point.Y + 16 * scale > safe.Top)
            throw new Exception($"Badge overlaps the HUD reserve at {scale}x.");
    }
    if (Math.Abs(first.X - second.X) < 32 * scale && Math.Abs(first.Y - second.Y) < 32 * scale)
        throw new Exception($"Two click targets overlap at {scale}x.");
    if (MarkerLayout.TryPlace(safe, 640, 360, 640, 360, scale, occupied, out _))
        throw new Exception("A centered target should release its navigation badge.");
    if (MarkerLayout.TryPlace(new MarkerBounds(0, 0, 18, 18), 2000, 1450, 640, 360, scale, occupied, out _))
        throw new Exception("A viewport smaller than a badge must leave the HUD clear.");
}
Console.WriteLine("PASS marker clearance, separation and centered-target removal at 1x and 1.5x.");
