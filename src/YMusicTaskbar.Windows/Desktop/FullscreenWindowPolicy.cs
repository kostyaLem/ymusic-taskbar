namespace YMusicTaskbar;

public static class FullscreenWindowPolicy
{
    // Capture borders and desktop overlays are often monitor-sized but do not cover its content.
    private const uint ToolWindow = 0x00000080;
    private const uint NoActivate = 0x08000000;
    private const uint Transparent = 0x00000020;

    public static bool CanCoverDesktop(uint extendedStyle, byte? opacity = null, bool colorKeyed = false)
        => (extendedStyle & (ToolWindow | NoActivate | Transparent)) == 0
            && !colorKeyed && (opacity is null or 255);

    public static bool CoversMonitor(PixelRect window, PixelRect monitor)
        => window.Width > 0 && window.Height > 0 && monitor.Width > 0 && monitor.Height > 0
            && window.Left <= monitor.Left + 2 && window.Top <= monitor.Top + 2
            && window.Right >= monitor.Right - 2 && window.Bottom >= monitor.Bottom - 2;
}