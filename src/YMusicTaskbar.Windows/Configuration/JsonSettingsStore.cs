using System;
using System.IO;
using System.Text.Json;

namespace YMusicTaskbar;

public sealed class JsonSettingsStore(string? filePath = null) : ISettingsStore
{
    private readonly string path = filePath ?? AppPaths.SettingsFile;
    public Settings Load()
    {
        try
        {
            if (filePath is null) AppPaths.MigratePreviousSettings();
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    public void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, true);
    }
}
