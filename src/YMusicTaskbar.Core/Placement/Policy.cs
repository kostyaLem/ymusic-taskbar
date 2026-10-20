using System;
using System.Collections.Generic;
using System.Linq;

namespace YMusicTaskbar;


public static class Policy
{
    public static double ValueAtX(double x, double width, double maximum)
        => Math.Clamp((x - 5) / Math.Max(1, width - 10), 0, 1) * Math.Max(0, maximum);
    // Everything here uses physical pixels. Convert only the WPF content size at the boundary.
    public static double? FindGap(double left, double right, double width, IEnumerable<Span> occupied, double padding = 8, PlayerSide side = PlayerSide.Left)
    {
        if (side == PlayerSide.Right)
        {
            var mirrored = FindGap(-right, -left, width, occupied.Select(s => new Span(-s.End, -s.Start)), padding);
            return mirrored is { } value ? -value - width : null;
        }
        var cursor = left + padding;
        foreach (var span in occupied.Where(s => s.End > left && s.Start < right).OrderBy(s => s.Start))
        {
            if (span.Start - padding - cursor >= width) return cursor;
            cursor = Math.Max(cursor, span.End + padding);
        }
        return right - padding - cursor >= width ? cursor : null;
    }

    public static string? ChooseMonitor(IEnumerable<MonitorOption> monitors) => monitors
        .Where(m => !m.Fullscreen && m.TaskbarVisible && m.HasRoom)
        .OrderByDescending(m => m.Preferred).Select(m => m.Name).FirstOrDefault();

    public static double PositionSeconds(double reported, double lastUpdatedAgo, bool playing, double duration)
        => Math.Clamp(reported + (playing ? Math.Max(0, lastUpdatedAgo) : 0), 0, Math.Max(0, duration));
}
