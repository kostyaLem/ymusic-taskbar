using YMusicTaskbar;

internal sealed class PreviewAudio : IAudioService
{
    public bool Available => true;
    public double Level => 56;
    public bool Muted => false;
    public event EventHandler<VolumeNotification>? VolumeChanged { add { } remove { } }
    public event EventHandler? SessionsInvalidated { add { } remove { } }
    public void Refresh() { }
    public void Reset() { }
    public bool AcceptNotification(VolumeNotification notification) => false;
    public void SetLevel(double value, bool unmute = false) => throw new InvalidOperationException("Documentation preview is read-only");
    public void SetMuted(bool muted) => throw new InvalidOperationException("Documentation preview is read-only");
    public AudioSnapshot Snapshot() => new(true, Level, Muted, 0, 0, 0, 0, 0, 0, []);
    public void Dispose() { }
}
