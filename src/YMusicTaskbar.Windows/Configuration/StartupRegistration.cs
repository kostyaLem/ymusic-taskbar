using System;
using Microsoft.Win32;

namespace YMusicTaskbar;

public sealed class StartupRegistration(string executablePath,
    string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run") : IStartupRegistration
{
    private const string Name = "YMusicTaskbar";
    public bool Enabled
    {
        get { using var key = Registry.CurrentUser.OpenSubKey(keyPath); return string.Equals(key?.GetValue(Name) as string, Quote(executablePath), StringComparison.OrdinalIgnoreCase); }
    }
    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        if (enabled) key.SetValue(Name, Quote(executablePath)); else key.DeleteValue(Name, false);
        if (IsDirectCommand(key.GetValue(CompatibilityIds.PreviousName) as string)) key.DeleteValue(CompatibilityIds.PreviousName, false);
    }
    public bool RepairExistingRegistration()
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        if (key is null) return false;
        string? command = key.GetValue(Name) as string;
        string? previous = key.GetValue(CompatibilityIds.PreviousName) as string;
        // Preserve an explicit current entry, including custom wrappers; migrate only known direct commands.
        if (command is not null && !IsDirectCommand(command)) return false;
        bool migratePrevious = IsDirectCommand(previous);
        if (command is null && !migratePrevious) return false;
        bool changed = !string.Equals(command, Quote(executablePath), StringComparison.OrdinalIgnoreCase) || migratePrevious;
        if (changed) key.SetValue(Name, Quote(executablePath));
        if (migratePrevious) key.DeleteValue(CompatibilityIds.PreviousName, false);
        return changed;
    }
    private static bool IsDirectCommand(string? command)
    {
        if (command is null) return false;
        string path = command.Trim();
        if (path.StartsWith('"') && path.EndsWith('"') && path.Length > 2) path = path[1..^1];
        string name = System.IO.Path.GetFileName(path);
        return System.IO.Path.IsPathFullyQualified(path) &&
            (string.Equals(name, "YMusicTaskbar.exe", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(name, CompatibilityIds.PreviousName + ".exe", StringComparison.OrdinalIgnoreCase));
    }
    private static string Quote(string value) => "\"" + value + "\"";
}
