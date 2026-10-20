using System;

namespace YMusicTaskbar;

public interface IAudioService : IDisposable
{
    bool Available { get; }
    double Level { get; }
    bool Muted { get; }
    event EventHandler<VolumeNotification>? VolumeChanged;
    event EventHandler? SessionsInvalidated;
    void Refresh();
    void Reset();
    bool AcceptNotification(VolumeNotification notification);
    void SetLevel(double value, bool unmute = false);
    void SetMuted(bool muted);
    AudioSnapshot Snapshot();
}
