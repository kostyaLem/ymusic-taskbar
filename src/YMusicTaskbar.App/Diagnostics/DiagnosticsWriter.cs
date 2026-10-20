using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace YMusicTaskbar;

internal static class DiagnosticsWriter
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static async Task SaveAsync(string path, RuntimeSnapshot snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path + ".tmp", JsonSerializer.Serialize(snapshot, Options));
        File.Move(path + ".tmp", path, true);
    }
}
