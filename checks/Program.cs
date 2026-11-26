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
    var corner = new MarkerPoint(safe.Left + 16 * scale, safe.Top - 16 * scale);
    if (!MarkerLayout.TryPlaceTooltip(safe, corner, 200 * scale, 54 * scale, scale, out var tip) ||
        tip.X - 100 * scale < corner.X + 16 * scale ||
        tip.X + 100 * scale > safe.Right || tip.Y + 27 * scale > safe.Top ||
        tip.Y - 27 * scale < safe.Bottom)
        throw new Exception($"A wrapped hover reason should remain inside the HUD reserve at {scale}x.");
    if (MarkerLayout.TryPlaceTooltip(new MarkerBounds(0, 0, 48, 48),
        new MarkerPoint(24, 24), 75, 60, scale, out _))
        throw new Exception("An oversized tooltip should not cover the HUD or its own marker.");
    occupied.Clear();
    if (!MarkerLayout.TryPlaceAbove(safe, new MarkerPoint(640, 360), scale, out var centered) ||
        centered.X != 640 || centered.Y - 16 * scale != 360 + 4 * scale ||
        !MarkerLayout.TryReserve(centered, scale, occupied))
        throw new Exception($"A visible target should have a centered badge above its sprite at {scale}x.");
    if (MarkerLayout.TryReserve(centered, scale, occupied))
        throw new Exception("Two targets must not get overlapping hit boxes.");
    if (MarkerLayout.TryPlaceAbove(safe, new MarkerPoint(640, safe.Top), scale, out _))
        throw new Exception("A target under the HUD cannot have an anchored badge there.");
    if (!MarkerLayout.TryPlaceEdge(safe, 2000, 1450, 640, 360, scale, occupied, out var first))
        throw new Exception("An off-screen encounter should fit inside the native marker frame.");
    if (!MarkerLayout.TryPlaceEdge(safe, 2000, 1450, 640, 360, scale, occupied, out var second))
        throw new Exception("Two encounters sharing a bearing should both be reachable.");
    foreach (var point in occupied)
    {
        if (point.X - 16 * scale < safe.Left || point.X + 16 * scale > safe.Right ||
            point.Y - 16 * scale < safe.Bottom || point.Y + 16 * scale > safe.Top)
            throw new Exception($"Badge overlaps the HUD reserve at {scale}x.");
    }
    if (Math.Abs(first.X - second.X) < 32 * scale && Math.Abs(first.Y - second.Y) < 32 * scale)
        throw new Exception($"Two click targets overlap at {scale}x.");
    if (MarkerLayout.TryPlaceEdge(new MarkerBounds(0, 0, 18, 18), 2000, 1450, 640, 360, scale, occupied, out _))
        throw new Exception("A viewport smaller than a badge must leave the HUD clear.");
}
Console.WriteLine("PASS anchored alignment, HUD clearance, and marker separation at 1x and 1.5x.");
