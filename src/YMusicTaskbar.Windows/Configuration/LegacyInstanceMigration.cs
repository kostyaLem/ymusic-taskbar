using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace YMusicTaskbar;

public static class LegacyInstanceMigration
{
    public static bool IsPrototype(string? product, Version? version, string? framework) =>
        string.Equals(product, CompatibilityIds.PreviousName, StringComparison.Ordinal) &&
        version == new Version(1, 0, 0, 0) && framework == "net8.0";

    public static async Task<bool> StopPrototypeAsync()
    {
        bool stopped = false;
        using var current = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(CompatibilityIds.PreviousName))
        {
            using (process)
            {
                try
                {
                    if (process.Id == Environment.ProcessId || process.SessionId != current.SessionId) continue;
                    string? path = process.MainModule?.FileName;
                    if (path is null) continue;
                    var info = FileVersionInfo.GetVersionInfo(path);
                    var version = new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
                    // Inspect only sibling runtime metadata, never load or execute the old assembly.
                    string config = Path.ChangeExtension(path, ".runtimeconfig.json");
                    if (!File.Exists(config) || new FileInfo(config).Length > 65536) continue;
                    using var json = JsonDocument.Parse(File.ReadAllText(config));
                    string? framework = json.RootElement.GetProperty("runtimeOptions").GetProperty("tfm").GetString();
                    if (string.Equals(info.ProductName, CompatibilityIds.PreviousName, StringComparison.Ordinal) &&
                        version >= new Version(0, 2, 0, 0) && version <= new Version(0, 2, 3, 0) && framework == "net10.0")
                    {
                        SingleInstanceLease.RequestExit();
                        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(4));
                        stopped = true; continue;
                    }
                    if (!IsPrototype(info.ProductName, version, framework)) continue;
                    // This prototype predates the graceful --quit protocol.
                    process.Kill(); await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(4)); stopped = true;
                }
                catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or IOException or JsonException or TimeoutException or System.Collections.Generic.KeyNotFoundException)
                { /* An unrelated, inaccessible or already stopped process must not block launch. */ }
            }
        }
        return stopped;
    }
}
