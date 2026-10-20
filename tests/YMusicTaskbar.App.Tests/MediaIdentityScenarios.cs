using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal static class MediaIdentityScenarios
{
    public static void Run(Action<bool, string> check)
    {
        check(CompatibilityIds.IsMusicSession(CompatibilityIds.MediaAppId), "Modern media application ID is recognized");
        check(CompatibilityIds.IsMusicSession(CompatibilityIds.MusicProcess + ".exe"), "Windows 10 executable media identity is recognized");
        check(CompatibilityIds.IsMusicSession((CompatibilityIds.MusicProcess + ".exe").ToUpperInvariant()), "Executable media identity comparison ignores casing");
        check(!CompatibilityIds.IsMusicSession(null) && !CompatibilityIds.IsMusicSession(""), "Missing media identity is rejected");
        check(!CompatibilityIds.IsMusicSession("browser.exe") && !CompatibilityIds.IsMusicSession("DifferentMusicApp.exe"), "Unrelated music and browser sessions are rejected");
        check(!CompatibilityIds.IsMusicSession("prefix." + CompatibilityIds.MediaAppId), "Partial application ID never matches");
        check(!CompatibilityIds.IsMusicSession(CompatibilityIds.MusicProcess + ".exe.other"), "Executable name prefix never matches");
    }
}