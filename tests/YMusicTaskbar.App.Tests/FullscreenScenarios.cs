using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal static class FullscreenScenarios
{
    public static void Run(Action<bool, string> check)
    {
        check(!FullscreenWindowPolicy.CanCoverDesktop(0x00000080), "Screen-sized tool window is not fullscreen content");
        check(!FullscreenWindowPolicy.CanCoverDesktop(0x08000000), "Non-activating capture border does not hide player");
        check(!FullscreenWindowPolicy.CanCoverDesktop(0x00080020), "Transparent click-through capture overlay does not hide player");
        check(!FullscreenWindowPolicy.CanCoverDesktop(0x00080000, 0), "Invisible layered window does not hide player");
        check(!FullscreenWindowPolicy.CanCoverDesktop(0x00080000, 100), "Translucent overlay does not hide player");
        check(!FullscreenWindowPolicy.CanCoverDesktop(0x00080000, 255, true), "Color-keyed capture border does not hide player");
        check(FullscreenWindowPolicy.CanCoverDesktop(0), "Normal borderless fullscreen application remains eligible");
        check(FullscreenWindowPolicy.CanCoverDesktop(8), "Topmost fullscreen application remains eligible");
        check(FullscreenWindowPolicy.CanCoverDesktop(0x00080000, 255), "Opaque layered fullscreen application remains eligible");
        check(FullscreenWindowPolicy.CanCoverDesktop(0x00080000), "Per-pixel layered application is not rejected solely for layering");
        var monitor = new PixelRect(0, 0, 1920, 1080);
        check(FullscreenWindowPolicy.CoversMonitor(monitor, monitor), "Fullscreen content covers its monitor");
        check(!FullscreenWindowPolicy.CoversMonitor(new(0, 0, 1920, 1040), monitor), "Maximized application above taskbar is not fullscreen");
        check(FullscreenWindowPolicy.CoversMonitor(new(-1920, 0, 0, 1080), new(-1920, 0, 0, 1080)), "Fullscreen detection supports negative monitor coordinates");
        check(!FullscreenWindowPolicy.CoversMonitor(new(-1920, 0, 0, 1080), monitor), "Fullscreen on another monitor does not hide this monitor");
        check(!FullscreenWindowPolicy.CoversMonitor(default, default), "Missing window bounds never count as fullscreen");
    }
}