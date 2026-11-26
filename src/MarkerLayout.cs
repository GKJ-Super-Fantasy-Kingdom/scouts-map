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

        internal static bool TryPlaceAbove(MarkerBounds frame, MarkerPoint top, float scale, out MarkerPoint result)
        {
            float half = BadgeSize * scale / 2f;
            result = new MarkerPoint(top.X, top.Y + half + 4f * scale);
            return result.X - half >= frame.Left && result.X + half <= frame.Right &&
                result.Y - half >= frame.Bottom && result.Y + half <= frame.Top;
        }

        internal static bool TryReserve(MarkerPoint candidate, float scale, List<MarkerPoint> occupied)
        {
            float spacing = (BadgeSize + 4f) * scale;
            foreach (var point in occupied)
            {
                if (Math.Abs(point.X - candidate.X) < spacing && Math.Abs(point.Y - candidate.Y) < spacing)
                    return false;
            }
            occupied.Add(candidate);
            return true;
        }

        internal static bool TryPlaceTooltip(MarkerBounds frame, MarkerPoint badge,
            float width, float height, float scale, IReadOnlyList<MarkerPoint> occupied, out MarkerPoint point)
        {
            point = default;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            float left = frame.Left + halfWidth;
            float right = frame.Right - halfWidth;
            float bottom = frame.Bottom + halfHeight;
            float top = frame.Top - halfHeight;
            if (left > right || bottom > top)
                return false;

            float badgeHalf = BadgeSize / 2f * scale;
            float gap = badgeHalf + 7f * scale;
            float clearance = badgeHalf + 4f * scale;
            float dx = (halfWidth + gap) * (badge.X < (left + right) / 2f ? 1f : -1f);
            float dy = (halfHeight + gap) * (badge.Y < (bottom + top) / 2f ? 1f : -1f);

            // Try each side beside the hovered badge before shifting the panel along a side.
            for (int shift = 0; shift < 3; shift++)
            {
                float offset = shift == 2 ? -1f : shift;
                float x = Math.Clamp(badge.X + offset * halfWidth, left, right);
                float y = Math.Clamp(badge.Y + offset * halfHeight, bottom, top);
                if (TryAt(badge.X + dx, y, out point) || TryAt(badge.X - dx, y, out point) ||
                    TryAt(x, badge.Y + dy, out point) || TryAt(x, badge.Y - dy, out point))
                    return true;
            }
            return false;

            bool TryAt(float x, float y, out MarkerPoint candidate)
            {
                candidate = default;
                if (x < left || x > right || y < bottom || y > top)
                    return false;
                foreach (var marker in occupied)
                {
                    if (Math.Abs(x - marker.X) < halfWidth + clearance &&
                        Math.Abs(y - marker.Y) < halfHeight + clearance)
                        return false;
                }
                candidate = new MarkerPoint(x, y);
                return true;
            }
        }

        internal static bool TryPlaceEdge(MarkerBounds frame, float targetX, float targetY,
            float centerX, float centerY, float scale, List<MarkerPoint> occupied, out MarkerPoint result)
        {
            result = default;
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
                if (!TryReserve(candidate, scale, occupied))
                    continue;
                result = candidate;
                return true;
            }
            return false;
        }
    }
}
