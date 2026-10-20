using YMusicTaskbar;

internal sealed class PreviewMedia(byte[] artwork) : IMediaService
{
    public string Title => "Тёплый вечер";
    public string Artist => "Демо-исполнитель";
    public byte[]? Artwork => artwork;
    public bool Playing => true;
    public double Duration => 224;
    public double Position => 86;
    public double DisplayPosition => Position;
    public PlaybackCapabilities Capabilities => new(true, true, true, true, true, true);
    public Task InitializeAsync() => Task.CompletedTask;
    public Task RefreshAsync() => Task.CompletedTask;
    public Task<bool> ExecuteAsync(PlayerCommand command, double positionSeconds = 0) => throw new InvalidOperationException("Documentation preview is read-only");
    public PlaybackSnapshot Snapshot() => new("demo", Title, Artist, Playing, Duration, Position, true, Capabilities);
    public void Dispose() { }
}
