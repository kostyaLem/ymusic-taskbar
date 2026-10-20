using System;
using System.Collections.Generic;
using System.Linq;

namespace YMusicTaskbar;

public sealed class DesktopSnapshotCache
{
    private readonly Dictionary<string, (DesktopSlot Slot, DateTimeOffset At)> lastGood = new();
    public IReadOnlyList<DesktopSlot> Merge(IReadOnlyList<DesktopSlot> incoming, DateTimeOffset now) => incoming.Select(slot =>
    {
        if (slot.Error is null) { lastGood[slot.Monitor] = (slot, now); return slot; }
        if (lastGood.TryGetValue(slot.Monitor, out var old) && now - old.At < TimeSpan.FromSeconds(3))
            return slot with { Visible = old.Slot.Visible, Occupied = old.Slot.Occupied };
        return slot;
    }).ToArray();
}
