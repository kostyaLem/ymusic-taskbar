using System.IO;
using Microsoft.Win32;
using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal static class StartupScenarios
{
    public static void Run(Action<bool, string> check)
    {
        string path = @"Software\YMusicTaskbar.Tests\" + Guid.NewGuid().ToString("N");
        string current = @"C:\Applications With Spaces\YMusicTaskbar.exe";
        var registration = new StartupRegistration(current, path);
        try
        {
            check(!registration.RepairExistingRegistration() && !registration.Enabled, "First launch does not enable startup");
            using (var key = Registry.CurrentUser.CreateSubKey(path)) key.SetValue("YMusicTaskbar", @"""C:\Old Prototype\YMusicTaskbar.exe""");
            check(registration.RepairExistingRegistration() && registration.Enabled, "Existing stale startup migrates to current executable");
            using (var key = Registry.CurrentUser.OpenSubKey(path))
                check(Equals(key!.GetValue("YMusicTaskbar"), '"' + current + '"'), "Migrated startup quotes paths with spaces");
            check(!registration.RepairExistingRegistration(), "Startup repair is idempotent");
            registration.SetEnabled(false);
            check(!registration.RepairExistingRegistration() && !registration.Enabled, "Disabled startup stays disabled");
            using (var key = Registry.CurrentUser.CreateSubKey(path)) key.SetValue("YMusicTaskbar", @"""C:\Other\Different.exe""");
            check(!registration.RepairExistingRegistration(), "Startup repair preserves unrelated commands");
            using (var key = Registry.CurrentUser.CreateSubKey(path)) key.SetValue("YMusicTaskbar", @"""C:\Old\YMusicTaskbar.exe"" --custom");
            check(!registration.RepairExistingRegistration(), "Startup repair preserves custom command arguments");
            registration.SetEnabled(true);
            check(registration.Enabled, "Explicit startup opt-in uses current executable");
            registration.SetEnabled(false);
            using (var key = Registry.CurrentUser.CreateSubKey(path)) key.SetValue(CompatibilityIds.PreviousName, '"' + @"C:\Previous\" + CompatibilityIds.PreviousName + ".exe\"");
            check(registration.RepairExistingRegistration() && registration.Enabled, "Rename preserves existing startup opt-in");
            using (var key = Registry.CurrentUser.OpenSubKey(path)) check(key!.GetValue(CompatibilityIds.PreviousName) is null, "Rename removes duplicate previous startup value");
            check(!registration.RepairExistingRegistration(), "Rename migration is idempotent");
            registration.SetEnabled(false);
            using (var key = Registry.CurrentUser.CreateSubKey(path)) key.SetValue(CompatibilityIds.PreviousName, "\"C:\\Other\\wrapper.exe\" --custom");
            check(!registration.RepairExistingRegistration(), "Rename preserves unrelated previous commands");
        }
        finally { Registry.CurrentUser.DeleteSubKey(path, throwOnMissingSubKey: false); }
        check(LegacyInstanceMigration.IsPrototype(CompatibilityIds.PreviousName, new Version(1, 0, 0, 0), "net8.0"), "Known prototype is recognized despite default version 1.0");
        check(!LegacyInstanceMigration.IsPrototype("YMusicTaskbar", new Version(0, 2, 2, 0), "net10.0"), "Current releases are never treated as prototype");
        check(!LegacyInstanceMigration.IsPrototype("DifferentProduct", new Version(1, 0, 0, 0), "net8.0"), "Other products are never selected for migration");
        check(!LegacyInstanceMigration.IsPrototype("YMusicTaskbar", new Version(1, 1, 0, 0), "net8.0"), "Unknown product versions are preserved");
        string directory = Path.Combine(Path.GetTempPath(), "YMusicTaskbar-migration-" + Guid.NewGuid().ToString("N"));
        string previous = Path.Combine(directory, "previous"), currentSettings = Path.Combine(directory, "current");
        try
        {
            Directory.CreateDirectory(previous);
            string json = "{\"Compact\":true}";
            File.WriteAllText(Path.Combine(previous, "settings.json"), json);
            AppPaths.MigrateSettings(previous, currentSettings);
            check(File.ReadAllText(Path.Combine(currentSettings, "settings.json")) == json, "Rename copies existing settings without modifying them");
            File.WriteAllText(Path.Combine(currentSettings, "settings.json"), "{}");
            AppPaths.MigrateSettings(previous, currentSettings);
            check(File.ReadAllText(Path.Combine(currentSettings, "settings.json")) == "{}", "Existing renamed settings take precedence");
            check(File.ReadAllText(Path.Combine(previous, "settings.json")) == json, "Previous settings remain available for rollback");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }
}
