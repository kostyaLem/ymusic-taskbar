using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal static class ScreenCaptureScenarios
{
    public static void Run(Action<bool, string> check)
    {
        var screen = new PixelRect(0, 0, 1920, 1080);
        check(ScreenCapturePolicy.IsSelectionOverlay("ScreenClippingHost", screen, screen), "Windows 10 capture selector suppresses the live overlay");
        check(ScreenCapturePolicy.IsSelectionOverlay("SnippingTool", screen, screen), "Windows 11 capture selector suppresses the live overlay");
        check(ScreenCapturePolicy.IsSelectionOverlay("screensketch", screen, screen), "Capture identity matching is case-insensitive");
        check(!ScreenCapturePolicy.IsSelectionOverlay("SnippingTool", new(100, 100, 1200, 900), screen), "Ordinary Snipping Tool editor does not suppress the player");
        check(!ScreenCapturePolicy.IsSelectionOverlay("SnippingTool", new(0, 0, 1920, 1040), screen), "Maximized editor above taskbar does not suppress the player");
        check(!ScreenCapturePolicy.IsSelectionOverlay("ms-teams", screen, screen), "Teams screen sharing does not trigger capture-selection suppression");
        check(!ScreenCapturePolicy.IsSelectionOverlay("Codex", screen, screen), "Desktop control does not trigger capture-selection suppression");
        check(!ScreenCapturePolicy.IsSelectionOverlay("OtherSnippingTool", screen, screen), "Capture process matching does not use name substrings");
        var left = new PixelRect(-1920, 0, 0, 1080);
        check(ScreenCapturePolicy.IsSelectionOverlay("ScreenClippingHost", new(-1920, 0, 1920, 1080), left), "Capture selection spanning displays is recognized on negative coordinates");
        check(!ScreenCapturePolicy.IsSelectionOverlay("SnippingTool", default, screen), "Missing capture bounds do not suppress the player");
        using var monitor = new ScreenCaptureMonitor();
        _ = monitor.IsSelecting;
        monitor.Dispose();
        check(true, "Native capture monitor scans real windows and supports repeated disposal");
    }
}