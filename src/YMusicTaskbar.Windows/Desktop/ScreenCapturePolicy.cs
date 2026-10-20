using System;

namespace YMusicTaskbar;

public static class ScreenCapturePolicy
{
    public static bool IsSelectionOverlay(string processName, PixelRect window, PixelRect monitor)
        => IsCaptureProcess(processName) && FullscreenWindowPolicy.CoversMonitor(window, monitor);

    public static bool IsCaptureProcess(string processName)
        => string.Equals(processName, "ScreenClippingHost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processName, "SnippingTool", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processName, "ScreenSketch", StringComparison.OrdinalIgnoreCase);
}