using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
namespace YMusicTaskbar;

public sealed record WindowsVersionInfo(string Type, string? Edition, string? DisplayVersion,
    int BuildNumber, int? Revision, string Version, string Architecture)
{
    public static WindowsVersionInfo Capture()
    {
        var version = Environment.OSVersion.Version;
        int build = version.Build;
        int? revision = null;
        string? edition = null, release = null, installation = null;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (int.TryParse(key?.GetValue("CurrentBuildNumber") as string, out int currentBuild)) build = currentBuild;
            revision = key?.GetValue("UBR") as int?;
            edition = key?.GetValue("EditionID") as string;
            release = key?.GetValue("DisplayVersion") as string;
            installation = key?.GetValue("InstallationType") as string;
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException or IOException) { }
        bool server = installation?.StartsWith("Server", StringComparison.OrdinalIgnoreCase) == true;
        string type = server ? "Windows Server" : build >= 22000 ? "Windows 11" : "Windows 10";
        string fullVersion = $"{version.Major}.{version.Minor}.{build}" + (revision is { } ubr ? $".{ubr}" : "");
        return new(type, edition, release, build, revision, fullVersion, RuntimeInformation.OSArchitecture.ToString());
    }
}