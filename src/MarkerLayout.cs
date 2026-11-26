using System;
using System.Collections.Generic;

namespace ScoutsMap
{
    internal readonly struct MarkerPoint
    {
        internal readonly float X;
        internal readonly float Y;

        internal MarkerPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    internal readonly struct MarkerBounds
    {
        internal readonly float Left, Bottom, Right, Top;

        internal MarkerBounds(float left, float bottom, float right, float top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }
    }

    internal static class MarkerLayout
    {
        internal const float BadgeSize = 32f;

        internal static bool TryPlaceTooltip(MarkerBounds frame, MarkerPoint badge,
            float width, float height, float scale, out MarkerPoint point)
        {
            point = default;
            float left = frame.Left + width / 2f;
            float right = frame.Right - width / 2f;
            float bottom = frame.Bottom + height / 2f;
            float top = frame.Top - height / 2f;
            if (left > right || bottom > top)
                return false;

            // Place the callout on the side with room, away from badges stacked along the same edge.
            float gap = (BadgeSize / 2f + 7f) * scale;
            float dx = width / 2f + gap;
            float dy = height / 2f + gap;
            float x = badge.X + (badge.X < (left + right) / 2f ? dx : -dx);
            if (x >= left && x <= right)
            {
                point = new MarkerPoint(x, Math.Clamp(badge.Y, bottom, top));
                return true;
            }
            float y = badge.Y + (badge.Y < (bottom + top) / 2f ? dy : -dy);
            if (y < bottom || y > top)
                return false;

            point = new MarkerPoint(Math.Clamp(badge.X, left, right), y);
            return true;
        }

        internal static bool TryPlace(MarkerBounds frame, float targetX, float targetY,
            float centerX, float centerY, float scale, List<MarkerPoint> occupied, out MarkerPoint result)
        {
            result = default;
            // The native marker frame leaves room for the HUD. Centered destinations need no arrow.
            if (Math.Abs(targetX - centerX) < centerX * 0.53f &&
                Math.Abs(targetY - centerY) < centerY * 0.48f)
                return false;

            float half = BadgeSize * scale / 2f;
            float left = frame.Left + half;
            float right = frame.Right - half;
            float bottom = frame.Bottom + half;
            float top = frame.Top - half;
            if (left > right || bottom > top)
                return false;

            float midX = (left + right) / 2f;
            float midY = (bottom + top) / 2f;
            float x = Math.Clamp(midX + 1.2f * (targetX - centerX), left, right);
            float y = Math.Clamp(midY + 1.2f * (targetY - centerY), bottom, top);
            bool slideVertically = x == left || x == right ||
                (y != bottom && y != top && Math.Abs(x - midX) > Math.Abs(y - midY));
            float spacing = (BadgeSize + 4f) * scale;

            // Prefer the projected position, then spread equally to either side along its edge.
            for (int slot = 0; slot <= occupied.Count * 2; slot++)
            {
                int step = (slot + 1) / 2 * (slot % 2 == 0 ? -1 : 1);
                float offset = step * spacing;
                var candidate = slideVertically
                    ? new MarkerPoint(x, y + offset)
                    : new MarkerPoint(x + offset, y);
                if (candidate.X < left || candidate.X > right || candidate.Y < bottom || candidate.Y > top)
                    continue;
                bool clear = true;
                foreach (var point in occupied)
                {
                    if (Math.Abs(point.X - candidate.X) < spacing && Math.Abs(point.Y - candidate.Y) < spacing)
                    {
                        clear = false;
                        break;
                    }
                }
                if (!clear)
                    continue;
                occupied.Add(candidate);
                result = candidate;
                return true;
            }
            return false;
        }
    }
}
