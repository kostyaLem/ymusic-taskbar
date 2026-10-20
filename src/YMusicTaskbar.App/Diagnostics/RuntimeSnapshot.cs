using System;
using System.Collections.Generic;

namespace YMusicTaskbar;

internal sealed record RuntimeSnapshot(DateTimeOffset CapturedAt, PlaybackSnapshot Media, AudioSnapshot Audio,
    bool MusicRunning, bool Visible, string Status, string? Error, Placement? Placement, object? Ui,
    IReadOnlyList<DesktopSlot> Monitors)
{
    public WindowsVersionInfo OperatingSystem { get; init; } = WindowsVersionInfo.Capture();
}
