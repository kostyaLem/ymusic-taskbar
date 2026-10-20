using System.Collections.Generic;

namespace YMusicTaskbar;

public sealed record DesktopSlot(string Monitor, bool Primary, PixelRect Bounds, PixelRect Taskbar,
    bool Fullscreen, bool Visible, IReadOnlyList<Span> Occupied, string? Error, uint Dpi = 96);
