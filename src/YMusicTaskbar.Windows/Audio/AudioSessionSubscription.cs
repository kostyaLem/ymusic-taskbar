using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace YMusicTaskbar;

internal sealed class AudioSessionSubscription : IDisposable
{
    public readonly IAudioSessionControl2 Control;
    public readonly ISimpleAudioVolume Volume;
    public readonly SessionEvents Sink;
    public readonly Guid Token = Guid.NewGuid();
    public bool Active;
    public double Level;
    public bool Muted;
    public long LastRead;
    private bool registered;
    public AudioSessionSubscription(string id, IAudioSessionControl2 control, ISimpleAudioVolume volume,
        Action<VolumeNotification> notify, Action invalidate, Guid ownContext)
    {
        Control = control; Volume = volume;
        Sink = new SessionEvents(id, Token, ownContext, notify, invalidate);
    }
    public void Register() { ComInterop.Check(Control.RegisterAudioSessionNotification(Sink)); registered = true; }
    public void Read()
    {
        // An older queued notification must not overwrite a newer native read.
        long stamp = Stopwatch.GetTimestamp();
        ComInterop.Check(Volume.GetMasterVolume(out float level)); ComInterop.Check(Volume.GetMute(out bool muted));
        Level = level * 100; Muted = muted; LastRead = stamp;
    }
    public void Dispose()
    {
        Sink.Deactivate();
        try { if (registered) Control.UnregisterAudioSessionNotification(Sink); }
        finally { registered = false; ComInterop.Release(Control); }
    }
}
