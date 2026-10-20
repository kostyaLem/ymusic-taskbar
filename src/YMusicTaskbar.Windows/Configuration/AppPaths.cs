using System;
using System.IO;

namespace YMusicTaskbar;

public static class AppPaths
{
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YMusicTaskbar");
    public static string SettingsFile => Path.Combine(DirectoryPath, "settings.json");
    public static void MigrateSettings(string previousDirectory, string currentDirectory)
    {
        string previous = Path.Combine(previousDirectory, "settings.json");
        string current = Path.Combine(currentDirectory, "settings.json");
        if (File.Exists(current) || !File.Exists(previous)) return;
        Directory.CreateDirectory(currentDirectory);
        File.Copy(previous, current, overwrite: false);
    }
    public static void MigratePreviousSettings() => MigrateSettings(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompatibilityIds.PreviousName), DirectoryPath);
}
