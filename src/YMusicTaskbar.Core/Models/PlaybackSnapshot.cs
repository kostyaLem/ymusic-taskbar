namespace YMusicTaskbar;

public sealed record PlaybackSnapshot(string? Source, string Title, string Artist, bool Playing,
    double Duration, double Position, bool HasCover, PlaybackCapabilities Controls);
