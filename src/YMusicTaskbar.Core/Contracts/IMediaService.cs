using System;
using System.Threading.Tasks;

namespace YMusicTaskbar;

public interface IMediaService : IDisposable
{
    string Title { get; }
    string Artist { get; }
    byte[]? Artwork { get; }
    bool Playing { get; }
    double Duration { get; }
    double Position { get; }
    double DisplayPosition { get; }
    PlaybackCapabilities Capabilities { get; }
    Task InitializeAsync();
    Task RefreshAsync();
    Task<bool> ExecuteAsync(PlayerCommand command, double positionSeconds = 0);
    PlaybackSnapshot Snapshot();
}
