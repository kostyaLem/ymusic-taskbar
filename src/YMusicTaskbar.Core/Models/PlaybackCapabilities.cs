namespace YMusicTaskbar;

public sealed record PlaybackCapabilities(
    bool Previous = false, bool Next = false, bool Play = false, bool Pause = false,
    bool Toggle = false, bool Seek = false, bool Repeat = false, bool Shuffle = false);
