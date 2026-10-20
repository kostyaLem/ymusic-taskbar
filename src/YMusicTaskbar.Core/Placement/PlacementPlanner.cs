using System;
using System.Collections.Generic;
using System.Linq;
namespace YMusicTaskbar;

public static class PlacementPlanner
{
    public static Placement? Choose(IReadOnlyList<DesktopSlot> slots, string? preferred, bool compact = false, int compactWidth = 360)
    {
        return ChooseForSettings(slots, new Settings { PreferredMonitor = preferred, Compact = compact }, compactWidth);
    }
    public static Placement? ChooseForSettings(IReadOnlyList<DesktopSlot> slots, Settings settings, int compactWidth = 360)
    {
        string? preferred = settings.PreferredMonitor;
        var candidates = new Dictionary<string, Placement>();
        foreach (var slot in slots)
        {
            if (slot.Fullscreen || !slot.Visible || slot.Taskbar.Width <= slot.Taskbar.Height) continue;
            var profile = settings.ProfileFor(slot.Monitor);
            if (!profile.Enabled) continue;
            bool compact = profile.Compact;
            uint dpi = slot.Dpi;
            double scale = dpi / 96.0;
            int height = Math.Min((int)Math.Round((compact ? 32 : 46) * scale), slot.Taskbar.Height - 4);
            if (height < (compact ? 24 : 30) * scale) continue;
            foreach (int logicalWidth in compact ? new[] { compactWidth, Math.Min(compactWidth, 320), 298 }.Distinct() : new[] { 540, 480, 420, 360 })
            {
                int width = (int)Math.Round(logicalWidth * scale);
                double? x = Policy.FindGap(slot.Taskbar.Left, slot.Taskbar.Right, width, slot.Occupied, 8 * scale, profile.Side);
                if (x is not null)
                {
                    candidates[slot.Monitor] = new(slot.Monitor, (int)x, slot.Taskbar.Top + (slot.Taskbar.Height - height) / 2, width, height, dpi);
                    break;
                }
            }
        }
        string? chosen = Policy.ChooseMonitor(slots.Select(s => new MonitorOption(s.Monitor,
            preferred is null ? s.Primary : s.Monitor == preferred, s.Fullscreen, s.Visible, candidates.ContainsKey(s.Monitor))));
        return chosen is not null ? candidates[chosen] : null;
    }
}
