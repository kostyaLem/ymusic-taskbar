using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
namespace YMusicTaskbar;

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
public sealed class SessionEvents : IAudioSessionEvents
{
    private readonly string id;
    private readonly Guid registration, ownContext;
    private readonly Action<VolumeNotification> notify;
    private readonly Action invalidate;
    private int active = 1;
    public SessionEvents(string id, Guid registration, Guid ownContext, Action<VolumeNotification> notify, Action invalidate)
    { this.id = id; this.registration = registration; this.ownContext = ownContext; this.notify = notify; this.invalidate = invalidate; }
    public void Deactivate() => Interlocked.Exchange(ref active, 0);
    public int OnSimpleVolumeChanged(float volume, bool mute, IntPtr context)
    {
        if (Volatile.Read(ref active) == 0) return 0;
        try
        {
            Guid source = context == IntPtr.Zero ? Guid.Empty : Marshal.PtrToStructure<Guid>(context);
            notify(new(id, registration, volume * 100d, mute, source, source == ownContext, DateTimeOffset.Now, Stopwatch.GetTimestamp()));
        }
        catch { } // Exceptions must never cross a native callback boundary.
        return 0;
    }
    private int Changed() { try { if (Volatile.Read(ref active) != 0) invalidate(); } catch { } return 0; }
    public int OnStateChanged(int state) => Changed();
    public int OnSessionDisconnected(int reason) => Changed();
    public int OnDisplayNameChanged(string name, IntPtr context) => 0;
    public int OnIconPathChanged(string path, IntPtr context) => 0;
    public int OnChannelVolumeChanged(uint count, IntPtr volumes, uint channel, IntPtr context) => 0;
    public int OnGroupingParamChanged(IntPtr grouping, IntPtr context) => 0;
}
