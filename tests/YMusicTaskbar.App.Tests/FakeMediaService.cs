using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal sealed class FakeMediaService : IMediaService
{
    public string Title { get; set; } = "Проверочный трек";
    public string Artist { get; set; } = "Исполнитель";
    public byte[]? Artwork => null;
    public bool Playing { get; set; }
    public double Duration => 120;
    public double Position { get; set; } = 10;
    public double DisplayPosition => Position;
    public PlaybackCapabilities Capabilities { get; set; } = new(true, true, true, true, true, true);
    public int Calls { get; private set; }
    public PlayerCommand LastCommand { get; private set; }
    public double LastValue { get; private set; }
    public bool Accepted { get; set; } = true;
    public TaskCompletionSource<bool>? Pending { get; set; }
    public Task InitializeAsync() => Task.CompletedTask;
    public Task RefreshAsync() => Task.CompletedTask;
    public Task<bool> ExecuteAsync(PlayerCommand command, double value = 0)
    { Calls++; LastCommand = command; LastValue = value; return Pending?.Task ?? Task.FromResult(Accepted); }
    public PlaybackSnapshot Snapshot() => new("fake", Title, Artist, Playing, Duration, Position, false, Capabilities);
    public void Dispose() { }
}
