using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal sealed class FakeAudioService : IAudioService
{
    public bool Available { get; set; } = true;
    public double Level { get; private set; } = 42;
    public bool Muted { get; private set; }
    public event EventHandler<VolumeNotification>? VolumeChanged { add { } remove { } }
    public event EventHandler? SessionsInvalidated { add { } remove { } }
    public void Refresh() { }
    public void Reset() { }
    public bool AcceptNotification(VolumeNotification notification) => false;
    public void SetLevel(double value, bool unmute = false) { Level = Math.Clamp(value, 0, 100); if (unmute && Level > 0) Muted = false; }
    public void SetMuted(bool muted) => Muted = muted;
    public AudioSnapshot Snapshot() => new(Available, Level, Muted, 0, 0, 0, 0, 0, 0, []);
    public void Dispose() { }
}
