namespace YMusicTaskbar;

// External identifiers and the previous application identity are compatibility data, not branding.
public static class CompatibilityIds
{
    public const string MediaAppId = "ru.yandex.desktop.music";
    public const string MusicProcess = "Яндекс Музыка";
    public static bool IsMusicSession(string? source) =>
        string.Equals(source, MediaAppId, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(source, MusicProcess + ".exe", System.StringComparison.OrdinalIgnoreCase);

    public const string PreviousName = "YandexMusicTaskbar";
    public const string MutexName = @"Local\" + PreviousName + ".Panel";
    public const string QuitEvent = @"Local\" + PreviousName + ".Quit";
}
