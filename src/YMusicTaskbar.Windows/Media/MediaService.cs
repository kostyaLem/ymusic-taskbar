using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Media.Control;

namespace YMusicTaskbar;

public sealed class MediaService : IMediaService
{
    private GlobalSystemMediaTransportControlsSessionManager? manager;
    private volatile bool metadataDirty = true;
    private readonly SeekPreview seekPreview = new();
    private GlobalSystemMediaTransportControlsSession? session;

    private GlobalSystemMediaTransportControlsSessionPlaybackInfo? Playback { get; set; }
    private GlobalSystemMediaTransportControlsSessionTimelineProperties? Timeline { get; set; }
    public string Title { get; private set; } = "Запустите трек в музыкальном приложении";
    public string Artist { get; private set; } = "";
    public byte[]? Artwork { get; private set; }
    public bool Playing => Playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

    public async Task InitializeAsync() => manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(8));

    private void OnMetadata(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) => metadataDirty = true;

    public async Task RefreshAsync()
    {
        if (manager is null) await InitializeAsync();
        var found = manager!.GetSessions().FirstOrDefault(s => CompatibilityIds.IsMusicSession(s.SourceAppUserModelId));
        if (!Equals(found, session))
        {
            seekPreview.Cancel();
            if (session is not null) session.MediaPropertiesChanged -= OnMetadata;
            session = found;
            if (session is not null) session.MediaPropertiesChanged += OnMetadata;
            metadataDirty = true;
            Artwork = null;
            Title = "Запустите трек в музыкальном приложении";
            Artist = "";
        }
        Playback = session?.GetPlaybackInfo();
        Timeline = session?.GetTimelineProperties();
        if (session is null || !metadataDirty) return;
        metadataDirty = false;
        try
        {
            var properties = await session.TryGetMediaPropertiesAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(4));
            string title = string.IsNullOrWhiteSpace(properties.Title) ? "Музыкальное приложение" : properties.Title;
            if (Title != title) seekPreview.Cancel();
            Title = title;
            Artist = properties.Artist;
            Artwork = null;
            if (properties.Thumbnail is null) return;
            using var source = await properties.Thumbnail.OpenReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(4));
            if (source.Size == 0 || source.Size > 8 * 1024 * 1024) return;
            using var stream = source.AsStreamForRead();
            using var output = new MemoryStream();
            await stream.CopyToAsync(output);
            Artwork = output.ToArray();
        }
        catch { metadataDirty = true; throw; }
    }

    public double Duration => Math.Max(0, (Timeline?.EndTime - Timeline?.StartTime)?.TotalSeconds ?? 0);
    public double Position => Timeline is null ? 0 : Policy.PositionSeconds(
        (Timeline.Position - Timeline.StartTime).TotalSeconds,
        (DateTimeOffset.UtcNow - Timeline.LastUpdatedTime).TotalSeconds, Playing, Duration);
    public double DisplayPosition => seekPreview.Display(Position, Playing, Duration, DateTimeOffset.UtcNow);

    public async Task<bool> ExecuteAsync(PlayerCommand command, double positionSeconds = 0)
    {
        var s = session;
        var c = s?.GetPlaybackInfo().Controls;
        if (s is null || c is null) return false;
        var timeline = command == PlayerCommand.Seek ? s.GetTimelineProperties() : Timeline;
        double? target = SeekTarget.Normalize(positionSeconds, (timeline?.EndTime - timeline?.StartTime)?.TotalSeconds ?? 0);
        var operation = command switch
        {
            PlayerCommand.Next when c.IsNextEnabled => s.TrySkipNextAsync(),
            PlayerCommand.Previous when c.IsPreviousEnabled => s.TrySkipPreviousAsync(),
            PlayerCommand.TogglePlayPause when c.IsPlayPauseToggleEnabled => s.TryTogglePlayPauseAsync(),
            PlayerCommand.TogglePlayPause when Playing && c.IsPauseEnabled => s.TryPauseAsync(),
            PlayerCommand.TogglePlayPause when !Playing && c.IsPlayEnabled => s.TryPlayAsync(),
            PlayerCommand.Seek when c.IsPlaybackPositionEnabled && target.HasValue => s.TryChangePlaybackPositionAsync((timeline!.StartTime + TimeSpan.FromSeconds(target.Value)).Ticks),
            _ => null
        };
        if (command == PlayerCommand.Seek && operation is not null) seekPreview.Begin(target!.Value, DateTimeOffset.UtcNow);
        try
        {
            bool accepted = operation is not null && await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(4));
            if (command == PlayerCommand.Seek && !accepted) seekPreview.Cancel();
            return accepted;
        }
        catch { if (command == PlayerCommand.Seek) seekPreview.Cancel(); throw; }
    }

    public PlaybackCapabilities Capabilities
    {
        get
        {
            var c = Playback?.Controls;
            return c is null ? new() : new(c.IsPreviousEnabled, c.IsNextEnabled, c.IsPlayEnabled, c.IsPauseEnabled,
                c.IsPlayPauseToggleEnabled, c.IsPlaybackPositionEnabled, c.IsRepeatEnabled, c.IsShuffleEnabled);
        }
    }
    public PlaybackSnapshot Snapshot() => new(session?.SourceAppUserModelId, Title, Artist, Playing,
        Duration, Position, Artwork is not null, Capabilities);
    public void Dispose() { if (session is not null) session.MediaPropertiesChanged -= OnMetadata; }
}
