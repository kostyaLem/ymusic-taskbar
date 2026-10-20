using System;

namespace YMusicTaskbar;

public sealed record VolumeNotification(string SessionId, Guid Registration, double Level, bool Muted,
    Guid Context, bool IsOwnChange, DateTimeOffset At, long Timestamp);
