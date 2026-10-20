using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YMusicTaskbar;

internal sealed class DesktopMonitor(IDesktopService desktop, NotificationService notifications)
{
    private readonly DesktopSnapshotCache cache = new();
    private Task<IReadOnlyList<DesktopSlot>>? pending;
    private DateTimeOffset nextScan;
    public IReadOnlyList<DesktopSlot> Current { get; private set; } = Array.Empty<DesktopSlot>();
    public void Tick(DateTimeOffset now)
    {
        if (pending?.IsCompleted == true)
        {
            try { Current = cache.Merge(pending.GetAwaiter().GetResult(), now); }
            catch (Exception exception) { notifications.Log(exception); }
            pending = null;
        }
        if (pending is null && now >= nextScan)
        { pending = Task.Run(desktop.Inspect); nextScan = now.AddMilliseconds(750); }
    }
}
